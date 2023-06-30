using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;
using Toast.Gamebase;
using XDR;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UniRx;
using UniRx.Async;

public partial class NetworkServiceHandler
{
	private string loginContents;
	public static bool IsSessionValid { get; private set; } = false;
	private bool needToFetchConfig { get; set; } = true;

	private const float WAIT_RECONNECT_TIME = 15f;

	private struct RequestResult<T> where T : XDR.IMessage
	{
		public STAR_ErrCode ErrorCode;
		public string ErrorMessage;
		public T Result;
		public bool IsSuccess => ErrorCode == STAR_ErrCode.ERR_SUCCESS;
	}
	private async Task<RequestResult<TResult>> request<TResult>(XDR.IMessage request) where TResult : XDR.IMessage
	{
		var response = await NetworkClient.Instance.SendRequest(request, false, TimeSpan.FromSeconds(WAIT_RECONNECT_TIME));
		if (response.header != null && response.header.errCode == (int)STAR_ErrCode.ERR_EXPIRED_SESSION)
			IsSessionValid = false;

		if (response.header != null
			&& response.header.errCode != (int)STAR_ErrCode.ERR_SUCCESS
			&& response.header.errCode != (int)STAR_ErrCode.ERR_FAIL_FIND_USER
			&& response.header.errCode != (int)STAR_ErrCode.ERR_INVALID_PASSWORD
			&& response.header.errCode != (int)STAR_ErrCode.ERR_UNMATCH_ITEM_COUNT
			&& response.header.errCode != (int)STAR_ErrCode.ERR_INVALID_USER)		// 230224 - 유저 편입시 에러처리는 따로 한다.
		{
			PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
			{
				//Momo.Localization.Get("system_error_loginfail")
				string message = Momo.Localization.Get($"system_error_{response.header.errCode:X7}");
				popup.SetSystemPopupController(message);
			}, () =>
			{
				Logger.Log("!!!");
			});
		}
		// 230224 - 유저 편입시 에러처리
		else if (response.header.errCode.Equals((int)STAR_ErrCode.ERR_INVALID_USER))
		{
			Logger.LogError("ERR_INVALID_USER!!!!!");
			var closePopup = new TaskCompletionSource<bool>();
			PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
			{
				//Momo.Localization.Get("system_error_loginfail")
				string message = Momo.Localization.Get($"system_error_{response.header.errCode:X7}");
				popup.SetSystemPopupController(message);
			}, onCloseAction: () =>
			{
				Logger.Log("!!!");
				closePopup.SetResult(true);
			});
			await closePopup.Task;

			PlayerPrefs.SetInt("SkipPassport", 1);
			await MPServiceFacade.Platform.Logout(true);
		}

		return new RequestResult<TResult>()
		{
			ErrorCode = response.ErrorCode(),
			ErrorMessage = response.ErrorMessage(),
			Result = response.GetMessage<TResult>()
		};
	}


	[ServiceHandler(NetworkServiceRequest.Login, -1)]
	public async Task<MPServiceResult> SetLoginContents(string contents)
	{
		loginContents = contents;

		return MPServiceResult.Success();
	}

	[ServiceHandler(NetworkServiceRequest.Login)]
	public async Task<MPServiceResult> Login(string contents)
	{
		var login = JsonUtility.FromJson<Contents.Login>(contents);

		var serverLoginType = STAR_LoginType.GUEST;
		switch (login.LoginType)
		{
			case GamebaseAuthProvider.GOOGLE:
				serverLoginType = STAR_LoginType.GOOGLE_PLAY;
				break;
			case GamebaseAuthProvider.GAMECENTER:
				serverLoginType = STAR_LoginType.GAME_CENTER;
				break;
			case GamebaseAuthProvider.FACEBOOK:
				serverLoginType = STAR_LoginType.FACEBOOK;
				break;
		}
		NetworkClient.Instance.ResetTransaction();
		var response = await request<SPEAK_RES_Login>(new SPEAK_REQ_Login
		{
			token = login.Token,
			isGuest = login.IsGuest,
			majorVer = login.Version.Major,
			minorVer = login.Version.Minor,
			buildNo = login.Version.BuildNumber,
			osType = (sbyte)login.OSType,
			loginType = (sbyte)serverLoginType,
			langType = (sbyte)login.Language,
			osName = login.OSName,
		});
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GameServerLoginFailed);
		IsSessionValid = true;

		var message = response.Result;
		login.ConfigVersion = message.configVersion;
		login.ConfigRevision = message.configRevision;
		login.checkLcmsUser = message.checkLcmsUser;
		login.ocUrl = message.ocUrl;
		login.sttUrl = message.sttUrl;
		login.sttPort = message.sttPort;
		login.sttMagic = message.sttMagic;
		login.fileDownUrl = message.fileDownUrl;
		login.toastAuthTocken = message.toastAuthTocken;
		login.checkUserPassport = message.checkUserPassport;
		login.checkOpeningUser = message.checkOpeningUser;	// HGKIM 221216 - 오픈형로그인 유저인지 여부 체크
		login.ebsId = message.ebsId;						// HGKIM 221227 - ebsID

		//기존의 함수 대체하기
		//GameData.SetConfigInfo(message);

		MPServiceResult result = MPServiceResult.Success(login);
		loginContents = result.Contents;
		return result;

	}

	[ServiceHandler(NetworkServiceRequest.Login, 1)]
	public async Task<MPServiceResult> EnterLobby(string contents)
	{

		var login = JsonUtility.FromJson<Contents.Login>(loginContents);
		//		//계정연동 코드
		if (login.checkUserPassport == false)
		//if( true)
		{
			GamebaseController.Instance.FinishLoading(false);

			// HGKIM 221219 - 로그인 선택 팝업 이쪽으로 이동
			// 로그인 셀렉트 팝업을 연다
			var closeSelectPopup = new TaskCompletionSource<bool>();
			PopupManager.Instance.OpenPopup<SelectLoginPopupController>(popup =>
			{
				popup.Bind();
			},
			onCloseAction: async () =>
			{
				await Task.Delay(100);

				closeSelectPopup.SetResult(true);
			});
			await closeSelectPopup.Task;
			
			// 웹뷰는 성공했는데 통신에 실패했을경우
			if (GameData.isOpenLoginFail)
			{
				PlayerPrefs.SetInt("SkipPassport", 1);
				await MPServiceFacade.Platform.Logout(true);	// 아에 로그아웃 처리를 해버린다.
				return MPServiceResult.Fail(MPServiceResultCode.AccountCodeFailed);
			}

			// 여기는 새로운 로그인으로 탈 때 사용한다.
			if (GameData.isOpenPublicLogin)
			{
				Logger.Log($"개방형 로그인 성공!!!");

				// HGKIM 221228 - 퍼미션 체크를 한다.
				bool hasPermission = Utility.HasMicrophonePermission() || Utility.HasCameraPermission();
				if (hasPermission == false)
				{
					var closePopup = new TaskCompletionSource<bool>();
					PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
					{
						popup.SetSystemPopupController(
							Momo.Localization.Get("popup_permission_title"),
							Momo.Localization.Get("popup_permission_message"));
					},
						onCloseAction: () =>
						{

							closePopup.SetResult(true);
						});
					await closePopup.Task;

					if (Utility.HasMicrophonePermission() == false)
					{
						await Utility.RequestMicrophone();
						await UniRx.Async.UniTask.Delay(200);
						await UniRx.Async.UniTask.WaitUntil(() => Application.isFocused == true);
					}

					if (Utility.HasCameraPermission() == false)
					{
						await Utility.RequestCamera();
						await UniRx.Async.UniTask.Delay(200);
						await UniRx.Async.UniTask.WaitUntil(() => Application.isFocused == true);
					}

					if (Utility.HasGalleryWritePermission() == false)
					{
						await Utility.RequestGallery();
						await UniRx.Async.UniTask.Delay(200);
						await UniRx.Async.UniTask.WaitUntil(() => Application.isFocused == true);
					}
				}
			}
			// 여기는 기존 로그인으로 들어간다.
			else if (GameData.isOpenPublicLogin == false)
			{
				Logger.Log("기존 로그인을 탄다!!!");

				var closePopup = new TaskCompletionSource<bool>();
				PopupManager.Instance.OpenPopup<PassportPopupController>(popup =>
				   {
					   popup.Bind();
				   },
					onCloseAction: async () =>
					{
						await Task.Delay(100);
					//PopupManager.Instance.OpenPopup<AccountCodeResultPopupController>("AccountCodeResultPopupController",
					//	popup =>
					//	{

					//	},
					//	onCloseAction: () =>
					//	{
					//		
					//	});
					closePopup.SetResult(true);
					});
				await closePopup.Task;
				GamebaseController.Instance.StartLoading();
			}
		}
		// HGKIM 221226 - 개방형 로그인 여부 체크를 위해 추가
		else 
		{
			Logger.Log("auto login!!");
			if (login.checkOpeningUser)
			{
				Logger.Log("개방형로그인!!!!");
				GameData.isOpenPublicLogin = login.checkOpeningUser;    // 서버에서 만들어지면 여기 추가한다.
				GameData.ebsId = login.ebsId;							// ebsID 저장
			}
		}
		// HGKIM 221226 - 개방형 로그인 여부 체크를 위해 추가
		//#if UNITY_ANDROID
		//bool isFirst = (0 == PlayerPrefs.GetInt(string.Format("permission"), 0));
		//if (isFirst)
		//{
		//	//최초 실행
		//	GamebaseController.Instance.FinishLoading(false);
		//	var closePopup = new TaskCompletionSource<bool>();

		//	PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
		//	popup =>
		//	{
		//		popup.SetSystemPopupController(
		//			Momo.Localization.Get("popup_permission_title"),
		//			Momo.Localization.Get("popup_permission_message"));
		//	},
		//	onCloseAction: () =>
		//	{
		//		PlayerPrefs.SetInt(string.Format("permission"), 1);
		//		Permission.RequestUserPermission(Permission.Microphone);
		//		closePopup.SetResult(true);
		//	});
		//	await closePopup.Task;

		//	await UniRx.Async.UniTask.Delay(200);
		//	await UniRx.Async.UniTask.WaitUntil(() => Application.isFocused == true);

		//	GamebaseController.Instance.StartLoading();

		//}

		//#endif
		return MPServiceResult.Success(contents);
	}

	[ServiceHandler(NetworkServiceRequest.Withdraw)]
	public async Task<MPServiceResult> Withdraw(string contents)
	{
		//var response = await request<STAR_RES_TX_Withdraw>(new STAR_REQ_TX_Withdraw());
		//if (!response.IsSuccess)
		//	return MPServiceResult.Fail(MPServiceResultCode.WithdrawFailed);
		return MPServiceResult.Success();
	}

	[ServiceHandler(NetworkServiceRequest.LoadGameConfig)]
	public async Task<MPServiceResult> LoadGameConfig(string contents)
	{
		if (!needToFetchConfig)
			return MPServiceResult.Success();

		var response = await request<STAR_RES_LoadGameConfig>(new STAR_REQ_LoadGameConfig());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.LoadConfigFailed);


		return MPServiceResult.Success(response.Result.configJson);
	}

	///// 2차 ///////////////////////////////////////////////////////////////////////////////////////////////

	// 회원 코드
	[ServiceHandler(NetworkServiceRequest.AccountCode)]
	public async Task<MPServiceResult> AccountCode(string contents)
	{
		Logger.Log("NetworkServiceRequest.AccountCode");
		var data = JsonUtility.FromJson<Contents.AccountCode>(contents);
		var response = await request<SPEAK_RES_AccountCode>(new SPEAK_REQ_AccountCode
		{
			loginId = data.code
		});

		if (!response.IsSuccess)
		{
			return MPServiceResult.Fail(MPServiceResultCode.AccountCodeFailed);
		}
		var res = response.Result;

		// HGKIM 221221 - test code 
		if (GameData.isOpenPublicLogin)
		{
			Logger.Log("NetworkServiceRequest.AccountCode!! 개방형 로그인!!!");
			GameData.SetUserInfo(Gamebase.GetUserID(), res.lmsUserInfo);
		}
		// 여기 아래가 원래 있던 코드
		else
		{
			PassportPopupController passportPopup = PopupManager.Instance.GetPopup<PassportPopupController>();
			passportPopup.isEmtpyPassword = res.ispasswordEmpty;
			GameData.SetUserInfo(Gamebase.GetUserID(), res.lmsUserInfo);
		}
		return MPServiceResult.Success(res);
	}

	// HGKIM 221220 - 개방형 로그인 추가
	//[ServiceHandler(NetworkServiceRequest.AccountCodeNew)]
	//public async Task<MPServiceResult> AccountCodeNew(string contents)
	//{
	//	Logger.Log("[NetworkServiceRequest] AccountCodeNew");
	//	var data = JsonUtility.FromJson<Contents.AccountCode>(contents);
	//	var response = await request<SPEAK_RES_AccountCode>(new SPEAK_REQ_AccountCode
	//	{
	//		loginId = data.code
	//	});

	//	if (!response.IsSuccess)
	//	{
	//		return MPServiceResult.Fail(MPServiceResultCode.AccountCodeFailed);
	//	}
	//	var res = response.Result;
	//	GameData.SetUserInfo(Gamebase.GetUserID(), res.lmsUserInfo);

	//	return MPServiceResult.Success(res);
	//}

	[ServiceHandler(NetworkServiceRequest.Key)]
	public async Task<MPServiceResult> Password(string contents)
	{
		var data = JsonUtility.FromJson<Contents.AccountCode>(contents);
		var response = await request<SPEAK_RES_Password>(new SPEAK_REQ_Password
		{
			password = data.code
		});

		if (!response.IsSuccess)
		{
			return MPServiceResult.Fail(MPServiceResultCode.PasswordFailed);
		}
		var res = response.Result;
		return MPServiceResult.Success(res);
	}
	
	[ServiceHandler(NetworkServiceRequest.AccountCodeWithdraw)]
	public async Task<MPServiceResult> AccountCodeWithdraw(string contents)
	{
		var response = await request<SPEAK_RES_AccountCodeWithdraw>(new SPEAK_REQ_AccountCodeWithdraw());

		if (!response.IsSuccess)
		{
			return MPServiceResult.Fail(MPServiceResultCode.AccountCodeWithDrawFailed);
		}

		return MPServiceResult.Success("{}");
	}

	// 여권 만들기
	[ServiceHandler(NetworkServiceRequest.MakePassport)]
	public async Task<MPServiceResult> MakePassport(string contents)
	{
		var data = JsonUtility.FromJson<Contents.MakePassport>(contents);
		var result = await request<SPEAK_RES_PassportMake>(new SPEAK_REQ_PassportMake { nickName = data.nickName, imgIdx = data.profileIndex });
		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.MakePassportFailed);

		return MPServiceResult.Success("");
	}

	// 사용자 프로필 조회
	[ServiceHandler(NetworkServiceRequest.GetProfile)]
	public async Task<MPServiceResult> GetProfile(string contents)
	{
		var response = await request<SPEAK_RES_GetProfile>(new SPEAK_REQ_GetProfile());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetProfileFailed);

		GameData.SetUserInfo(response.Result.userGameInfo.userNo, response.Result.lmsUserInfo);
		GameData.SetUserGameInfo(response.Result.userGameInfo);

		//여기서 앱가드 넣어준다.	//20220712
#if UNITY_ANDROID
		PerftestManager.SetId(GameData.User.loginId);
#elif UNITY_IOS
		//NativeAppGuardIOS.SetAppGuardIOS("WwcShiDaqtKzaVHI", Application.version, "AI펭톡", GameData.User.loginId);
#endif
		

		return MPServiceResult.Success(GameData.User);
	}

	// 게임 데이터
	[ServiceHandler(NetworkServiceRequest.GetGameData)]
	public async Task<MPServiceResult> GetGameData(string contents)
	{
		var response = await request<SPEAK_RES_GameData>(new SPEAK_REQ_GameData());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetGameDataFailed);

		GameData.Study.ParseStudy(response.Result);

		GameData.ParseStampData(response.Result.stampData.stamps);
		GameData.ParseItemData(response.Result.itemData.items);
		GameData.ParseAttend(response.Result.attendData.attends);
		GameData.ParseTalkerInfo(response.Result.talkCodeData.talkerCodes);

		return MPServiceResult.Success(GameData.Study.Worlds);
	}

	// 파닉스 데이터
	[ServiceHandler(NetworkServiceRequest.GetPhonicsData)]
	public async Task<MPServiceResult> GetPhonicsData(string contents)
	{
		var response = await request<SPEAK_RES_GameData_Phonics>(new SPEAK_REQ_GameData_Phonics());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetPhonicsDataFailed);

		GameData.Study.ParsePhonics(response.Result);

		return MPServiceResult.Success(GameData.Study.Worlds);
	}

	// 출석
	[ServiceHandler(NetworkServiceRequest.Attendance)]
	public async Task<MPServiceResult> Attendance(string contents)
	{
		var response = await request<SPEAK_RES_AttendPopup>(new SPEAK_REQ_AttendPopup());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.AttendanceFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 공지
	[ServiceHandler(NetworkServiceRequest.GetNoticeList)]
	public async Task<MPServiceResult> GetNoticeList(string contents)
	{
		var response = await request<SPEAK_RES_AppNoticePopup>(new SPEAK_REQ_AppNoticePopup());
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetNoticeListFailed);

		Logger.Log($"notice count:{response.Result.noticeList.Count}");
		Data.NoticeContainer container = new Data.NoticeContainer();
		container.noticeInfo = response.Result.noticeList.Select(n => new Data.NoticeInfo
		{
			bannerUrl = n.bannerUrl,
			linkUrl = n.linkUrl,
			seq = (int)n.noticeSeq
		}).ToList();
		return MPServiceResult.Success(container);
	}

	// 로비 입장
	[ServiceHandler(NetworkServiceRequest.GetLobbyInfo)]
	public async Task<MPServiceResult> GetLobbyInfo(string contents)
	{
		var result = await request<SPEAK_RES_EnterLobby>(new SPEAK_REQ_EnterLobby());

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetLobbyInfoFailed);

		// 파닉스 학습 기록 조회 (스탬프와 별 표기를 위함)
		await StudyData.Current.Request_StudyRecord_Phonics();

		//SPEAK_ST_ItemInven

		GameData.Study.SetStudyRecords(result.Result);
		GameData.SetInven(result.Result.itemInvens);
		GameData.SetTutorial(result.Result.openTutorials);
		foreach (var item in result.Result.openTutorials)
		{
			Logger.Log(item.tutorialName + item.no);
		}
		GameData.RefreshRecommendData();
		GameData.SetHasStamp();

		return MPServiceResult.Success("");
	}

	// 월드 활동 시작
	[ServiceHandler(NetworkServiceRequest.WorldStartActivity)]
	public async Task<MPServiceResult> WorldStartActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_World_StartActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.WorldStartActivityFailed);

		return MPServiceResult.Success(response.Result);
	}
	// 월드 활동 종료
	[ServiceHandler(NetworkServiceRequest.WorldFinishActivity)]
	public async Task<MPServiceResult> WorldFinishActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_World_FinishActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.WorldFinishActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 참치캔 보너스 활동 시작
	[ServiceHandler(NetworkServiceRequest.WorldStartTunaActivity)]
	public async Task<MPServiceResult> WorldStartTunaActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_World_StartTunaActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.WorldStartTunaActivityFailed);

		return MPServiceResult.Success(response.Result);
	}
	// 참치캔 보너스 활동 종료
	[ServiceHandler(NetworkServiceRequest.WorldFinishTunaActivity)]
	public async Task<MPServiceResult> WorldFinishTunaActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_World_FinishTunaActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.WorldFinishTunaActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 테마 활동 시작
	[ServiceHandler(NetworkServiceRequest.ThemeStartActivity)]
	public async Task<MPServiceResult> ThemeStartActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_ThemeWorld_StartActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ThemeStartActivityFailed);

		return MPServiceResult.Success(response.Result);
	}
	// 테마 활동 종료
	[ServiceHandler(NetworkServiceRequest.ThemeFinishActivity)]
	public async Task<MPServiceResult> ThemeFinishActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_ThemeWorld_FinishActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ThemeFinishActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 스피킹 활동 시작
	[ServiceHandler(NetworkServiceRequest.SpeakingStartActivity)]
	public async Task<MPServiceResult> SpeakingStartActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_Speaking_StartActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SpeakingStartActivityFailed);

		return MPServiceResult.Success(response.Result);
	}
	// 스피킹 활동 종료
	[ServiceHandler(NetworkServiceRequest.SpeakingFinishActivity)]
	public async Task<MPServiceResult> SpeakingFinishActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_Speaking_FinishActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SpeakingFinishActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 파닉스 활동 시작
	[ServiceHandler(NetworkServiceRequest.PhonicsStartActivity)]
	public async Task<MPServiceResult> PhonicsStartActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_Phonics_StartActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.PhonicsStartActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 파닉스 활동 종료
	[ServiceHandler(NetworkServiceRequest.PhonicsFinishActivity)]
	public async Task<MPServiceResult> PhonicsFinishActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_Phonics_FinishActivity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.PhonicsFinishActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 렛츠톡 활동 시작
	[ServiceHandler(NetworkServiceRequest.LetsTalkStartEpisode)]
	public async Task<MPServiceResult> LetsTalkStartEpisode(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_LetsTalk_StartEpisode>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.LetsTalkStartEpisodeFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 렛츠톡 활동 종료
	[ServiceHandler(NetworkServiceRequest.LetsTalkFinishEpisode)]
	public async Task<MPServiceResult> LetsTalkFinishEpisode(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_LetsTalk_FinishEpisode>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.LetsTalkFinishEpisodeFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 랭킹 정보
	[ServiceHandler(NetworkServiceRequest.GetStudyRanking)]
	public async Task<MPServiceResult> GetStudyRanking(IMessage msg)
	{
		var result = await request<SPEAK_RES_StudyRanking>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetStudyRankingFailed);

		return MPServiceResult.Success(result.Result);
	}

	// 공지,선생님 알림
	[ServiceHandler(NetworkServiceRequest.GetAlarm)]
	public async Task<MPServiceResult> GetAlarm(IMessage msg)
	{
		var result = await request<SPEAK_RES_Alarm>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GetAlarmFailed);

		foreach (var item in result.Result.alarmNotice)
		{
			NewMarkManager.Instance.CheckToAddNewMark(NewMarkManager.NewMarkType.NewMark_NoticeItem, item.noticeNo);
		}

		foreach (var item in result.Result.alarmTeacher)
		{
			var startDt = Utility.GetDateTime(item.startDt);
			var endDt = Utility.GetDateTime(item.endDt);

			if (false == string.IsNullOrEmpty(item.feedbackDt))
			{
				NewMarkManager.Instance.CheckToAddNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Comment);
			}

			if (startDt > DateTime.Now)
			{
				NewMarkManager.Instance.CheckToAddNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Youtube);
			}

			if (startDt < DateTime.Now && endDt > DateTime.Now)
			{
				NewMarkManager.Instance.CheckToAddNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Open);
			}
		}

		return MPServiceResult.Success(result.Result);
	}

	// 활동 현황
	[ServiceHandler(NetworkServiceRequest.ActivityStatus)]
	public async Task<MPServiceResult> ActivityStatus(IMessage msg)
	{
		var result = await request<SPEAK_RES_ActivityStatus>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ActivityStatusFailed);

		return MPServiceResult.Success(result.Result);
	}

	// HGKIM 220726 - 오늘의 영단어 리퀘스트 추가
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_PushNotice)]
	public async Task<MPServiceResult> SPEAK_REQ_PushNotice(IMessage msg)
	{
		Logger.Log($"HGKIM || SPEAK_REQ_PushNotice || string is : {msg}");
		var response = await request<SPEAK_RES_PushNotice>(msg);
		if (!response.IsSuccess)    // 에러처리 임시작성
			//return MPServiceResult.Fail(MPServiceResultCode.LogoutFailed);
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_PushNoticeFailed,
				Contents = "SPEAK_REQ_PushNotice",
				message = response.Result,
				GamebaseErrorCode = 996
			};

		return MPServiceResult.Success(response.Result);
	}
	// HGKIM 220726 - 오늘의 영단어 리퀘스트 여기까지
	// HGKIM 220727 - 오늘의 영단어 완료 처리 패킷
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_PushNotice_InsertLog)]
	public async Task<MPServiceResult> SPEAK_REQ_PushNotice_InsertLog(IMessage msg)
	{
		Logger.Log($"HGKIM || SPEAK_REQ_PushNotice_InsertLog");

		var response = await request<SPEAK_RES_PushNotice_InsertLog>(msg);
		if (!response.IsSuccess)    // 에러처리 임시작성
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_PushNotice_InsertLog,
				Contents = "SPEAK_REQ_PushNotice_InsertLog",
				message = response.Result,
				GamebaseErrorCode = 997
			};

		return MPServiceResult.Success(response.Result);
	}
	// HGKIM 220727 - 오늘의 영단어 완료 처리 여기까지
	// HGKIM 220803 - 오늘의 영단어 획득한 단어 조회
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_PushNotice_Study)]
	public async Task<MPServiceResult> SPEAK_REQ_PushNotice_Study(IMessage msg)
	{
		Logger.Log("HGKIM || SPEAK_REQ_PushNotice_Study");

		var response = await request<SPEAK_RES_PushNotice_Study>(msg);
		if (!response.IsSuccess)	// 에러처리 임시작성
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_PushNotice_InsertLog,
				Contents = "SPEAK_REQ_PushNotice_Study",
				message = response.Result,
				GamebaseErrorCode = 998
			};

		return MPServiceResult.Success(response.Result);
	}
	// HGKIM 220803 - 오늘의 영단어 획득한 단어 조회 여기까지
	// HGKIM 220809 - 새 학년 체크 요청
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_GradeUp_Check)]
	public async Task<MPServiceResult> SPEAK_REQ_GradeUp_Check(string msg)
	{
		Logger.Log("HGKIM || SPEAK_REQ_GradeUp_Check");

		var response = await request<SPEAK_RES_GradeUp_Check>(new SPEAK_REQ_GradeUp_Check());
		if(!response.IsSuccess)
		{
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_PushNotice_Study,  // 에러코드 임시로 지정했다.
				Contents = "SPEAK_REQ_GradeUp_Check",
				message = response.Result,
				GamebaseErrorCode = 998									// 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_GradeUp_Store)]
	public async Task<MPServiceResult> SPEAK_REQ_GradeUp_Store(IMessage msg)
	{
		Logger.Log("HGKIM || SPEAK_REQ_GradeUp_Store");

		var response = await request<SPEAK_RES_GradeUp_Store>(msg);
		if (!response.IsSuccess)
		{
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_PushNotice_Study,  // 에러코드 임시로 지정했다.
				Contents = "SPEAK_REQ_GradeUp_Store",
				message = response.Result,
				GamebaseErrorCode = 998                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	// HGKIM 220809 - 진급처리 종료
	// HGKIM 220928 - 비밀번호 변경 API
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_Password_Change)]
	public async Task<MPServiceResult> SPEAK_REQ_Password_Change(string contents)
	{
		Logger.Log("HGKIM || SPEAK_REQ_Password_Change || string");
		var msg = JsonUtility.FromJson<Contents.ChangePwd>(contents);

		var response = await request<SPEAK_RES_Password_Change>(new SPEAK_REQ_Password_Change { oldStdetPwd = msg.oldStdetPwd, newStdetPwd = msg.newStdetPwd });
		if(!response.IsSuccess)
		{
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_REQ_Password_Change,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_REQ_Password_Change",
				message = response.Result,
				GamebaseErrorCode = 995                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	//[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_Password_Change)]
	//public async Task<MPServiceResult> SPEAK_REQ_Password_Change(IMessage msg)
	//{
	//	Logger.Log("HGKIM || SPEAK_REQ_Password_Change || IMessage");

	//	var response = await request<SPEAK_RES_Password_Change>(msg);
	//	if (!response.IsSuccess)
	//	{
	//		return new MPServiceResult
	//		{
	//			Code = MPServiceResultCode.SPEAK_REQ_Password_Change,   // 에러코드 임시로 지정했다.
	//			Contents = "SPEAK_REQ_Password_Change",
	//			message = response.Result,
	//			GamebaseErrorCode = 995                                 // 에러코드 임시로 지정했다.
	//		};
	//	}

	//	return MPServiceResult.Success(response.Result);
	//}
	// HGKIM 220928 - 비밀번호 변경 API 여기까지
	// 230215 - 랭킹 갱신 여부 요청
	//SPEAK_REQ_RankingPrize
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_RankingPrize)]
	public async Task<MPServiceResult> SPEAK_REQ_RankingPrize(string contents)
	{
		Logger.Log("SPEAK_REQ_RankingPrize IN");

		var response = await request<SPEAK_RES_RankingPrize>(new SPEAK_REQ_RankingPrize());
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_RankingPrize Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_RankingPrizeFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_REQ_RankingPrize",
				message = response.Result,
				GamebaseErrorCode = 994                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	// 230215 - 랭킹 갱신 여부 요청 여기까지
	// 230215 - 랭킹 보상 획득 전송
	//SPEAK_REQ_RankingPrize_Receive
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_RankingPrize_Receive)]
	public async Task<MPServiceResult> SPEAK_REQ_RankingPrize_Receive(IMessage msg)
	{
		Logger.Log("SPEAK_REQ_RankingPrize IN");

		var response = await request<SPEAK_RES_RankingPrize_Receive>(msg);
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_RankingPrize_Receive Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_RankingPrize_ReceiveFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_REQ_RankingPrize_Receive",
				message = response.Result,
				GamebaseErrorCode = 994                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	// 230215 - 랭킹 보상 획득 전송 여기까지
	// 230510 - 우편함 관련
	// SPEAK_REQ_Mailbox - 우편함 리스트 요청
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_Mailbox)]
	public async Task<MPServiceResult> SPEAK_REQ_Mailbox(IMessage msg)
	{
		Logger.Log("SPEAK_REQ_Mailbox IN");

		var response = await request<SPEAK_RES_Mailbox>(msg);
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_Mailbox Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_MailboxFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_RES_Mailbox",
				message = response.Result,
				GamebaseErrorCode = 993                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	//SPEAK_REQ_Mailbox_Receive - 참치캔 받기 요청
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_Mailbox_Receive)]
	public async Task<MPServiceResult> SPEAK_REQ_Mailbox_Receive(IMessage msg)
	{
		Logger.Log("SPEAK_REQ_Mailbox_Receive IN");

		var response = await request<SPEAK_RES_Mailbox_Receive>(msg);
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_Mailbox_Receive Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_Mailbox_ReceiveFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_RES_Mailbox_Receive",
				message = response.Result,
				GamebaseErrorCode = 991                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	//SPEAK_REQ_Mailbox_ReceiveAll - 참치캔 모두받기 요청
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_Mailbox_ReceiveAll)]
	public async Task<MPServiceResult> SPEAK_REQ_Mailbox_ReceiveAll(IMessage msg)
	{
		Logger.Log("SPEAK_REQ_Mailbox_ReceiveAll IN");

		var response = await request<SPEAK_RES_Mailbox_ReceiveAll>(msg);
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_Mailbox_ReceiveAll Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_Mailbox_ReceiveAllFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_RES_Mailbox_ReceiveAll",
				message = response.Result,
				GamebaseErrorCode = 990                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	// 230510 - 우편함 관련 여기까지
	// 230510 - 참치캔 획득 내역 리스트 요청
	//SPEAK_REQ_RewardTuna
	[ServiceHandler(NetworkServiceRequest.SPEAK_REQ_RewardTuna)]
	public async Task<MPServiceResult> SPEAK_REQ_RewardTuna(IMessage msg)
	{
		Logger.Log("SPEAK_REQ_RewardTuna IN");

		var response = await request<SPEAK_RES_RewardTuna>(msg);
		if (!response.IsSuccess)
		{
			Logger.Log("SPEAK_RES_RewardTuna Fail");
			return new MPServiceResult
			{
				Code = MPServiceResultCode.SPEAK_RES_RewardTunaFailed,   // 에러코드 임시로 지정했다.
				Contents = "SPEAK_RES_RewardTuna",
				message = response.Result,
				GamebaseErrorCode = 989                                 // 에러코드 임시로 지정했다.
			};
		}

		return MPServiceResult.Success(response.Result);
	}
	// 230510 - 참치캔 획득 내역 리스트 요청 여기까지


	//튜토리얼 완료
	[ServiceHandler(NetworkServiceRequest.CompleteTutorial)]
	public async Task<MPServiceResult> CompleteTutorial(IMessage msg)
	{
		var result = await request<SPEAK_RES_CompleteTutorial>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.TutorialFailed);

		return MPServiceResult.Success(result.Result);
	}

	//스캔잇 검색
	[ServiceHandler(NetworkServiceRequest.ScanItSearch)]
	public async Task<MPServiceResult> ScanItSearch(IMessage msg)
	{
		var result = await request<SPEAK_RES_ScanItSearch>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ScanItSearchFailed);

		return MPServiceResult.Success(result.Result);
	}

	//스캔잇 리스트
	[ServiceHandler(NetworkServiceRequest.ScanItStart)]
	public async Task<MPServiceResult> ScanItStart(IMessage msg)
	{
		var result = await request<SPEAK_RES_ScanItStart>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ScanItStartFailed);

		GameData.ParseScanIt(result.Result);
		return MPServiceResult.Success(result.Result);
	}

	//스쿨톡 방정보
	[ServiceHandler(NetworkServiceRequest.SchoolTalk_RoomInfo)]
	public async Task<MPServiceResult> SchoolTalk_RoomInfo(IMessage msg)
	{
		var result = await request<SPEAK_RES_SchoolTalk_RoomInfo>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SchoolTalk_RoomInfoFailed);

		return MPServiceResult.Success(result.Result);
	}

	//스쿨톡 대화방
	[ServiceHandler(NetworkServiceRequest.SchoolTalk_Conversation)]
	public async Task<MPServiceResult> SchoolTalk_Conversation(IMessage msg)
	{
		var result = await request<SPEAK_RES_SchoolTalk_Conversation>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SchoolTalk_ConversationFailed);

		return MPServiceResult.Success(result.Result);
	}

	//스쿨톡 메시지 히스토리
	[ServiceHandler(NetworkServiceRequest.SchoolTalk_Message)]
	public async Task<MPServiceResult> SchoolTalk_Message(IMessage msg)
	{
		var result = await request<SPEAK_RES_SchoolTalk_Message>(msg);

		if (!result.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SchoolTalk_MessageFailed);

		return MPServiceResult.Success(result.Result);
	}


	//아이템 갯수 검증
	[ServiceHandler(NetworkServiceRequest.MatchItem)]
	public async Task<MPServiceResult> MatchItem(IMessage msg)
	{
		var result = await request<SPEAK_RES_MatchItem>(msg);

		// 230421 - 기존의 fail을 안타고 fail 정보는 유지하되 result 메시지는 안날리게 수정
		if (!result.IsSuccess)
		{
			//return MPServiceResult.Fail(MPServiceResultCode.MatchItemFailed);	// 기존 코드 백업
			return new MPServiceResult
			{
				Code = MPServiceResultCode.MatchItemFailed,
				Contents = null,
				message = result.Result,
				GamebaseErrorCode = Toast.Gamebase.GamebaseErrorCode.UNKNOWN_ERROR
			};
		}

		return MPServiceResult.Success(result.Result);
	}

	// 보유 코스튬 요청
	[ServiceHandler(NetworkServiceRequest.Costume_InvenList)]
	public async Task<MPServiceResult> Costume_InvenList(IMessage msg)
	{
		var response = await request<SPEAK_RES_DecorItemInven>(msg);
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.CostumeInvenListFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 코스튬상품 요청
	[ServiceHandler(NetworkServiceRequest.Costume_Products)]
	public async Task<MPServiceResult> Costume_Products(IMessage msg)
	{
		var response = await request<SPEAK_RES_Products>(msg);
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.CostumeProductsFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 코스튬상품 구매요청
	[ServiceHandler(NetworkServiceRequest.Costume_PurchaseProduct)]
	public async Task<MPServiceResult> Costume_PurchaseProduct(IMessage msg)
	{
		var response = await request<SPEAK_RES_PurchaseProduct>(msg);
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.CostumePurchaseProductFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 코스튬 장착요청
	[ServiceHandler(NetworkServiceRequest.Costume_PutOn)]
	public async Task<MPServiceResult> Costume_PutOn(IMessage msg)
	{
		var response = await request<SPEAK_RES_PutOnItem>(msg);
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.CostumePutOnFailed);

		return MPServiceResult.Success(response.Result);
	}

	// 코스튬 해제요청
	[ServiceHandler(NetworkServiceRequest.Costume_PutOff)]
	public async Task<MPServiceResult> Costume_PutOff(IMessage msg)
	{
		var response = await request<SPEAK_RES_PutOffItem>(msg);
		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.CostumePutOffFailed);

		return MPServiceResult.Success(response.Result);
	}
}
