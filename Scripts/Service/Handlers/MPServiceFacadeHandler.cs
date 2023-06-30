#define ONLY_STORE
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;

using Toast.Gamebase;

using Momo;

	using Data;
	using Contents;
using UnityEditor;

public partial class MPServiceFacadeHandler
{
	[ServiceHandler(MPServiceName.RestoreConnection)]
	public async Task<MPServiceResult> restoreConnection(string contents)
	{
		if (!NetworkClient.IsNetworkOffline)
		{
			if (!NetworkServiceHandler.IsSessionValid)
			{
				return await MPServiceFacade.Instance.Request(MPServiceName.Login, new Login()
				{
					LoginType = GamebaseAuthProvider.GUEST,
					NetworkRestoration = true
				});
			}
		}
		return MPServiceResult.Success();
	}

	[ServiceHandler(MPServiceName.Login, -1)]
	public async Task<MPServiceResult> initPlatform(string contents)
	{
        var initResult = await MPServiceFacade.Platform.Initialize();
        if (initResult.IsSuccess == false)
        {
            return MPServiceResult.Fail(MPServiceResultCode.GamebaseFailed);
        }

        return MPServiceResult.Success(contents);
	}

	[ServiceHandler(MPServiceName.Withdraw, showLoading: true)]
	public async Task<MPServiceResult> withdraw(string contents)
	{
		var result = await MPServiceFacade.Platform.Withdraw(false);
		return new MPServiceResult
		{
			Code = result.IsSuccess ? MPServiceResultCode.Success : MPServiceResultCode.WithdrawFailed,
			GamebaseErrorCode = result.ErrorCode,
			Contents = contents
		};
	}

	[ServiceHandler(MPServiceName.Login, -1, true)]
	public async Task<MPServiceResult> syncBeforeLogin(string contents)
	{
		// 게임 서버에 로그인 되어있으면 동기화 먼저 진행한다.
		// 동기화가 실패해도 걍 무시한다.
		if (NetworkServiceHandler.IsSessionValid)
		{
			Logger.Log($"[MPServiceFacade::Login] Synchronizing user gamedata: {MPServiceFacade.Platform.UserID}");
			//세션이 안맞으면 데이터 동기화가 필요할까?
			//await MPServiceFacade.Network.Request(NetworkServiceRequest.SyncAll, new DataSync { ForceToServer = false });
		}
		return MPServiceResult.Success();
	}
	[ServiceHandler(MPServiceName.Login, 0)]
	public async Task<MPServiceResult> login(string contents)
	{
		if (MPServiceFacade.Platform.IsInitialized)
		{
			string lastProvider = GamebaseAsync.GetLastLoggedInProvider();
			var loginData = JsonUtility.FromJson<Login>(contents);
			if (loginData.LoginType == string.Empty)
				return MPServiceResult.Fail(MPServiceResultCode.InvalidLoginType);
			
			Logger.Log($"[MPServiceFacade::Login] Logging-in with {lastProvider}");
			
			await MPServiceFacade.Platform.Login(loginData.LoginType);
		}
		return MPServiceResult.Success(contents);
	}


	[ServiceHandler(MPServiceName.Login, 3)]
	public async Task<MPServiceResult> syncUserDataAfterLogin(string loginContents)
	{
		var loginData = JsonUtility.FromJson<Login>(loginContents);
			
		bool syncDirtiesAlways = !string.IsNullOrEmpty(loginData.TransferUserID);

		loginData.Token = MPServiceFacade.Platform.Token;
		loginData.IsGuest = loginData.LoginType == GamebaseAuthProvider.GUEST;
		loginData.Version = ConstantValues.ApplicationVersion;
		loginData.OSType = MPServiceFacade.OSType;
		loginData.Language = STAR_LanguageType.LANG_KO;
		loginData.OSName = SystemInfo.operatingSystem;
		//loginData.friendsList = 
		loginData.FacebookId = "";

		string serverConfigVersion = null;
		int serverConfigRevision = 0;
		if (GamebaseController.IsLoggedIn)
		{
			NetworkClient.Instance.ResetTransaction();
			NetworkClient.Instance.SetUserNo(MPServiceFacade.Platform.UserID);
			NetworkClient.Instance.InitCommonURL(MPServiceFacade.Platform.GameServerAddress);

			IntroSceneHandler handler = SceneController.Instance.GetSceneHandler<IntroSceneHandler>("Intro");
			handler.SetProgress(0.25f);
			//로그인 패킷 요청
			var serverLoginResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.Login, loginData);
            if (!serverLoginResult.IsSuccess)
                return MPServiceResult.Fail(serverLoginResult.Code);
			handler.SetProgress(0.5f);
			// 게임 데이터 요청
			var gameDataResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetGameData, "{}");
            if (!gameDataResult.IsSuccess)
            	return MPServiceResult.Fail(gameDataResult.Code);
			// 파닉스 데이터 요청
			var phonicsDataResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetPhonicsData, "{}");
			if (!phonicsDataResult.IsSuccess)
				return MPServiceResult.Fail(phonicsDataResult.Code);
			handler.SetProgress(0.75f);
			// 유저 정보 요청
			var profileResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetProfile, "{}");
			if (!profileResult.IsSuccess)
				return MPServiceResult.Fail(profileResult.Code);
			handler.SetProgress(0.85f);
			loginData.UserID = MPServiceFacade.Platform.UserID;
			PlayerPrefs.SetString("LastLoginUserID", loginData.UserID);

			// 230215 - 주간, 월간 랭킹 결과 요청
			bool isWeekGet = true;	// true 일 경우에 받은거  false 일 경우에 안받은거
			bool isMonthGet = true; // true 일 경우에 받은거  false 일 경우에 안받은거
			NewMarkManager.Instance.RemoveNewMark(NewMarkManager.NewMarkType.NewMark_Ranking);    // 혹시 모르니 처음에 초기화를 해주고 시작한다.
			var rankResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_RankingPrize, "{}");
			if (rankResult.IsSuccess)
			{
				var rtnRankData = rankResult.message as SPEAK_RES_RankingPrize;

				// 개방형 사용자일 경우
				if (GameData.isOpenPublicLogin)
				{
					if (rtnRankData.rankingPrizes.Count > 1)
					{
						// 만약에 0번에 담겨온게 주간정보일경우
						if (rtnRankData.rankingPrizes[0].rankingCategory.Equals("W") || rtnRankData.rankingPrizes[0].rankingCategory.Equals("w"))
						{
							isWeekGet = rtnRankData.rankingPrizes[0].isGet;
							isMonthGet = rtnRankData.rankingPrizes[1].isGet;
						}
						// 반대일경우
						else
						{
							isWeekGet = rtnRankData.rankingPrizes[1].isGet;
							isMonthGet = rtnRankData.rankingPrizes[0].isGet;
						}
					}
					//else
					//	return MPServiceResult.Fail(MPServiceResultCode.SPEAK_RES_RankingPrizeFailed);
				}
				// 학생 코드 회원일 경우
				else
				{
					if (rtnRankData.rankingPrizes.Count > 1)
						isWeekGet = rtnRankData.rankingPrizes[0].isGet;
					//else
					//	return MPServiceResult.Fail(MPServiceResultCode.SPEAK_RES_RankingPrizeFailed);
				}
			}
			else    // rankResult.IsSuccess == false
			{
				Logger.LogError($"SPEAK_RES_RankingPrize Fail! ==> ErrCode : {rankResult.Code}");
				return MPServiceResult.Fail(rankResult.Code);
			}
			handler.SetProgress(0.1f);

			CheckStartNewRank(isWeekGet, isMonthGet);
            // 230215 - 주간, 월간 랭킹 결과 요청 여기까지
            // 230510 - 우편함 데이터 요청
            #region Mail
            {
                var mailListResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_Mailbox, new SPEAK_REQ_Mailbox());
				if (mailListResult.IsSuccess)
				{
					var mailListData = mailListResult.message as SPEAK_RES_Mailbox;

					GameData.Mails.readMailIdList();
					if (mailListData.mailboxes.Count > 0)
                    {
						GameData.mailDataList = mailListData.mailboxes;
						foreach (SPEAK_ST_Mailbox d in GameData.mailDataList)
						{
							if (GameData.Mails.isNewMail(d.mailNo) == true)
							{
								NewMarkManager.Instance.AddNewMark(NewMarkManager.NewMarkType.NewMark_mail);
								break;
							}
						}
					}	
				}
				else    // mailListResult.IsSuccess == false
				{
					Logger.LogError($"SPEAK_RES_Mailbox Fail! ==> ErrCode : {mailListResult.Code}");
					return MPServiceResult.Fail(mailListResult.Code);
				}
			}
            #endregion 
            // 230510 - 우편함 데이터 요청 여기까지

            var data = serverLoginResult.GetContents<Login>();
			serverConfigVersion = data.ConfigVersion;
			serverConfigRevision = data.ConfigRevision;
        }
        else
		{
			loginData.UserID = PlayerPrefs.GetString("LastLoginUserID", "");
			
		}

		//GameService.Inventory.ClearInven();
		

		//if (GamebaseController.IsLoggedIn)
		//{
		//	bool fetchGameConfig = false;
		//	if (string.IsNullOrEmpty(ctx.config_version))
		//	{
		//		fetchGameConfig = true;
		//	}
		//	else
		//	{
		//		var serverVersion = Contents.Version.FromString(serverConfigVersion);
		//		var clientVersion = Contents.Version.FromString(ctx.config_version);
		//		if (serverVersion.Major == clientVersion.Major &&
		//			serverVersion.Minor == clientVersion.Minor)
		//		{
		//			if (serverConfigRevision != ctx.config_revision)
		//				fetchGameConfig = true;
		//		}
		//	}
		//	if (fetchGameConfig)
		//	{
		//		//var loadConfigResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.LoadGameConfig, "{}");
		//		//if (loadConfigResult.IsSuccess && !string.IsNullOrEmpty(loadConfigResult.Contents))
		//		//{
		//		//	Config.load(loadConfigResult.Contents);
		//		//}
		//	}
		//}

	

		if (GamebaseController.IsLoggedIn)
		{
			//var push = (PushAgreement) PlayerPrefs.GetInt("PushAggrements", (int)PushAgreement.AdNightlyPushAllowed);
			//await MPServiceFacade.Platform.RegisterPushAgreements(push);

#if ONLY_STORE
			RegisterPush(true, false, false);
#endif
		}
		return MPServiceResult.Success(loginContents);
	}

	public void RegisterPush(bool pushEnabled, bool adAgreement, bool adAgreementNight)
	{
		GamebaseRequest.Push.PushConfiguration pushConfiguration = new GamebaseRequest.Push.PushConfiguration();
		pushConfiguration.pushEnabled = pushEnabled;
		pushConfiguration.adAgreement = adAgreement;
		pushConfiguration.adAgreementNight = adAgreementNight;

		// You should receive the above values to the logged-in user.

		//HGKIM 220714 - 푸시 옵션 추가함 => 포그라운드
		GamebaseRequest.Push.NotificationOptions options = new GamebaseRequest.Push.NotificationOptions
		{
			foregroundEnabled = true,
			priority = GamebaseNotificationPriority.HIGH
		};

		Logger.Log($"HGKIM || [GamebaseAsyncWrapper] => foregroundEnabled : {options.foregroundEnabled} / badgeEnabled : {options.badgeEnabled}");

		// HGKIM 220714 - 푸시 옵션 추가함
		Gamebase.Push.RegisterPush(pushConfiguration, options, (error) =>
		{
			if (Gamebase.IsSuccess(error))
			{
				Logger.Log("RegisterPush succeeded.");
			}
			else
			{
				Logger.Log(string.Format("RegisterPush failed. error is {0}", error));
			}
		});

		// HGKIM 220714 - 기존 소스 백업
		//Gamebase.Push.RegisterPush(pushConfiguration, (error) =>
		//{
		//	if (Gamebase.IsSuccess(error))
		//	{
		//		Debug.Log("RegisterPush succeeded.");
		//	}
		//	else
		//	{
		//		Debug.Log(string.Format("RegisterPush failed. error is {0}", error));
		//	}
		//});
	}

	private void CheckStartNewRank(bool isWeekGet = true, bool isMonthGet = true)
	{
		// 230215 - isWeekGet, isMonthGet 이 false 일 경우에 보상을 수령받지 않은 것 이기 때문에 랭킹정보가 갱신된걸로 판단하여 new 마크를 활성화 시킨다.
		if(isWeekGet.Equals(false) || isMonthGet.Equals(false))
		{
			Logger.Log("Ranking Reset!!");
			NewMarkManager.Instance.AddNewMark(NewMarkManager.NewMarkType.NewMark_Ranking);
		}
	}

	[ServiceHandler(MPServiceName.Login, 4)]
	public async Task<MPServiceResult> updateLobby(string contents)
	{
		var loginData = JsonUtility.FromJson<Login>(contents);
		if (SceneController.Lobby != null && !loginData.DoNotUpdateLobby)
		{
			//todo jin
			//if (!loginData.NetworkRestoration)
			//	MPServiceFacade.Game.LastStagePlayResult = null;
			var refreshLobbyTask = new TaskCompletionSource<bool>();
			//todo jin
			//SceneController.Lobby?.RefreshLobby(refreshLobbyTask.SetResult);
			//else

			refreshLobbyTask.SetResult(true);
			await refreshLobbyTask.Task;
		}
		return MPServiceResult.Success();
	}
	
    [ServiceHandler(MPServiceName.AccountCode)]
    public async Task<MPServiceResult> accountCode(string contetsJson)
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.AccountCode, contetsJson);
        return result;
    }

	[ServiceHandler(MPServiceName.Key)]
	public async Task<MPServiceResult> password(string contetsJson)
	{
		var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.Key, contetsJson);
		return result;
	}

    [ServiceHandler(MPServiceName.GameStart)]
    public async Task<MPServiceResult> gameStart(string contetsJson)
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.GameStart, contetsJson);
        return result;
    }

	//게임종료에서는 종료 패킷을 만들자..
}
