using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UniRx;
using UniRx.Async;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

public partial class GameData
{
	public static UserInfo User { get; private set; } = new UserInfo();
	public static ConfigInfo Config { get; private set; } = new ConfigInfo();
	public static UserPlayInfo PlayInfo { get; private set; } = new UserPlayInfo();
	public static SchoolTalkJoinRoom schoolTalkJoinRoom { get; private set; } = new SchoolTalkJoinRoom();

	public static Dictionary<int, SPEAK_ST_Item> masterItem = new Dictionary<int, SPEAK_ST_Item>();
	public static GameData.Study.ScanIt selectScanitCate;
	public static Dictionary<int, ScanItContent> scanItContent = new Dictionary<int, ScanItContent>();
	public static Inventory inventory = new Inventory();
	public static CostumeInfo costumeinfo = new CostumeInfo();
	public static ImageCapture imageCapture = new ImageCapture();

	public static Dictionary<int, Stamp> StampDatas = new Dictionary<int, Stamp>();
	private static List<SPEAK_ST_USER_ATTEND> Attends = null;
	public static List<SPEAK_ST_Tutorial> openTutorials = null;
	public static SPEAK_ST_USER_ATTEND GetAttend(int index) => Attends.SafeGetValue(index);
	public static bool HasAttendData() => Attends.Count != 0;
	public static Dictionary<string, Talker> Talkers = null;

	//2.4.6 받은 우편내역
	public static mailStorage Mails = new mailStorage();

	private static Dictionary<DayOfWeek, RecommendScript> recommendScripts = new Dictionary<DayOfWeek, RecommendScript>()
	{
		{
			DayOfWeek.Sunday,	//없음
			new RecommendScript
			{
				korDesc = new List<string> { "자유롭게 영어 활동을 해보세요!" },
				engDesc = new List<string> { "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_06" }
			}
		},
		{
			DayOfWeek.Monday,	//토픽
			new RecommendScript
			{
				korDesc = new List<string> { "오늘은 토픽 월드를 함께 해볼까요?", "자유롭게 영어 활동을 해보세요" },
				engDesc = new List<string> { "Shall we visit TOPIC WORLD?", "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_03", "T_main_06" }
			}
		},
		{
			DayOfWeek.Tuesday,	//레츠톡
			new RecommendScript
			{
				korDesc = new List<string> { "오늘은 렛츠톡을 함께 해볼까요?", "자유롭게 영어 활동을 해보세요" },
				engDesc = new List<string> { "Shall we visit LET'S TALK?", "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_05", "T_main_06" }
			}
		},
		{
			DayOfWeek.Wednesday,	//토픽
			new RecommendScript
			{
				korDesc = new List<string> { "오늘은 토픽 월드를 함께 해볼까요?", "자유롭게 영어 활동을 해보세요" },
				engDesc = new List<string> { "Shall we visit TOPIC WORLD?", "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_03", "T_main_06" }
			}
		},
		{
			DayOfWeek.Thursday,	//스피킹
			new RecommendScript
			{
				korDesc = new List<string> { "오늘은 스피킹을 함께 해볼까요?", "자유롭게 영어 활동을 해보세요" },
				engDesc = new List<string> { "Shall we visit SPEAKING?", "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_04", "T_main_06" }
			}
		},
		{
			DayOfWeek.Friday,	//토픽
			new RecommendScript
			{
				korDesc = new List<string> { "오늘은 토픽 월드를 함께 해볼까요?", "자유롭게 영어 활동을 해보세요" },
				engDesc = new List<string> { "Shall we visit TOPIC WORLD?", "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_03", "T_main_06" }
			}
		},
		{
			DayOfWeek.Saturday,	//
			new RecommendScript
			{
				korDesc = new List<string> { "자유롭게 영어 활동을 해보세요!" },
				engDesc = new List<string> { "Have fun and enjoy the activities!" },
				soundFile = new List<string> { "T_main_06" }
			}
		},
	};

	#region user info
	[System.Serializable]
	public class UserInfo
	{
		// STAR_ST_Profile
		public string userNo;
		// STAR_ST_LcmsUserInfo
		public string loginId;      // stdetCd (01팽수35015)	// 학생 코드
		public int lcmsUserNo;      // stdetNo (9000016)	// 학생 번호
									//public string schoolBoardCode; 	// schBrdCd;	// 교육청 코드
									//public string schoolBoardName; 	// schBrdNm;	// 교육청 명
									//public int schoolNo; 		// schNo;	// 학교 번호
		public string schoolName;   // schNm;	// 학교 명
		public string gradeCode;	// semeCd;	// 학년 코드	// HGKIM 220727 - 주석처리된 것 해제
		public string gradeName;    // semeNm;	// 학년 명
		public int classNo;         // clsNo 	// 반번호 2021.11.10 미사용 결정
		//public string classNm;         // clsNm 	//반 이름
									   //public string publisherCode; 	// pblCd;	// 출판사 코드
									   //public string publisherName; 	// pblNm;	// 출판사 명
		public string userName;     // indvNickNm;	// 개인 별명
		public int imageNo; // 이미지 번호
							//public string sexCode;		// sexCd;	// 성별 (M: 남자, F: 여자)
							//public string nickName; 	// nickNm;	// 닉네임- 선생님이 셋팅
		public bool agreePrivacy;   // indvInfoAgYn	// 개인정보동의 여부
	}

	public static bool isOpenPublicLogin = false;    // HGKIM 221219 - 개방형 로그인 여부 // 230101 - 원래 GetSet 형태로 하려고 했는데 public 로 변경
	public static bool isMobileWebviewOpend = false;	// HGKIM 230126 - 웹뷰 열렸는지 체크하는용도

	public static long sessionId;	// HGKIM 221226 - 섹션 아이디 저장(로그인 위해)
	public static string ebsId;     // HGKIM 221226 - ebs 아이디 저장
	public static bool isOpenLoginFail;	// HGKIM 221231 - 웹뷰 로그인은 성공했는데 그 이후 통신에서 실패할경우 true

	/// <summary>
	/// 개방형 로그인 정보들 초기화 시킴
	/// </summary>
	public static void OpenLoginDataInit()
	{
		isOpenPublicLogin = false;
		sessionId = 0;
		ebsId = "";
		isOpenLoginFail = false;

		Logger.Log("OpenLogin Data Init!!!!");
	}

	public static void SetUserInfo(string userNo, SPEAK_ST_LcmsUserInfo lcmsInfo)
	{
		GameData.User.userNo = userNo;

		GameData.User.loginId = lcmsInfo.stdetCd;
		GameData.User.lcmsUserNo = lcmsInfo.stdetNo;
		GameData.User.schoolName = lcmsInfo.schNm;
		GameData.User.gradeCode = lcmsInfo.grdCd;		// HGKIM 220727 - 학년 코드 오늘의 단어 사용시에 필요해서 저장하도록 수정
		GameData.User.gradeName = lcmsInfo.grdNm;
		GameData.User.classNo = lcmsInfo.clsNo;
		//GameData.User.classNm = lcmsInfo.clsNm;
		GameData.User.userName = lcmsInfo.indvNickNm;
		GameData.User.agreePrivacy = lcmsInfo.indvInfoAgYn.Equals("Y");
		PlayerPrefsManager.SetPrivateKey(GameData.User.lcmsUserNo.ToString());
	}

	// HGKIM 220805 - 진급 할 떄에 쓰이는 UserInfo Data
	public class UserInfo_Real
	{
		public string gradeNo;	// 학년
		public string classNo;	// 반번호
		public int number;	// 번호
		public string userName;	// 실제 이름
	}

	public static UserInfo_Real UserInfoReal { get; private set; } = new UserInfo_Real();

	public static void SetRealUserInfo(UserInfo_Real info)
	{
		UserInfoReal.gradeNo = info.gradeNo;	// 학년
		UserInfoReal.classNo = info.classNo;	// 반번호
		UserInfoReal.number = info.number;		// 번호
		UserInfoReal.userName = info.userName;	// 실제 이름
	}

	/// <summary>
	/// 진급 성공시 정보 업데이트용
	/// </summary>
	/// <param name="info">진급 시도시 썼던 데이터</param>
	public static void UpdatedUserInfo(UserInfo_Real info)
	{
		GameData.User.gradeName = String.Format($"{GameData.UserInfoReal.gradeNo}학년");
		GameData.User.gradeCode = ChangedGradeNameToGradeCode(info.gradeNo);
		GameData.User.classNo = int.Parse(info.classNo);

		Utility.Firebase_SetUserProperty("grade_name", GameData.User.gradeName);
		Utility.Firebase_SetUserProperty("class_name", GameData.User.classNo.ToString());
	}

	public static string ChangedGradeNameToGradeCode(string grade)
	{
		string rtnStr = "";
		Logger.Log($"HGKIM || CheckGradeCode || grade : {grade}");

		switch (grade)
		{
			case "3학년":
			case "3":
				rtnStr = "004001";
				break;
			case "4학년":
			case "4":
				rtnStr = "004002";
				break;
			case "5학년":
			case "5":
				rtnStr = "004003";
				break;
			case "6학년":
			case "6":
				rtnStr = "004004";
				break;

			default:
				// 기타 예외처리를 해줘야 한다.
				Logger.Log("HGKIM || CheckGradeCode || grade Error!!!");
				break;
		}

		return rtnStr;
	}
	#endregion

	#region ConfigInfo
	[System.Serializable]
	public class ConfigInfo
	{
		[System.Serializable]
		public class Header
		{
			public bool isSuccessful;
			public int resultCode;
			public string resultMessage;
		}

		[System.Serializable]
		public class Launching
		{
			[System.Serializable]
			public class Client
			{
				[System.Serializable]
				public class Url_Dev
				{
					public string audioUrl;
					public string ttsUrl;
					public string imageUrl;
					public string speechUrl;
					public string appKey_smartDL;
					public List<int> content_OnOff;
                    public List<string> content_WhiteList;
                }
				public Url_Dev dev;

				[System.Serializable]
				public class Url_QA
				{
					public string audioUrl;
					public string ttsUrl;
					public string imageUrl;
					public string speechUrl;
					public string appKey_smartDL;
                    public List<int> content_OnOff;
                    public List<string> content_WhiteList;
                }
				public Url_QA qa;

				[System.Serializable]
				public class Url_Review
				{
					public string audioUrl;
					public string ttsUrl;
					public string imageUrl;
					public string speechUrl;
					public string appKey_smartDL;
                    public List<int> content_OnOff;
                    public List<string> content_WhiteList;
                }
				public Url_Review review;
				[System.Serializable]
				public class Url_Real
				{
					public string audioUrl;
					public string ttsUrl;
					public string imageUrl;
					public string speechUrl;
					public string appKey_smartDL;
                    public List<int> content_OnOff;
					public List<string> content_WhiteList;
                }
				public Url_Real real;

				public string AudioUrl()
				{
					switch (MPServiceFacade.Platform.MaintenanceCode)
					{
						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
							return real.audioUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.audioUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return qa.audioUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.audioUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.audioUrl;

						default:
							return dev.audioUrl;
					}
				}

				public string TTSUrl()
				{
					switch (MPServiceFacade.Platform.MaintenanceCode)
					{
						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
							return real.ttsUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.ttsUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return qa.ttsUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.ttsUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.ttsUrl;

						default:
							return dev.ttsUrl;
					}
				}

				public string ImageUrl()
				{
					switch (MPServiceFacade.Platform.MaintenanceCode)
					{
						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
							return real.imageUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.imageUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return qa.imageUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.imageUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.imageUrl;

						default:
							return dev.imageUrl;
					}
				}

				public string SpeechUrl()
				{
					switch (MPServiceFacade.Platform.MaintenanceCode)
					{
						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
							return real.speechUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.speechUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return qa.speechUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.speechUrl;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.speechUrl;

						default:
							return dev.speechUrl;
					}
				}

				public string Key_SmartDL()
				{
					switch (MPServiceFacade.Platform.MaintenanceCode)
					{
						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
							return real.appKey_smartDL;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.appKey_smartDL;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return qa.appKey_smartDL;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.appKey_smartDL;

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.appKey_smartDL;

						default:
							return dev.appKey_smartDL;
					}
				}

                public bool IsContentOpen(GameContent_Type type)
                {
                    switch (MPServiceFacade.Platform.MaintenanceCode)
                    {
                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
                            return Convert.ToBoolean( real.content_OnOff[(int)type]);

                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return Convert.ToBoolean(dev.content_OnOff[(int)type]);
							
                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
							return Convert.ToBoolean(qa.content_OnOff[(int)type] );

                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return Convert .ToBoolean(review.content_OnOff[(int)type]);

                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return Convert.ToBoolean(real.content_OnOff[(int)type]);

                        default:
							return Convert.ToBoolean(dev.content_OnOff[(int)type]);
                    }
                }

                public bool IsWhiteList_Member(string studentCode)
                {
                    switch (MPServiceFacade.Platform.MaintenanceCode)
                    {
                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
                            return real.content_WhiteList.Contains(studentCode);

                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
							return dev.content_WhiteList.Contains(studentCode);

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
                            return qa.content_WhiteList.Contains(studentCode);

                        case Toast.Gamebase.GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
							return review.content_WhiteList.Contains(studentCode);

						case Toast.Gamebase.GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
							return real.content_WhiteList.Contains(studentCode);

						default:
							return dev.content_WhiteList.Contains(studentCode);
					}
                }
            }

			public Client client;
		}

		public Header header;
		public Launching launching;
	}
	public static void SetConfig(ConfigInfo configInfo)
	{
		Config = configInfo;
	}
	#endregion


	public class Inventory
	{
		public Dictionary<int, Item> itemInfo = new Dictionary<int, Item>();
		public Item this[int itemId]
		{
			get
			{
				Item item = null;
				if (!itemInfo.TryGetValue(itemId, out item))
				{
					item = new Item();
					itemInfo.Add(itemId, item);
				}
				return item;
			}
		}
		public Item this[ItemId itemId]
		{
			get
			{
				int keyId = (int)itemId;
				Item item = null;
				if (!itemInfo.TryGetValue(keyId, out item))
				{
					item = new Item();
					itemInfo.Add(keyId, item);
				}
				return item;
			}
		}

		public IObservable<int> ObserveItemCount(ItemId itemId)
		{
			return this[(int)itemId].CountRx;
		}

		public void ParseInven(List<SPEAK_ST_ItemInven> items)
		{
			foreach (var item in items)
			{
				Item myItem = itemInfo[item.no];
				myItem.Count = item.itmCount;
			}
		}

		public void AddItem(List<SPEAK_ST_ItemInven> items)
		{
			foreach (var item in items)
			{
				Item myItem = itemInfo[item.no];
				myItem.Count += item.itmCount;
			}
		}

		public void SubItem(GameData.ItemId itemId, int count)
		{
			Item myItem = null;
			itemInfo.TryGetValue((int)itemId, out myItem);
			if (null == myItem)
			{
				return;
			}

			myItem.Count = Math.Max(0, myItem.Count - count);
		}

		public void SubItem(List<SPEAK_ST_ItemInven> items)
		{
			foreach (var item in items)
			{
				Item myItem = itemInfo[item.no];
				myItem.Count = Math.Max(0, myItem.Count - item.itmCount);
			}
		}

		public bool SafeAddItem(ItemId itemId, int amount)
		{
			Item myItem = this[itemId];

			int resultCount = myItem.Count + amount;
			if (resultCount < 0)
				return false;

			myItem.Count = resultCount;
			Logger.Log($"Inventory | SafeAddItem +{amount} = {resultCount}");
			return true;
		}

		/// <summary>
		/// 230414 - 참치캔을 통째로 갱신시킨다.
		/// </summary>
		public void SafeRefreshItem(int amount)
		{
			Item myItem = this[GameData.ItemId.Coin];

			if(amount < 0)
			{
				Logger.LogError("갯수가 음수다 에러!!!!!");
				return;
			}

			myItem.Count = amount;
			Logger.Log($"Inventory | SafeRefreshItem  = {amount}");
		}
	}


	public class Item
	{
		private SecReactiveProperty<int> count = new SecReactiveProperty<int>(0);

		public IObservable<int> CountRx
		{
			get
			{
				return Observable.CombineLatest(count)
					.Select(l => l.Select(v => v.Value).Sum());
			}
		}

		public int Count { get { return count.Value; } set { count.Value = value; } }
	}

	#region Stamp
	public static void ParseStampData(List<SPEAK_ST_Stamp> stampList)
	{
		StampDatas.Clear();
		for (int i = 0; i < stampList.Count; i++)
		{
			Stamp stamp = new Stamp(stampList[i]);
			StampDatas.Add(stamp.no, stamp);
		}
	}

	public static void SetHasStamp()
	{
		bool isFirstLogin = PlayerPrefsManager.GetInt("FirstLogin") == 1 ? true : false;
		if (isFirstLogin)
		{
			PlayerPrefsManager.SetInt("FirstLogin", 0);
			NewMarkManager.Instance.AddHasStamp(0, false);
		}
		GameData.StampDatas[0].isHasStamp = true;

		// World reward stamp 
		foreach (var item in GameData.Study.Worlds)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.World);
		}
		// Theme reward stamp 
		foreach (var item in GameData.Study.Themes)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.Theme);
		}
		// Speaking reward stamp 
		foreach (var item in GameData.Study.Speakings)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.Speaking);
		}
		// LetsTalk reward stamp 
		foreach (var item in GameData.Study.LetsTalks)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.LetsTalk);
		}
		// ScanIt reward stamp 
		foreach (var item in GameData.Study.ScanIts)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.ScanIt);
		}
		// Phonics reward stamp 
		foreach (var item in GameData.Study.Phonicses)
		{
			_SetHasStamp(isFirstLogin, item, Stamp.StampType.Phonics);
		}
	}

	private static void _SetHasStamp(bool isFirstLogin, Study.BigCategory bigCategory, Stamp.StampType stampType)
	{
		GameData.StampDatas[bigCategory.rewardStampNo].stampType = stampType;

		if (bigCategory.Finish)
		{
			if (isFirstLogin)
			{
				NewMarkManager.Instance.AddHasStamp(bigCategory.rewardStampNo, false);
			}
			else
			{
				GameData.StampDatas[bigCategory.rewardStampNo].isHasStamp = NewMarkManager.Instance.IsHasStamp(bigCategory.rewardStampNo);
			}
		}
		else
		{
			NewMarkManager.Instance.RemoveNewMark(NewMarkManager.NewMarkType.NewMark_StampItem, bigCategory.rewardStampNo);
		}
	}

	public class Stamp
	{
		public enum StampType
		{
			None = 0,
			World,
			Theme,
			Speaking,
			LetsTalk,
			ScanIt,
			SchoolTalk,
			Phonics
		}

		public int no { get; private set; }
		public string stampName { get; private set; }
		public string desc { get; private set; }
		public string filename { get; private set; }
		public string filepath { get; private set; }
		public bool isHasStamp { get; set; }

		public StampType stampType { get; set; }

		public Stamp(SPEAK_ST_Stamp data)
		{
			no = data.no;
			stampName = data.stampName;
			desc = data.desc;
			filename = data.filename;
			filepath = data.filepath;
		}
	}
	#endregion

	#region Attend

	public enum eDays
	{
		ONE = 100001,
		TWO = 100002,
		THREE = 100003,
		FOUR = 100004,
		FIVE = 100005,
		SIX = 100006,
		SEVEN = 100007,
	}

	public static void ParseAttend(List<SPEAK_ST_USER_ATTEND> attends)
	{
		Attends = attends;
	}
	#endregion

	// 230317 - 메일 푸시 받았는지 여부
	#region MailPushData
	public static bool isMailPush = false;
	#endregion
	// 230510 - 메일함 데이터 리스트
	#region MailListData

	public static List<SPEAK_ST_Mailbox> mailDataList = new List<SPEAK_ST_Mailbox>();
	#endregion

	// HGKIM 220718 - 오늘의 영단어 푸시를 받았는지 여부
	#region TodaysWordData
	// 220726 - 추가 : 메인화면 팝업에서 버튼을 눌러도 이걸 켜준다.
	public static bool isTodayWordPush = false;
	public static bool isTodayWordClear = false;

	public static TodaysWord todaysWord { get; private set; } = new TodaysWord();

	public class TodaysWord
	{
		public int pnNum;		// 고유번호
		public string date;		// 날자(MMDD)
		public string title;	// 타이틀
		public string wdText;	// 단어
		public string wdTrans;	// 단어 해석
		public string wdAudio;	// 단어 오디오
		public string wdImage;	// 단어 이미지
		public string phrText;	// 문장
		public string phrTrans;	// 문장 해석
		public string phrAudio; // 문장 오디오
		public string studyYn;	// 학습 완료 여부 (Y/N)
	}

	/// <summary>
	/// HGKIM 220816 - 로그아웃시 데이터가 남아있는거 같아 초기화 시키는 것을 만들었다.
	/// </summary>
	public static void ResetTodaysWordData()
	{
		Logger.Log("HGKIM || Init TodaysWordData");
		todaysWord = new TodaysWord();
		isTodayWordClear = false;
	}

	public static void SetTodaysWordData(SPEAK_ST_PushNotice noticeData)
	{
		todaysWord.pnNum = noticeData.pnNo;
		todaysWord.date = noticeData.pnDate;
		todaysWord.title = noticeData.pnTitle;
		todaysWord.wdText = noticeData.pnWdText;
		todaysWord.wdTrans = noticeData.pnWdTrans;
		todaysWord.wdAudio = noticeData.pnWdAudio;
		todaysWord.wdImage = noticeData.pnWdImage;
		todaysWord.phrText = noticeData.pnPhrText;
		todaysWord.phrTrans = noticeData.pnPhrTrans;
		todaysWord.phrAudio = noticeData.pnPhrAudio;
		todaysWord.studyYn = noticeData.studyYn;

		if(todaysWord.studyYn.Equals("Y") || todaysWord.studyYn.Equals("y"))
		{
			ClearTodaysWord();
		}
	}

	public static void ClearTodaysWord()
	{
		Logger.Log("HGKIM || ClearTodaysWord || isTodayWordClear!!!!");
		isTodayWordClear = true;
	}
	#endregion
	// HGKIM 220718 - 오늘의 영단어 데이터 여기까지

	#region LobbyRecommend

	public class RecommendScript
	{
		public List<string> korDesc;
		public List<string> engDesc;
		public List<string> soundFile;

		private int korIndex = 0;
		private int engIndex = 0;
		private int soundIndex = 0;

		public void ClearIndex()
		{
			korIndex = 0;
			engIndex = 0;
			soundIndex = 0;
		}

		public string GetKor()
		{
			if (korIndex >= korDesc.Count)
			{
				return string.Empty;
			}
			return korDesc[korIndex];
		}

		public string NextKor()
		{
			korIndex++;
			return GetKor();
		}

		public string GetEng()
		{
			if (engIndex >= engDesc.Count)
			{
				return string.Empty;
			}
			return engDesc[engIndex];
		}

		public string NextEng()
		{
			engIndex++;
			return GetEng();
		}

		public string GetSound()
		{
			if (soundIndex >= soundFile.Count)
			{
				return string.Empty;
			}
			return soundFile[soundIndex];
		}
		public string NextSound()
		{
			soundIndex++;
			return GetSound();
		}
	}

	public static RecommendScript GetRecommendScript(DayOfWeek dayofweek)
	{
		if (recommendScripts.ContainsKey(dayofweek) == false)
		{
			return null;
		}
		return recommendScripts[dayofweek];
	}

	public class RecommendData
	{
		public string title;
		public string leftTitle;
		public string desc;
		public string imagePath;
		public string type;
		public int targetNo;
	}

	private static Dictionary<string, RecommendData> RecommendDatas = new Dictionary<string, RecommendData>();
	public static RecommendData GetRecommendData(string educationCode) => RecommendDatas.SafeGetValue(educationCode);

	public static void RefreshRecommendData()
	{
		RecommendDatas.Clear();

		// world
		Study.Topic topic = PlayInfo.currentTopic;
		Study.World world = PlayInfo.currentWorld;
		RecommendDatas.Add(EducationCode.World, new RecommendData
		{
			title = "토픽월드",
			leftTitle = $"TOPIC {topic.stage}",
			desc = topic.name,
			imagePath = world.imagePath,
			targetNo = topic.no,
			type = EducationCode.World
		});


		//레츠톡과 스피킹은 동일한 UI 처리
		// letstalk
		Study.LetsTalk letsTalk;
		List<Study.LetsTalk> unLockeds = Study.LetsTalks.FindAll(l => l.Locked == false);
		if (unLockeds.Count > 0)
		{
			letsTalk = unLockeds[UnityEngine.Random.Range(0, unLockeds.Count - 1)];
			RecommendDatas.Add(EducationCode.LetsTalk, new RecommendData
			{
				title = "렛츠톡",
				leftTitle = letsTalk.name,
				desc = letsTalk.desc,
				imagePath = letsTalk.imagePath,
				targetNo = letsTalk.episodes[0].no,
				type = EducationCode.LetsTalk
			}); ;
		}

		Study.Speaking speaking;
		List<Study.Speaking> doings = Study.Speakings.FindAll(s => s.Finish == false);
		if (doings.Count > 0)
		{
			speaking = doings[UnityEngine.Random.Range(0, doings.Count - 1)];
			RecommendDatas.Add(EducationCode.Speaking, new RecommendData
			{
				title = "스피킹",
				leftTitle = speaking.name,
				desc = speaking.desc,
				imagePath = speaking.imagePath,
				targetNo = speaking.scenes[0].no,
				type = EducationCode.Speaking
			});
		}
		//UnityEngine.Debug.Log($"world = {GetRecommendData(EducationCode.World).title}, letstalk = {GetRecommendData(EducationCode.LetsTalk).title}");
	}

	#endregion

	#region ItemData
	public static void ParseItemData(List<SPEAK_ST_Item> items)
	{

		//전체 아이템 

		masterItem.Clear();

		foreach (var item in items)
		{
			masterItem.Add(item.no, item);
		}
	}

	public static SPEAK_ST_Item GetMasterItem(int itemId)
	{
		if (masterItem.ContainsKey(itemId) == false)
		{
			return null;
		}
		return masterItem[itemId];
	}

	public static void SetInven(List<SPEAK_ST_ItemInven> itemInvens)
	{
		inventory.ParseInven(itemInvens);
	}

	public static void SetTutorial(List<SPEAK_ST_Tutorial> tutorials)
	{
		openTutorials = tutorials;
	}

	public static void AddTutorial(SPEAK_ST_Tutorial tutorial)
	{
		openTutorials.Add(tutorial);
	}

	public static bool IsCompleteTutorial(STAR_TutorialType tutorial)
	{
		int count = openTutorials.Where(p => p.no == (int)tutorial).Count();
		return count >= 1;
	}

	#endregion

	#region schooltalk
	public class SchoolTalkJoinRoom
	{
		public int roomNo;
		public SPEAK_RES_SchoolTalk_Conversation conversation;
	}
	#endregion

	#region scanit
	public class ScanItContent
	{
		public SPEAK_ST_Content content;
		public bool isHave;
	}

	public static void ParseScanIt(SPEAK_RES_ScanItStart scanIt)
	{
		scanItContent.Clear();
		int haveCnt = 0;
		foreach (var item in scanIt.contents)
		{
			ScanItContent data = new ScanItContent();
			data.content = item;
			data.isHave = scanIt.wordRecords.Contains(data.content.contentNo);
			if (data.isHave)
				++haveCnt;
			scanItContent.Add(item.contentNo, data);
		}

		Study.ScanIt scanit = Study.FindScanIt(selectScanitCate.no);
		scanit.SetRecord(haveCnt == scanIt.contents.Count ? Finish.YES : Finish.NO, haveCnt, scanIt.contents.Count);
	}
	#endregion

	#region -- user play info --------------------------------------------------
	public class UserPlayInfo
	{
		#region -- start & current info --------------------------------------------------

		public Study.World startWorld { get; private set; }
		public Study.World currentWorld { get; private set; }
		public Study.Topic currentTopic { get; private set; }
		public Study.Activity currentActivity { get; private set; }

		public void ParsePlayInfo(SPEAK_ST_UserGameInfo info)
		{
			// start world
			startWorld = Study.FindWorld(info.startWorldNo);
			if (startWorld == null)
				Logger.LogError($"ParsePlayInfo | Invalid startWorldNo = {info.startWorldNo}");

			// current activity
			var activity = Study.FindActivity(info.currentWorldActivityNo);
			if (activity == null)
			{
				Logger.LogError($"ParsePlayInfo | Invalid currentWorldActivityNo = {info.currentWorldActivityNo}");

				// set start world activity
				if (startWorld != null)
					activity = startWorld.topics[0].activitys[0];
				else
				{
					startWorld = Study.Worlds[0];
					activity = startWorld.topics[0].activitys[0];
				}
			}
			else if (activity.worldNo != info.currentWorldNo || activity.topicNo != info.currentTopicdNo)
			{
				Logger.LogError($"Invalid Current PlayInfo~!  " +
					$"worldNo:{activity.worldNo} != {info.currentWorldNo} || topicNo:{activity.topicNo} != {info.currentTopicdNo}");
			}

			SetCurrentActivity(activity);
		}

		public void ResetCurrentActivity()
		{
			SetCurrentActivity(currentActivity);
			Logger.Log($"<color=red>startWorldNo:{PlayInfo.startWorld.no} currentWorldNo:{PlayInfo.currentWorld.no} currentTopicdNo:{PlayInfo.currentTopic.no} currentWorldActivityNo:{PlayInfo.currentActivity.no}</color>");
		}

		private void SetCurrentActivity(Study.Activity inputActivity)
		{
			var activity = GetNextPlayActivity(inputActivity);

			if (inputActivity != activity)
				Logger.Log($"UserPlayInfo | SetCurrentActivity reset stage={inputActivity.stage} >> {activity.stage}");

			currentActivity = activity;
			currentWorld = Study.FindWorld(currentActivity.worldNo);
			currentTopic = Study.FindTopic(currentActivity.topicNo);
		}

		public Study.Activity GetNextPlayActivity(Study.Activity inputActivity)
		{
			var activity = inputActivity;

			while (activity.Finish)
			{
				if (activity.Next == null)
					break;
				activity = activity.Next;
			}
			return activity;
		}
		#endregion

		#region -- highestStage --------------------------------------------------

		/// <summary>
		/// 학습한 stage 중 가장 높은 stage
		/// </summary>
		public int highestStage;

		/// <summary>
		/// 학습 가능한 가장 높은 stage
		/// </summary>
		public int playableHighestStage
		{
			get
			{
				int stage = PlayInfo.highestStage + 1;

				Study.Topic topic = startWorld.topics[0];
				if (stage < topic.activitys[0].stage)
					stage = topic.activitys[0].stage;

				if (stage > Study.World.MaxStage)
					stage = Study.World.MaxStage;

				return stage;
			}
		}

		public void FinishStage(Study.Activity activity)
		{
			if (!activity.Finish)
				return;

			// set highestStage
			if (highestStage < activity.stage)
			{
				highestStage = activity.stage;
				Logger.Log($"UserPlayInfo | <color=white>set highestStage</color> : {highestStage}");
			}

			// set current 
			Study.Activity nextActivity = activity.Next ?? activity;
			SetCurrentActivity(nextActivity);

			Logger.Log($"UserPlayInfo | <color=white>set current</color> world={nextActivity.worldNo} topic={nextActivity.topicNo} activity={nextActivity.no}");
		}
		#endregion

		#region -- check open world for letstalk --------------------------------------------------

		public bool OpenWorld(int worldNo)
		{
			Study.World world = Study.FindWorld(worldNo);
			Study.Topic topic = world?.topics[0] ?? null;
			if (topic != null)
			{
				if (topic.activitys[0].stage <= playableHighestStage)
				{
					Logger.Log($"OpenWorld? {worldNo} true");
					return true;
				}
			}

			Logger.Log($"OpenWorld? {worldNo} false");
			return false;
		}
		#endregion
	}

	public static void SetUserGameInfo(SPEAK_ST_UserGameInfo info)
	{
		PlayInfo.ParsePlayInfo(info);
		GameData.User.imageNo = info.imgIdx;

		Logger.Log($"<color=white>startWorldNo:{PlayInfo.startWorld.no} lastWorldNo:{PlayInfo.currentWorld.no} lastTopicdNo:{PlayInfo.currentTopic.no} lastWorldActivityNo:{PlayInfo.currentActivity.no}</color>");
	}
	#endregion

	#region TalkerInfo
	[System.Serializable]
	public class Talker
	{
		public string code;    // cdInsV : 코드 인스턴스 값

		public string codeName;       // cdInsNm : 코드 인스턴스 명
		public string codeNameEng;    // cdInsEngNm : 코드 인스턴스 영문명

		public string filename;    // 첨부 파일 저장명
		public string filepath;    // 첨부 파일 경로

		public string imagePath => filepath + filename;

		public const string Chatbot = "010001";     // pengsu
		public const string Answer = "010002";        // user (대화 한 내역 전송할 때 펭수인지 유저인지 구분하기 위한 구분값) // 취약성 이슈로 변수명 변경  22.06.28
	}

	public static void ParseTalkerInfo(List<SPEAK_ST_TalkerCode> talkerCodes)
	{
		if (Talkers == null)
			Talkers = new Dictionary<string, Talker>();
		Talkers.Clear();

		for (int idx = 0; idx < talkerCodes.Count; ++idx)
		{
			SPEAK_ST_TalkerCode talkerData = talkerCodes[idx];
			Talker talker = new Talker();
			talker.code = talkerData.code;
			talker.codeName = talkerData.codeName;
			talker.codeNameEng = talkerData.codeNameEng;
			talker.filename = talkerData.filename;
			talker.filepath = talkerData.filepath;
			Talkers.Add(talker.code, talker);

			//Logger.Log($"ParseTalkerInfo  [{talker.code}] {talker.codeName} {talker.codeNameEng} {talker.filename}");
		}
	}
	#endregion

	#region -- user costume info --------------------------------------------------

	public class CostumeInfo
	{
		private static Dictionary<string, Costume_ItemData> costume_ItemTable = null;       //KeyValue<itemCode, ItemData>
		private static List<Costume_ItemData> costume_OwnedList = null;

		private static Dictionary<CostumeCategory, Costume_ItemData> costume_Equipped_ItemData = null;
		private static Dictionary<CostumeCategory, Costume_ItemData> costume_Preview_ItemData = null;

		private static Dictionary<CostumeCategory, Costume_ProductData> costume_Cart = null;
		private static Dictionary<string, Costume_ProductData> costume_ProductList = null;
		private static List<string> costume_SeasonItemCode = null;

		public List<Costume_ItemData> OwnedList { get => costume_OwnedList; }
		public Dictionary<CostumeCategory, Costume_ProductData> Cart => costume_Cart;
		public Dictionary<string, Costume_ProductData> ProductList { get => costume_ProductList; }
		public List<string> SeasonItemCode { get => costume_SeasonItemCode; }

		public CostumeInfo()
		{

			costume_ItemTable = new Dictionary<string, Costume_ItemData>(); //KeyValue<itemCode, ItemData>
			costume_OwnedList = new List<Costume_ItemData>();
			costume_Equipped_ItemData = new Dictionary<CostumeCategory, Costume_ItemData>();
			foreach (CostumeCategory type in System.Enum.GetValues(typeof(CostumeCategory)))
				costume_Equipped_ItemData.Add(type, null);

			costume_Preview_ItemData = new Dictionary<CostumeCategory, Costume_ItemData>();
			foreach (CostumeCategory type in System.Enum.GetValues(typeof(CostumeCategory)))
				costume_Preview_ItemData.Add(type, null);

			costume_Cart = new Dictionary<CostumeCategory, Costume_ProductData>();
			foreach (CostumeCategory type in System.Enum.GetValues(typeof(CostumeCategory)))
				costume_Cart.Add(type, null);
			costume_ProductList = new Dictionary<string, Costume_ProductData>();
			costume_SeasonItemCode = new List<string>();
		}

		public void ClearTable_ItemData()
		{
			if (costume_ItemTable == null)
				costume_ItemTable = new Dictionary<string, Costume_ItemData>();

			costume_ItemTable.Clear();
		}
		public void AddTable_ItemData(Costume_ItemData data)
		{
			if (costume_ItemTable == null)
				costume_ItemTable = new Dictionary<string, Costume_ItemData>();

			if (costume_ItemTable.ContainsKey(data.itemCd))
				costume_ItemTable[data.itemCd] = data;
			else
				costume_ItemTable.Add(data.itemCd, data);
		}

		public void ClearOwendList_ItemData()
		{
			if (costume_OwnedList == null)
				costume_OwnedList = new List<Costume_ItemData>();

			costume_OwnedList.Clear();
		}

		public void AddOwendList_ItemData(string itemCode)
		{
			if (costume_OwnedList == null)
				costume_OwnedList = new List<Costume_ItemData>();

			if (costume_ItemTable.ContainsKey(itemCode))
			{
				costume_OwnedList.Add(costume_ItemTable[itemCode]);
			}
			else
				Logger.LogError($"CostumeInfo | invalid itemCode = {itemCode}");
		}
		public void ClearAll_Equipped_ItemData()
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				costume_Equipped_ItemData[value] = null;
			}
		}

		public void Clear_Equipped_ItemData(string itemCode)
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				if (costume_Equipped_ItemData[value] == null)
					continue;

				if (itemCode.Equals(costume_Equipped_ItemData[value].itemCd))
					costume_Equipped_ItemData[value] = null;
			}
		}

		public void Set_Equipped_ItemData(string itemCode)
		{
			Costume_ItemData item = costume_ItemTable.SafeGetValue(itemCode);
			switch (item.itemPartCateNo)
			{
				case (int)SPEAK_DecorItemPartType.BODY:
					costume_Equipped_ItemData[CostumeCategory.BODY] = item;
					break;
				case (int)SPEAK_DecorItemPartType.FACE:
					costume_Equipped_ItemData[CostumeCategory.ACC_FACE] = item;
					break;
				case (int)SPEAK_DecorItemPartType.HEAD:
					costume_Equipped_ItemData[CostumeCategory.ACC_HEAD] = item;
					break;
				default: // BG
					costume_Equipped_ItemData[CostumeCategory.BG] = item;
					break;
			}
		}
		public Costume_ItemData GetEquipped_ItemData(CostumeCategory category)
		{
			return costume_Equipped_ItemData[category];
		}
		public bool IsEquipped(string itemCode)
		{
			foreach (KeyValuePair<CostumeCategory, Costume_ItemData> data in costume_Equipped_ItemData)
			{
				if (data.Value == null)
					continue;

				if (string.IsNullOrEmpty(data.Value.itemCd))
					continue;

				if (data.Value.itemCd.Equals(itemCode))
					return true;
			}

			return false;
		}
		public void Clear_ProductList()
		{
			if (costume_ProductList == null)
				costume_ProductList = new Dictionary<string, Costume_ProductData>();

			costume_ProductList.Clear();
			Clear_Season_ItemCode();
		}
		public void Add_ProductList(SPEAK_ST_Product data)
		{
			if (costume_ProductList == null)
				costume_ProductList = new Dictionary<string, Costume_ProductData>();


			if (data.decorItems.SafeGetValue(0) == default(SPEAK_ST_DecorItem))
			{
				Logger.LogError($"not found decoitems. product code = {data.productCode}");
				return;
			}

			if (costume_ProductList.ContainsKey(data.productCode))
			{
				costume_ProductList[data.productCode].productData = data;
				costume_ProductList[data.productCode].category = GetCostumeCategory(data);

                //상품당 하나의 아이템이 들어가는게 아니면 구조 개선 필요.
                string itemCode = data.decorItems.SafeGetValue(0).itemCode;
				if (default(Costume_ItemData) != costume_OwnedList.Where(node => node.itemCd == itemCode).FirstOrDefault())
					costume_ProductList[data.productCode].IsOwned = true;
			}
			else
			{
				Costume_ProductData newData = new Costume_ProductData();
				newData.productData = data;
				newData.category = GetCostumeCategory(data);

                //상품당 하나의 아이템이 들어가는게 아니면 구조 개선 필요.
                string itemCode = data.decorItems.SafeGetValue(0).itemCode;
                if (default(Costume_ItemData) != costume_OwnedList.Where(node => node.itemCd == itemCode).FirstOrDefault())
					newData.IsOwned = true;

                costume_ProductList.Add(data.productCode, newData);
			}
		}

        public string GetCostumeCategoryName(SPEAK_ST_Product data)
        {
            switch(GetCostumeCategory(data.decorItems.SafeGetValue(0)))
            {
				case CostumeCategory.BG:
					return "배경";
                case CostumeCategory.ACC_HEAD:
					return "머리";
				case CostumeCategory.ACC_FACE:
                    return "얼굴";
				case CostumeCategory.BODY:
                    return "옷";
				default:
					return "미상";
			}
        }

        public CostumeCategory GetCostumeCategory(SPEAK_ST_Product data)
		{
			return GetCostumeCategory(data.decorItems.SafeGetValue(0));
		}

		public CostumeCategory GetCostumeCategory(SPEAK_ST_DecorItem data)
		{
			if (data.itemCateNo == (int)SPEAK_DecorItemCategory.BACKGROUND)
			{
				return CostumeCategory.BG;
			}
			else
			{
				if (data.itemPartCateNo == (int)SPEAK_DecorItemPartType.HEAD)
				{
					return CostumeCategory.ACC_HEAD;
				}
				else if (data.itemPartCateNo == (int)SPEAK_DecorItemPartType.FACE)
				{
					return CostumeCategory.ACC_FACE;
				}
				else //if (data.itemPartCateNo == (int)SPEAK_DecorItemPartType.BODY)
				{
					return CostumeCategory.BODY;
				}
			}
		}

		public string GetItemCode_Product(string procuctCode)
		{
			return GetItemData_Product(procuctCode).itemCode;
		}


		public SPEAK_ST_DecorItem GetItemData_Product(string procuctCode)
		{
			SPEAK_ST_DecorItem result = null;
			try
			{
				result = costume_ProductList[procuctCode].productData.decorItems[0];
			}
			catch (Exception e)
			{
				Logger.Log(e);
			}
			return result;
		}
		public bool IsSeason(string itemCode)
		{
			return costume_SeasonItemCode.Contains(itemCode);
		}
		public void Clear_Season_ItemCode()
		{
			costume_SeasonItemCode.Clear();
		}
		public void Add_Season_ItemCode(string itemCode)
		{
			if (costume_SeasonItemCode.Contains(itemCode))
				return;
			costume_SeasonItemCode.Add(itemCode);
		}

		public void ClearAll_Preview_ItemData()
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				costume_Preview_ItemData[value] = null;
			}
		}

		public void Clear_Preview_ItemData(string itemCode)
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				if (costume_Preview_ItemData[value] == null)
					continue;

				if (itemCode.Equals(costume_Preview_ItemData[value].itemCd))
					costume_Preview_ItemData[value] = null;
			}
		}

		public void Set_Preview_ItemData(string itemCode)
		{

			Costume_ItemData item = costume_ItemTable.SafeGetValue(itemCode);
			if (item == null)
			{
				Logger.Log($"Not Found Item Data ItemCode : {itemCode}");
			}

			costume_Preview_ItemData[item.Category()] = item;
		}

		public Costume_ItemData GetPreview_ItemData(CostumeCategory category)
		{
			return costume_Preview_ItemData[category];
		}
		public void Clear_Cart()
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				costume_Cart[value] = null;
			}
		}

		public void Remove_Cart(string productCode)
		{
			foreach (CostumeCategory value in System.Enum.GetValues(typeof(CostumeCategory)))
			{
				if (costume_Cart[value] == null)
					continue;

				if (productCode.Equals(costume_Cart[value].productData.productCode))
					costume_Cart[value] = null;
			}
		}

		public void Add_Cart(string productCode)
		{
			if (costume_ProductList == null)
				costume_ProductList = new Dictionary<string, Costume_ProductData>();

			Costume_ProductData product = costume_ProductList.SafeGetValue(productCode);

			foreach (SPEAK_ST_DecorItem item in product.productData.decorItems)
			{

				costume_Cart[GetCostumeCategory(item)] = product;
			}
		}

		public bool IsEmpty_Cart()
		{
			foreach (KeyValuePair<CostumeCategory, Costume_ProductData> data in costume_Cart)
			{
				if (data.Value != null && data.Value.IsOwned == false)
					return false;
			}

			return true;
		}

		public bool IsContain_Cart(string productCode)
		{
			foreach (KeyValuePair<CostumeCategory, Costume_ProductData> data in costume_Cart)
			{
				if (data.Value == null)
					continue;

				if (data.Value.productData == null)
					continue;

				if (data.Value.productData.productCode.Equals(productCode))
					return true;
			}

			return false;
		}

		public int GetPrice_Cart()
		{
			int result = 0;
			foreach (KeyValuePair<CostumeCategory, Costume_ProductData> data in costume_Cart)
			{
				if (data.Value == null)
					continue;

				if (data.Value.productData == null)
					continue;

                if (data.Value.IsOwned == true)
                    continue;

                result += data.Value.productData.price - (int)System.Math.Round((data.Value.productData.price * data.Value.productData.discountRate * 0.01f), 0, System.MidpointRounding.AwayFromZero);

			}

			return result;
		}
        public List<string> GetProducts_Cart()
        {
			List<string> result = new List<string>();
            foreach (KeyValuePair<CostumeCategory, Costume_ProductData> data in costume_Cart)
            {
                if (data.Value == null)
                    continue;

                if (data.Value.productData == null)
                    continue;

                if (data.Value.IsOwned == true)
                    continue;

                if (!result.Contains(data.Value.productData.productCode))
					result.Add(data.Value.productData.productCode);

            }

            return result;
        }

		public void SetOwendProduct(string productcode)
        {
			Costume_ProductData data;
			if (costume_ProductList.TryGetValue(productcode, out data))
				data.IsOwned = true;
		}
    }

	public class Costume_ItemData
	{
		/*
		구성은 SPEAK_ST_DecorItem 동일합니다. 일부 컬럼명만 다릅니다.
		*/

		public int itemNo { get; set; }
		public string itemCd { get; set; }
		public string itemName { get; set; }
		public int itemCateNo { get; set; } //SPEAK_DecorItemCategory
		public int itemDetailCateNo { get; set; } //SPEAK_DecorDetailItemCategory 
		public int itemPartCateNo { get; set; } // SPEAK_DecorItemPartType

		public CostumeCategory Category()
		{
			if (itemCateNo == (int)SPEAK_DecorItemCategory.BACKGROUND)
			{
				return CostumeCategory.BG;
			}
			else
			{
				if (itemPartCateNo == (int)SPEAK_DecorItemPartType.HEAD)
				{
					return CostumeCategory.ACC_HEAD;
				}
				else if (itemPartCateNo == (int)SPEAK_DecorItemPartType.FACE)
				{
					return CostumeCategory.ACC_FACE;
				}
				else //if (itemPartCateNo == (int)SPEAK_DecorItemPartType.BODY)
				{
					return CostumeCategory.BODY;
				}
			}
		}
	}

	public class Costume_ProductData
	{
		public SPEAK_ST_Product productData { get; set; }
		public CostumeCategory category { get; set; }
		public bool IsOwned { get; set; }

	}
	#endregion  -- user costume info --------------------------------------------------


	#region -- Image Capture --------------------------------------------------
	public class ImageCapture
    {
        private readonly string savePath = Path.Combine(Application.persistentDataPath, "ImageCapture");
		private readonly string saveNameFormat = "PengTalk_{0}.png";
		private UnityEngine.Texture2D captureTexture = null;

		private AlbumQueue pool_CaptureImages;
		public string SavePath { get => savePath; }
		
		public ImageCapture()
		{
			captureTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
			pool_CaptureImages = new AlbumQueue(25);

		}

		public Texture2D CaptureScreen()
        {
			var texture2d = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();

			captureTexture.SetPixels(texture2d.GetPixels());
			captureTexture.Apply();

			Texture2D.DestroyImmediate(texture2d);

			return captureTexture;
        }
        public Texture2D CaptureScreen(int limitSize = 0)
        {
            int width = Screen.width;
            int height = Screen.height;
			captureTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
			captureTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			captureTexture.Apply();

            if (limitSize > 0)
            {
				captureTexture = ScaleTexture(captureTexture, limitSize);
            }

            return captureTexture;
        }

        public bool SaveCapture()
        {
			string path = string.Empty;
			bool isSuccess = false;

			try
            {
                byte[] bytes = captureTexture.EncodeToPNG();
				string filename = string.Format(saveNameFormat, System.DateTime.Now.ToString("yyyyMMddHHmmssffff"));
				string directoryName;

				path = Path.Combine(savePath, filename);
				directoryName = Path.GetDirectoryName(path);

				if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                File.WriteAllBytes(path, bytes);
				Logger.Log(filename);

				// 디바이스 갤러리에 저장
				NativeGallery.SaveImageToGallery(path, 
												 directoryName, 
												 filename, 
												 (bool errorCheck, string createdPath) => {
													 isSuccess = errorCheck;
												 });
			}
            catch (Exception e)
            {
                Logger.Log(e.ToString());
			}

			return isSuccess;
		}

        static Texture2D ScaleTexture(Texture2D source, int limitSize)
        {
            int width = source.width;
            int height = source.height;
            bool resize = false;

            if (limitSize > 0)
            {
                if (width > limitSize || height > limitSize)
                {
                    int newWidth = 0;
                    int newHeight = 0;

                    float tmpRatio = (width * 1.000f) / (height * 1.000f);
                    if (tmpRatio == 1)
                    {
                        newWidth = limitSize;
                        newHeight = limitSize;
                    }
                    else
                    {
                        if (tmpRatio > 1)
                        {
                            newWidth = limitSize;
                            newHeight = (int)(limitSize / tmpRatio);
                        }
                        else
                        {
                            newWidth = (int)(limitSize * tmpRatio);
                            newHeight = limitSize;
                        }
                    }

                    width = newWidth;
                    height = newHeight;
                    if (width > 0 && height > 0)
                    {
                        resize = true;
                    }
                }
            }

            if (resize)
            {
                Texture2D result = new Texture2D(width, height, source.format, true);
                Color[] rpixels = result.GetPixels(0);
                float incX = (1.0f / (float)width);
                float incY = (1.0f / (float)height);
                for (int px = 0; px < rpixels.Length; px++)
                {
                    rpixels[px] = source.GetPixelBilinear(incX * ((float)px % width), incY * ((float)Mathf.Floor(px / width)));
                }
                result.SetPixels(rpixels, 0);
                result.Apply();
                return result;
            }

            return source;
        }


        public void RemoveCapture(string path)
        {
            try
            {
				File.Delete(path);
			}
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }

		Texture2D loadTex = null;
		public async Task<Sprite> LoadImg(string filePath)
        {
			using (var uwr = UnityWebRequestTexture.GetTexture("file://" + filePath))
			{
				uwr.downloadHandler = new DownloadHandlerTexture();
				await uwr.SendWebRequest();
				loadTex = DownloadHandlerTexture.GetContent(uwr);
                Sprite spr = Sprite.Create(loadTex, new Rect(0, 0, loadTex.width, loadTex.height), new Vector2(0.5f, 0.5f));

				return spr;
			}
        }

        public async Task<(Texture2D tex, Sprite spr)> Load_Tex_Sprite(string filePath)
        {
			using (var uwr = UnityWebRequestTexture.GetTexture("file://" + filePath))
            {
                uwr.downloadHandler = new DownloadHandlerTexture();
                await uwr.SendWebRequest();
                loadTex = DownloadHandlerTexture.GetContent(uwr);
				Sprite spr = Sprite.Create(loadTex, new Rect(0, 0, loadTex.width, loadTex.height), new Vector2(0.5f, 0.5f));

                return (loadTex,spr);
            }
        }

        public async Task<Sprite> getAlbumImg(string filePath)
        {
			PengsooAlbum pengsooAlbum = pool_CaptureImages.Where(album => album.filePath == filePath).FirstOrDefault();
			if (pengsooAlbum == null || default(PengsooAlbum).Equals(pengsooAlbum))
            {
                var value = await Load_Tex_Sprite(filePath);
				pool_CaptureImages.Enqueue( new PengsooAlbum(filePath, value.tex, value.spr));
				return pool_CaptureImages.LastValue.sprite;
            }

			return pengsooAlbum.sprite;
        }

	}

	public class AlbumQueue : Queue<PengsooAlbum>
    {

        Queue<PengsooAlbum> m_Queue = new Queue<PengsooAlbum>();
        public Queue<PengsooAlbum> Queue
        {
            get { return m_Queue; }
            set { m_Queue = value; }
        }
		int fixedCount = 10;
		PengsooAlbum LastAlbum;

        public AlbumQueue(int count)
        {
			fixedCount = count;
        }
        public Int32 Count
        {
            get { return m_Queue.Count; }
        }
        public void Enqueue(PengsooAlbum album)
        {

			LastAlbum = album;

            m_Queue.Enqueue(album);

			if (m_Queue.Count > fixedCount)
			{
				m_Queue.Dequeue().Destroay();
			}
        }
        public PengsooAlbum LastValue
        {
            get { return LastAlbum; }
        }
    }

	public class PengsooAlbum
    {
		public string filePath;
		public UnityEngine.Texture2D texture;
		public UnityEngine.Sprite sprite;

		public PengsooAlbum(string path, Texture2D tex, Sprite spr)
        {
			filePath = path;
			texture = tex;
			sprite = spr;
		}

		public void Destroay()
        {
			filePath = string.Empty;
			Texture2D.DestroyImmediate(texture);
			Sprite.DestroyImmediate(sprite);
		}
    }
    #endregion -- Image Capture info --------------------------------------------------


    #region MAILSTORAGE
	public class mailStorage
    {
		List<int> _receivedMailListId = null;
		public bool isNewMail (int _mailId)
        {
			if (_receivedMailListId.Contains(_mailId) == false)
			{
				return true;
			}

			return false;
        }
		
		public void readMailIdList ()
        {
			if (_receivedMailListId == null) 
				_receivedMailListId = PlayerPrefsManager.GetArrayList<int>("maillist");

			UnityEngine.Debug.Log("[MAIL] read mail : " + _receivedMailListId.Count);
        }

		public void saveMailIdList (List<int> _mailListId)
        {
			PlayerPrefsManager.SetIntArray("maillist", _mailListId.ToArray());
			UnityEngine.Debug.Log("[MAIL] save mail : " + _mailListId.Count);
		}
    }

    #endregion 
}


