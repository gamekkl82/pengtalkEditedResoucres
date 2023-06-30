using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;
using UniRx;
using UnityEngine.UI;
using System.IO;


public class LobbySceneHandler : SceneHandler
{
    private bool isShowNotice = false;
    private bool isStarted = false;
    private bool isInitLogic = false;

    public PengsooUIController lobbyUI;
    [SerializeField] private Transform posLobbyBG;
    [SerializeField] private Camera modelCam;  // 현재는 필요치 않기 때문에 로비씬의 UI 카메라는 카메라 매니저에 등록하지 않습니다. (로비씬UI Depth = 0)

    private GameObject morning;
    private GameObject afternoon;

    [SerializeField] private GameObject pbjBtnCostume;
    
    public async Task InitializeLobbyScene()
    {
        await StartCoroutine(_initializeLobby());
    }

    private IEnumerator _initializeLobby()
    {
        isInitLogic = false;


        //Checkup Content On Off.
        if (!GameData.Config.launching.client.IsWhiteList_Member(GameData.User.loginId))
        {
            if (false == GameData.Config.launching.client.IsContentOpen(GameData.GameContent_Type.Costume))
                pbjBtnCostume.SetActive(false);
        }

        //if(false == float.TryParse(Toast.Gamebase.Internal.GamebaseUnitySDK.AppVersion))
        //morning = AssetBundleManager.Instance.Load_LobbyBG(true, posLobbyBG);
        //afternoon = AssetBundleManager.Instance.Load_LobbyBG(false, posLobbyBG);

        morning = PengsooManger.Instance.SetBG_Equipped_Prefab(false, true, posLobbyBG.transform);
        afternoon = PengsooManger.Instance.SetBG_Equipped_Prefab(false, false, posLobbyBG.transform);

        SetBackground();
        //InvokeRepeating("SetBackground", 0, 1);


        Utility.Firebase_SetCrrentScreen("로비", "LobbySceneHandler");
        SceneController.Common?.OffBallon();
        yield return null;
        //ChapterManager.Instance.LoadChapter(chapterLevel);
    }

    private void SetBackground()
    {
        bool isMorning = IsMorning();
        morning.SetActive(isMorning);
        afternoon.SetActive(isMorning == false);
    }

    public override bool InitializeSceneHandler()
    {
        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        commonHandler.PlayBGM(AudioNames.BGM.Lobby);
        return base.InitializeSceneHandler();
    }

    public bool IsMorning()
    {
        DateTime time = GetKoreanTimeNow();
        //Logger.Log($"time:{time.Hour}");
        return time.Hour >= 6 && 18 > time.Hour;
    }

    public void SetInputBlock(bool isBlock)
    {
        if(isBlock)
        {
            TouchController.Instance.BlockedTouch("CheckingLobby");
        }
        else
        {
            TouchController.Instance.ReleasedTouch("CheckingLobby");
        }
    }

    public void CheckedLobby(Action OnChecked)
    {
        Logger.Log("[CheckedLobby]");
        SetInputBlock(true);

        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        //commonHandler.educationCode = GameData.EducationCode.World;
        //commonHandler.lastPlayNo = 10;
        //commonHandler.justNowCompleteTopic = true;
        OnCheckedLobby(async () => 
        {
            await Request_AttendPopup();

            await Request_MatchItem(new SPEAK_ST_ItemInven { no = (int)GameData.ItemId.Coin, itmCount = GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count });

            Logger.Log("[OnCheckedLobby]");
            isStarted = true;
            lobbyUI.m_PengsooController.Init();
            lobbyUI.SetZero();
            PengsooVisibleManager.Instance.PushPengsooModel(lobbyUI);
            CameraManager.Instance.AddCamera("Pengsoo", modelCam, (int)CameraManager.CamDepth.MIDDLE, true);
            CameraManager.Instance.SetModelCamFov(); // 모델 카메라의 Fov값 해상도 대응 조정

            if (commonHandler.educationCode == GameData.EducationCode.None)
            {
                await RequestLobbyProcess();
            }
            else
            {
                OpenContents(commonHandler.educationCode, commonHandler.lastPlayNo, commonHandler.justNowCompleteTopic);
                isInitLogic = true;
            }
            lobbyUI.m_PengsooController.UpdatePengsoo();

            SetInputBlock(false);

            if (OnChecked != null)
            {
                OnChecked();
            }
        });
    }

    private void OpenContents(string educationCode, int topicNo, bool justNowCompleteTopic)
    {
        Logger.Log($"OpenContents:{topicNo}");
        if (educationCode == GameData.EducationCode.World)
        {
            PopupManager.Instance.OpenPopup<WorldPopupController>(onBindAction: popup =>
            {
                popup.WorldBind(topicNo, justNowCompleteTopic);

            });
        }
        else if (educationCode == GameData.EducationCode.Theme)
        {

            PopupManager.Instance.OpenPopup<WorldPopupController>(onBindAction: popup =>
            {
                popup.ThemeBind(topicNo);
            });
            PopupManager.Instance.OpenPopup<ThemePopupController>(onBindAction: popup =>
            {
                GameData.Study.Topic topic = GameData.Study.FindTopic(topicNo);
                GameData.Study.Theme theme = GameData.Study.FindTheme(topic.worldNo);

                popup.Bind(theme);
            });
        }
        else if (educationCode == GameData.EducationCode.Speaking)
        {
            PopupManager.Instance.OpenPopup<SpeakingPopupController>(onBindAction: popup =>
            {
                popup.Bind(topicNo);

            });
            PopupManager.Instance.OpenPopup<SpeakingAndPhonicsContentsPopupController>(onBindAction: popup =>
            {
                GameData.Study.Scene scene = GameData.Study.FindScene(topicNo);
                GameData.Study.Speaking speaking = GameData.Study.FindSpeaking(scene.speakingAndPhonicsNo);
                popup.Bind(speaking);
            });
        }
        else if (educationCode == GameData.EducationCode.Phonics)
        {
            SpeakingPopupController speakingPopup =  PopupManager.Instance.OpenPopup<SpeakingPopupController>(onBindAction: popup =>
            {
                popup.Bind(topicNo, true);
            });
            PopupManager.Instance.OpenPopup<SpeakingAndPhonicsContentsPopupController>(onBindAction: popup =>
            {
                GameData.Study.Scene scene = GameData.Study.FindScene(topicNo);
                GameData.Study.Phonics phonics = GameData.Study.FindPhonics(scene.speakingAndPhonicsNo);
                popup.Bind(phonics);
            },
            () =>
            {
                speakingPopup.SetScrollPage(true);
            });
        }
        else if (educationCode == GameData.EducationCode.LetsTalk)
        {
            PopupManager.Instance.OpenPopup<LetsTalkPopupController>(onBindAction: popup =>
            {
                popup.Bind(topicNo);

            });

            PopupManager.Instance.OpenPopup<LetsTalkEpisodePopupController>(onBindAction: popup =>
            {
                GameData.Study.Episode episode = GameData.Study.FindEpisode(topicNo);
                popup.Bind(episode.letsTalk);
            });
        }
        else if (educationCode == GameData.EducationCode.ScanIt)
        {
            OpenScanitList();
        }
        else if (educationCode == GameData.EducationCode.SchoolTalk)
        {
            OpenSchoolTalkList(false);
        }
    }

    private async void OpenScanitList()
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.ScanItStart, new SPEAK_REQ_ScanItStart { cateNo = GameData.selectScanitCate.no });
        if (result.IsSuccess)
        {
            PopupManager.Instance.OpenPopup<ScanItPopupController>(onBindAction: popup =>
            {
                popup.Bind();
            },onOpenAction:()=>
            {
                PopupManager.Instance.OpenPopup<ScanItObjectPopupController>(onBindAction: popup =>
                {
                    popup.Bind(GameData.selectScanitCate);
                });
            });
        }
    }

    private void OnCheckedLobby(Action onOpened)
    {

        float entranceLen = PlayLobbyEntranceAnimation();
        SceneController.Common.SetActiveTopGroup(CommonSceneHandler.TopGroupType.All);
        
        ResetCommonSceneData();

        if (null != onOpened)
        {
            onOpened();
        }
    }
    public float PlayLobbyEntranceAnimation()
    {
        //if (lobbyEntranceAnimation.clip != null)
        //{
        //    lobbyEntranceAnimation.Play();
        //    return lobbyEntranceAnimation.clip.length;
        //}

        return 0.1f;
    }

    public void ResetCommonSceneData()
    {

    }
    #region Process
    private async Task RequestLobbyProcess()
    {
        ShowImageNotices();

        if (IsEnableNotice())
        {
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetNoticeList, "{}");
            if (result.IsSuccess)
            {
                isShowNotice = true;
                Data.NoticeContainer container = result.GetContents<Data.NoticeContainer>();
                Logger.Log($"notice count:{container.noticeInfo.Count}");

                foreach (Data.NoticeInfo info in container.noticeInfo)
                {
                    var taskCompletion = new TaskCompletionSource<bool>();
                    PopupManager.Instance.OpenPopup<NoticePopupController>(
                     onBindAction: popup =>
                     {
                         popup.Bind(info);
                     },
                     onCloseAction: () =>
                     {
                         taskCompletion.SetResult(true);
                     });
              
                    await taskCompletion.Task;
                }
            }
        }

        /*
        if(IsEnableAttendance())
        {
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.Attendance, "{}");
            if (result.IsSuccess)
            {
                
                SPEAK_RES_AttendPopup response = result.GetContents<SPEAK_RES_AttendPopup>();
                if(response.items.Count != 0)
                {
                    GameData.inventory.AddItem(response.items);

                    isShowNotice = true;
                    var taskCompletion = new TaskCompletionSource<bool>();
                    PopupManager.Instance.OpenPopup<AttendancePopupController>(onBindAction: popup =>
                    {
                        popup.Bind(response);
                    },
                        onCloseAction: () =>
                        {
                            taskCompletion.SetResult(true);
                        });

                    await taskCompletion.Task;
                }
          
            }
        }
        */

        //원래는 마지막이지만 테스트 때문에 앞으로
        if (IsEnableLobbyTutorial())
        {
            var taskCompletion = new TaskCompletionSource<bool>();
            TutorialManager.Instance.Bind(new Tutorial_Lobby(OnTutorialLobby), OnTutorialLobby_Play, ()=> {
                
                SceneController.Common?.OffBallon();
                lobbyUI.SetDefault();

                TutorialManager.Instance.SendCompleteTutorial(STAR_TutorialType.MAIN);

                taskCompletion.SetResult(true);
            });
            await taskCompletion.Task;
        }

        if (IsEnableNoticeSchoolTalk())
        {
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetAlarm, new SPEAK_REQ_Alarm { });
            if (result.IsSuccess)
            {
                string p = "yyyy-MM-dd";
                var data = result.message as SPEAK_RES_Alarm;
                SPEAK_ST_AlarmTeacher teacherData = null;
                NotiTeacherType teacherType = NotiTeacherType.None;
                foreach (var item in data.alarmTeacher)
                {
                    DateTime startTime = Utility.GetDateTime(item.startDt);
                    DateTime endDt = Utility.GetDateTime(item.endDt);
                    DateTime regTime = Utility.GetDateTime(item.regDt);

                    if (string.IsNullOrEmpty(item.feedbackDt) == false)
                    {
                        if (NewMarkManager.Instance.IsNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Comment))
                        {
                            if (endDt.ToString(p).Equals(DateTime.Now.ToString(p)))
                            {
                                teacherType = NotiTeacherType.Comment;
                                teacherData = item;
                                break;
                            }
                        }
                    }

                    if (NewMarkManager.Instance.IsNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Open))
                    {
                        if (startTime.ToString(p).Equals(DateTime.Now.ToString(p)))
                        {
                            if (startTime < DateTime.Now)
                            {
                                teacherType = NotiTeacherType.Open;
                                teacherData = item;
                                break;
                            }
                        }
                    }

                    if (NewMarkManager.Instance.IsNewMark(NewMarkManager.NewMarkType.NewMark_TeacherItem, item.roomNo, NotiTeacherType.Youtube))
                    {
                        if (regTime.ToString(p).Equals(DateTime.Now.ToString(p)))
                        {
                            if (startTime > DateTime.Now)
                            {
                                teacherType = NotiTeacherType.Youtube;
                                teacherData = item;
                                break;
                            }
                        }
                    }
                }

                if (teacherData != null)
                {
                    var taskCompletion = new TaskCompletionSource<bool>();
                    PopupManager.Instance.OpenPopup<NotiSchoolTalkPopupController>("NotiSchoolTalkPopupController", popup =>
                    {
                        popup.Bind(teacherData, teacherType, () =>
                        {
                            if (teacherType != NotiTeacherType.Youtube)
                                taskCompletion.SetResult(true);
                        });
                    },
                    onCloseAction:()=>
                    {
                        if(teacherType == NotiTeacherType.Youtube)
                            taskCompletion.SetResult(true);
                    });

                    await taskCompletion.Task;
                }
            }
        }
        if (IsEnableCostume())
        {
            GameData.costumeinfo.ClearOwendList_ItemData();
            GameData.costumeinfo.ClearAll_Equipped_ItemData();
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.Costume_InvenList, new SPEAK_REQ_DecorItemInven { });
            if (result.IsSuccess)
            {
                SPEAK_RES_DecorItemInven response = result.GetContents<SPEAK_RES_DecorItemInven>();
                if (response.decorItems != null && response.decorItems.Count != 0)
                {
                    foreach (SPEAK_ST_DecorItemInven item in response.decorItems)
                    {

                        GameData.costumeinfo.AddOwendList_ItemData(item.decorItemCode);
                        if (true == item.use)
                            GameData.costumeinfo.Set_Equipped_ItemData(item.decorItemCode);
                    }
                }
            }

            UpdateBG(false);
            UpdatePengsoo(false);
        }


        if (IsEnableRecommend())
        {
            DoRecommend();
        }


    }

    private DateTime GetKoreanTimeNow()
    {
        return new DateTime(DateTime.UtcNow.Ticks + 9 * TimeSpan.TicksPerHour);
    }

    private DayOfWeek GetDayOfWeek()
    {
        return GetKoreanTimeNow().DayOfWeek;
    }

    public bool IsEnableNotice()
    {
        string ticksValue = PlayerPrefs.GetString("Notice_Ticks", string.Empty);
        if(string.IsNullOrEmpty(ticksValue))
        {
            return isShowNotice == false;
        }
        long ticks = long.Parse(ticksValue);
        TimeSpan diff = GetKoreanTimeNow() - new DateTime(ticks);
        Logger.Log($"[LobbySceneHandler/IsEnableNotice] {diff.Days}");
        return diff.Days > 7 && isShowNotice == false;
    }

    public bool IsEnableAttendance()
    {
        if (!GameData.HasAttendData())
            return false;
        
        if (!PlayerPrefs.HasKey("Talk_Attend_Time"))
        {
            return true;
        }

        //마지막 출첵 시간.
        string ticks;
        ticks = PlayerPrefs.GetString("Talk_Attend_Time");
        //현재 시각.
        DateTime dtNow = new DateTime(DateTime.Now.Ticks, DateTimeKind.Local);

        long elapsedTicks = dtNow.Ticks - long.Parse(ticks);
        TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

        if (elapsedSpan.TotalDays >= 1)
        {
            return true;
        }

        return false;
    }

    public bool IsEnableNoticeSchoolTalk()
    {
        return true;
    }

    public bool IsEnableLobbyTutorial()
    {
        return TutorialManager.Instance.IsCompleted(STAR_TutorialType.MAIN) == false;
    }

    public bool IsEnableRecommend()
    {
        return true;
    }

    public bool IsEnableCostume()
    {
        return true;
    }

    public void SetNoticeDate()
    {
        DateTime now = GetKoreanTimeNow();
        PlayerPrefs.SetString("Notice_Ticks", now.Ticks.ToString());
    }
    #endregion

    #region Lobby Bottom
    public void OpenScroll()
    {
        PopupManager.Instance.OpenPopup<ScrollPopupController>( onBindAction: popup =>
              {
                  popup.Bind();
              },
              onCloseAction: () =>
              {
              });
    }

    //public void OnClickStart()
    //{
    //    OpenGameStartPopup(1, 1, 0, null);
    //}
    #endregion

    //void OpenGameStartPopup(int _chapterLevel, int _stageLevel, int _alreadyMedalType, System.Action onNext = null)
    //{
    //    PopupManager.Instance.OpenPopup<GameStartPopupController>("GameStartPopupController", popup =>
    //    {
    //        popup.SetGameStartPopupController(_chapterLevel, _stageLevel, _alreadyMedalType);
    //    }, () => {
    //        if (onNext != null)
    //            onNext();
    //    });
    //}
    Action<System.Action> onDisappearTransitionScene = null;

    public void DisappearTransionScene(System.Action onComplete = null)
    {
        if (onDisappearTransitionScene != null)
        {
            onDisappearTransitionScene(onComplete);
            onDisappearTransitionScene = null;
        }
    }
    public async void OnClickBtn01()
    {
        Logger.Log("OnClickBtn01");
        if (isInitLogic == false)
        {
            return;
        }
        Utility.Firebase_LogEvent("토픽월드_메인클릭");
        ShowWorldPopup(0);
    }

    public void OnClickBtn02()
    {
        Logger.Log("OnClickBtn02");
        if (isInitLogic == false)
        {
            return;
        }
        Utility.Firebase_LogEvent("스피킹_메인클릭");
        ShowSpeakingPopup(0);
    }



    public async void OnClickBtn03()
    {
        Logger.Log("OnClickBtn03");
        if (isInitLogic == false)
        {
            return;
        }
        Utility.Firebase_LogEvent("렛츠톡_메인클릭");
        ShowLetsTalk(0);

    }

    public void OnClickBtn04()
    {
        if (isInitLogic == false)
        {
            return;
        }
        
        OffPengsoo();
		Utility.Firebase_LogEvent("스캔잇_메인클릭");        
		onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<ScanItPopupController>(onBindAction: popup =>
            {
                popup.Bind();
                DisappearTransionScene(() =>
                {

                });
            });
        });
        

    }

    // HGKIM 230102 - 개방형 팝업을 위해 async로 변경
    public async void OnClickBtn05()
    {
        if (isInitLogic == false)
        {
            return;
        }

        // 개방형 로그인 처리
        if (GameData.isOpenPublicLogin)
        {
            var closePopup = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController("학교 회원만 사용가능해요!",
                    okAction: () => { closePopup.SetResult(true); });
            });
            await closePopup.Task;
            return;
        }

        OffPengsoo();
        Utility.Firebase_LogEvent("스쿨톡_메인클릭");
        Logger.Log("OnClickBtn05");
        OpenSchoolTalkList(true);
    }

    public void OnClickBtn06()
    {
        Logger.Log("OnClickBtn06");
        if (isInitLogic == false)
        {
            return;
        }
        Utility.Firebase_LogEvent("파닉스_메인클릭");
        ShowSpeakingPopup(0, true);
    }

    async void OpenSchoolTalkList(bool useLoadingPopup)
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SchoolTalk_RoomInfo, new SPEAK_REQ_SchoolTalk_RoomInfo { });
        if (result.IsSuccess)
        {
            SPEAK_RES_SchoolTalk_RoomInfo roomInfo = result.message as SPEAK_RES_SchoolTalk_RoomInfo;
            if(useLoadingPopup)
            {
                onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
                {
                    PopupManager.Instance.OpenPopup<SchoolTalkListPopupController>(onBindAction: popup =>
                    {
                        popup.Bind(roomInfo);
                        DisappearTransionScene(() =>
                        {

                        });
                    });
                });
            }
            else
            {
                PopupManager.Instance.OpenPopup<SchoolTalkListPopupController>(onBindAction: popup =>
                {
                    popup.Bind(roomInfo, true);
                });
            }
        }
    }

    public void OnClickBtn10()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnClickBtn10");
    }

    public void OnClickBtn11()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnClickBtn11");
        Utility.Firebase_LogEvent("내가방_메인클릭");
        // HGKIM 220819 - 예외처리 - 내 가방을 직접 눌렀을 때에 플래그가 켜져있으면 꺼준다.
        if (GameData.isTodayWordPush == true) GameData.isTodayWordPush = false;

        PopupManager.Instance.OpenPopup<MyBagPopupController>(onBindAction: popup =>
           {
               popup.Bind();
               // 230421 - test code - 매치아이템 테스트하기 위해 강제로 10 올린다.
               //{
               //    GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count += 10;
               //}
           }//,  // 230421 - 매치아이템 테스트를 위해 빠져나갈때에 매치아이템을 실행한다.
           //onCloseAction: async () => 
           //{
           //    await Request_MatchItem(new SPEAK_ST_ItemInven { no = (int)GameData.ItemId.Coin, itmCount = GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count });
           //}
        );
    }

    public void OnClickBtn12()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnClickBtn12");
        Utility.Firebase_LogEvent("랭킹_메인클릭");
        PopupManager.Instance.OpenPopup<RankingPopupController>(popup =>
           {
           });
    }

    public void OnClickSetting()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnClickSetting");

		Utility.Firebase_LogEvent("설정_메인클릭");
        PopupManager.Instance.OpenPopup<SettingPopupController>( popup =>
           {
               popup.Bind();
           },
           onCloseAction: () =>
           {
           });
    }

    public void OnClickNotice()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnClickNotice");
		Utility.Firebase_LogEvent("알림_메인클릭");
        PopupManager.Instance.OpenPopup<NotiCenterPopupController>( popup =>
            {
            },
            onCloseAction: () =>
            {
                
            });
    }

    public void OnClickHelp()
    {
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();

		Utility.Firebase_LogEvent("도움말_메인클릭");
        Utility.Firebase_SetCrrentScreen("도움말", "LobbySceneHandler_help");
        Logger.Log("OnClickHelp");
        string url = "https://pengha.oc.toast.com/penghi/hc/article/";
#if UNITY_STANDALONE
        Application.OpenURL(url);
        //OnWebViewShow();
        //Toast.Gamebase.GamebaseAsync.Webview.OpenWebview(url, OnWebViewClose);
#else
        Toast.Gamebase.GamebaseAsync.Webview.OpenWebview(url, null);
#endif
        //PopupManager.Instance.OpenPopup<HelpPopupController>( popup =>
        //    {
        //    },
        //    onCloseAction: () =>
        //    {
        //        if (isTutorialLogic && IsEnableRecommend())
        //        {
        //            DoRecommend();
        //        }
        //    });
    }

    // 230405 - 참치캔 버튼 작동 때문에 추가
    public bool OnClickTunaRecord() 
    {
        if (isInitLogic == false)
        {
            return false;
        }
        OffPengsoo();

        return true; 
    }

    // 230317 - mail
    bool isMailClicked = false;
    public void OnClickMail()
    {
        if (isInitLogic == false)
        {
            return;
        }
        if (isMailClicked)
		{
            Logger.Log("Mail Clicked!!");
            return;
        }
        OffPengsoo();

        isMailClicked = true;

        PopupManager.Instance.OpenPopup<MailCenterPopupController>(popup =>
        {
            popup.Bind();
        },
        onCloseAction: () =>
        {
            isMailClicked = false;
        });
    }

    public void OnWebViewShow()
    {
        TouchController.Instance.BlockedTouch("EventOnWeb");
    }

    public void OnWebViewClose()
    {
        TouchController.Instance.ReleasedTouch("EventOnWeb");
    }

    bool isCostumeButtonClicked = false;        // HGKIM 220926 - 코스튬 버튼 연속으로 안눌리게 처리

    public async void OnClickCostume()
    {
        Logger.Log("OnClickCostume Button!!!");
        if (isInitLogic == false)
        {
            return;
        }

        if (isCostumeButtonClicked)
        {
            Logger.Log("isCostumeButtonClicked is ture");
            return;
        }

        isCostumeButtonClicked = true;

        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.Costume_Products, new SPEAK_REQ_Products { });


        if (result.IsSuccess)
        {
            GameData.costumeinfo.Clear_ProductList();

            SPEAK_RES_Products response = result.GetContents<SPEAK_RES_Products>();
            if (response.products != null && response.products.Count != 0)
            {
                DateTime sale_EndDate = DateTime.MinValue;
                string formatString = "yyyyMMddHHmm";
                foreach (SPEAK_ST_Product product in response.products)
                {
                    sale_EndDate = DateTime.ParseExact(product.startDate, formatString, null);
                    if (DateTime.Now - sale_EndDate <= TimeSpan.Zero)
                        continue;

                    GameData.costumeinfo.Add_ProductList(product);

                    if(product.saleType == (int)SPEAK_SaleType.SEASON_LIMIT)
                    for (int ii = 0; ii < product.decorItems.Count; ++ii)
                    {
                        GameData.costumeinfo.Add_Season_ItemCode(product.decorItems[ii].itemCode);
                    }
                }

            }

            OffPengsoo();
            Logger.Log("OnClickBtn11");
            Utility.Firebase_LogEvent("꾸미기_메인클릭");
            PopupManager.Instance.OpenPopup<CostumePopupController>(onBindAction: popup =>
            {
                popup.Bind();
            },
            onCloseAction: () =>
            {
                isCostumeButtonClicked = false;

                UpdateBG(false);
                UpdatePengsoo(false);
            });
        }
        else
        {
            PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
               popup =>
               {
                   popup.SetSystemPopupController("꾸미기", "꾸미기 상품정보를 받아오는데 실패하였어요.");
               });
        }
    }

    // HGKIM 220823 - 푸시로 이동하는 것 별도로 제작 (플래그 관리 떄문)
    public void OnGoingTodaysWord()
	{
        if (isInitLogic == false)
        {
            return;
        }
        OffPengsoo();
        Logger.Log("OnGoingTodaysWord");
        Utility.Firebase_LogEvent("내가방_메인클릭");

        PopupManager.Instance.OpenPopup<MyBagPopupController>(onBindAction: popup =>
        {
            popup.Bind();
        });
    }
    // HGKIM 230428 - 메일함 푸시이동이 생겨서 추가함
    public void OnGoingMail()
	{
        if (isInitLogic == false)
        {
            return;
        }
        Logger.Log("OnGoingMail");

        OnClickMail();
    }

    private async void DoRecommend()
    {
        Logger.Log("[Recommend]");
        
        OnPengsoo();

        SceneController.Common?.ShowText($"펭하~ {GameData.User.userName} 안녕하세요", "Pengha~ ", "T_main_01");
        //음성 데이터 만큼
        await Task.Delay(1000);

        // HGKIM 220803 - 푸시가 먼저 반응하도록 수정 (푸시 누르면 무조건 이동)
        // 여기는 푸시로 받았을때에 바로 들어갈 수 있는지 확인하는 테스트 코드
        if (GameData.isTodayWordPush)
        {
            Logger.Log("HGKIM :: 오늘의 단어 컨텐츠로 자동으로 이동합니다.");
            isInitLogic = true;
            OnGoingTodaysWord();
        }
        // HGKIM 230428 - 푸시가 하나 늘어나서 추가함 (메일함 시스템)
        else if (GameData.isMailPush)
		{
            Logger.Log("HGKIM :: 메일함로 자동으로 이동합니다.");
            isInitLogic = true;
            OnGoingMail();
        }
        else
        {
            // HGKIM 220930 - 오늘의 영단어 메인에서 안나오게 하라고 해서 제거
            //// HGKIM 220718 - 오늘의 단어를 하지 않았다면
            //if (GameData.isTodayWordClear.Equals(false))
            //{
            //    Logger.Log("HGKIM :: 오늘의 단어 컨텐츠로 팝업 내용을 변경합니다.");

            //    // 상단 말풍선에 표기되는 스크립트
            //    GameData.RecommendScript script = new GameData.RecommendScript
            //    {
            //        korDesc = new List<string> { "오늘의 영단어를 학습해볼까요?" },     // 한국어 코멘트
            //        engDesc = new List<string> { "Shall we learn today's word?" },      // 영어 코멘트
            //        soundFile = new List<string> { "" }                        // 재생할 사운드 파일
            //    };

            //    string text = "";

            //    // 하단 판넬에 들어가는 데이터
            //    if (GameData.todaysWord.title != null && GameData.todaysWord.wdText != null)
            //    {
            //        text = GameData.todaysWord.wdText;
            //    }
            //    else
            //    {
            //        text = "aaaaa";
            //    }
            //    // API로 받아온 오늘의 영단어를 여기서 넣어준다.
            //    GameData.RecommendData recommendData = new GameData.RecommendData
            //    {
            //        title = "일일영단어",   // 팝업 상단에 타이틀    // 이건 안바뀐다.
            //        leftTitle = "Word365",  // 왼쪽 그림 아래에 들어가는 단어 // 이건 안바뀐다.
            //        desc = text,//GameData.todaysWord.wdText,//"aaaa",          // 오른쪽 메인 부분에 들어가는 문장
            //        imagePath = "Textures/Mybag/mybag-menu-word",         // 왼쪽 그림 위에 덮을 그림이 있으면 여기에 Path를 넣는다.
            //        type = GameData.EducationCode.None, // 뭔지 모르겠음 - 메인 컨텐츠 학습코드 같음
            //        targetNo = 0                        // 위의 학습코드랑 세트로 몇번 학습에 들어갈지 결정하는거 같음
            //    };

            //    SceneController.Common?.ShowText(script.GetKor(), script.GetEng(), script.GetSound());

            //    PopupManager.Instance.OpenPopup<RecommendPopupController>(popup =>
            //    {
            //        popup.ShowMessage(recommendData, RecommendProcess, () =>
            //        {
            //            string kor = script.NextKor();
            //            string eng = script.NextEng();
            //            if (kor.IsNullOrEmpty() == false
            //            && eng.IsNullOrEmpty() == false)
            //            {
            //                SceneController.Common?.OnBallon();
            //                LastMessage(kor, eng, script.NextSound());
            //            }
            //        });
            //    },
            //    () =>
            //    {
            //        lobbyUI.SetPengsooAnimEnable(false); // 추천 학습 팝업이 보여지므로 펭수 애니가 작동하면 안됩니다.
            //    },
            //    () =>
            //    {
            //        lobbyUI.SetPengsooAnimEnable(true); // 추천 학습 팝업이 사라지므로 펭수 애니를 작동
            //    });

            //    isInitLogic = true;
            //}
            //// 오늘의 단어를 클리어 했다면 이쪽으로 간다.
            //else
            {
                DayOfWeek dayOfWeek = GetDayOfWeek(); // 원래는 딜레이 위에 있던 코드 - 사용할 일이 없으니 여기로 이동함

                GameData.RecommendScript script = GameData.GetRecommendScript(dayOfWeek);
                if (IsRecommendDay(dayOfWeek))
                {
                    //if 추천요일이면
                    //팝업 띄우기
                    string type = RecommendType(dayOfWeek);
                    Logger.Log(type);
                    GameData.RecommendData recommendData = GameData.GetRecommendData(type);
                    if (recommendData == null)
                    {
                        Logger.LogError($"{type} recommend not found");
                        LastMessage(script.GetKor(), script.GetEng(), script.GetSound());
                    }
                    else
                    {
                        //추천 대상에 따라 문구가 변경됨
                        SceneController.Common?.ShowText(script.GetKor(), script.GetEng(), script.GetSound());

                        PopupManager.Instance.OpenPopup<RecommendPopupController>(popup =>
                        {
                            popup.ShowMessage(recommendData, RecommendProcess, () =>
                            {
                                string kor = script.NextKor();
                                string eng = script.NextEng();
                                if (kor.IsNullOrEmpty() == false
                                && eng.IsNullOrEmpty() == false)
                                {
                                    SceneController.Common?.OnBallon();
                                    LastMessage(kor, eng, script.NextSound());
                                }
                            });
                        },
                        () =>
                        {
                            lobbyUI.SetPengsooAnimEnable(false); // 추천 학습 팝업이 보여지므로 펭수 애니가 작동하면 안됩니다.
                    },
                        () =>
                        {
                            lobbyUI.SetPengsooAnimEnable(true); // 추천 학습 팝업이 사라지므로 펭수 애니를 작동
                    });
                    }
                }
                else
                {
                    LastMessage(script.GetKor(), script.GetEng(), script.GetSound());
                }
                isInitLogic = true;
            }
        }
        // HGKIM 220718 - 여기까지
    }

    private async void LastMessage(string kor, string eng, string soundFile)
    {
        AudioController.Play(soundFile);
        SceneController.Common?.ShowText(kor, eng);
        await Task.Delay(2000);

        OffPengsoo();
    }

    private bool IsRecommendDay(DayOfWeek dayOfWeek)
    {
        return (dayOfWeek == DayOfWeek.Sunday || dayOfWeek == DayOfWeek.Saturday) == false;
    }

    private string RecommendType(DayOfWeek dayOfWeek)
    {
        switch(dayOfWeek)
        {
            case DayOfWeek.Monday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Friday:
                {
                    return GameData.EducationCode.World;
                }
            case DayOfWeek.Tuesday:
                {
                    return GameData.EducationCode.LetsTalk;
                }
            case DayOfWeek.Thursday:
                {
                    return GameData.EducationCode.Speaking;
                }
        }
        return string.Empty;
    }


    private void RecommendProcess(GameData.RecommendData recommendData)
    {
        if(GameData.EducationCode.World == recommendData.type)
        {
            ShowWorldPopup(recommendData.targetNo);
        }
        else if(GameData.EducationCode.LetsTalk == recommendData.type)
        {
            ShowLetsTalkEpisode(recommendData.targetNo);
        }
        else if (GameData.EducationCode.Speaking == recommendData.type)
        {
            ShowSpeakingContentsPopup(recommendData.targetNo);
        }
        // HGKIM 220726 - 요일이 없으니 지워도 되지만 그냥 이렇게 처리함
        else
        {
            GameData.isTodayWordPush = true;    // 이거 하나만 가지고 처리하려고 여기서 임시로 켜줌
            OnGoingTodaysWord();
        }
    }

    public void FromLetsTalkToWorldPopup()
    {
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            LetsTalkPopupController letsTalk = PopupManager.Instance.GetPopup<LetsTalkPopupController>();
            letsTalk.OnClosePopup();

            PopupManager.Instance.OpenPopup<WorldPopupController>(popup =>
            {
                popup.Bind(0);
                DisappearTransionScene(() =>
                {

                });
            });
        });
    }

    private void ShowWorldPopup(int index)
    {
        OffPengsoo();
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<WorldPopupController>(onBindAction: popup =>
            {
                popup.Bind(index);
                DisappearTransionScene(() =>
                {

                });
            });
        });
    }
    private void ShowSpeakingPopup(int topicNo, bool isPhonicsEntry = false)
    {
        OffPengsoo();
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<SpeakingPopupController>(popup =>
            {
                popup.Bind(topicNo, isPhonicsEntry);
                DisappearTransionScene(() =>
                {
                    popup.SetScrollPage(isPhonicsEntry);
                });
            });
        });
    }
    private void ShowSpeakingContentsPopup(int topicNo)
    {
        OffPengsoo();
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<SpeakingPopupController>(popup =>
            {
                popup.Bind(topicNo);

            });

            PopupManager.Instance.OpenPopup<SpeakingAndPhonicsContentsPopupController>(onBindAction: popup =>
            {
                GameData.Study.Scene scene = GameData.Study.FindScene(topicNo);
                GameData.Study.Speaking speaking = GameData.Study.FindSpeaking(scene.speakingAndPhonicsNo);
                popup.Bind(speaking);
                DisappearTransionScene(() =>
                {

                });
            });
        });
    }

    private void ShowLetsTalk(int topicNo)
    {
        OffPengsoo();
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<LetsTalkPopupController>(popup =>
            {
                popup.Bind(topicNo);
                DisappearTransionScene(() =>
                {

                });
            });
        });
    }
    private void ShowLetsTalkEpisode(int topicNo)
    {
        OffPengsoo();
        onDisappearTransitionScene = SceneController.Common.ImageOutTransition(2, () =>
        {
            PopupManager.Instance.OpenPopup<LetsTalkPopupController>(popup =>
            {
                popup.Bind(topicNo, true);
                
            });

            PopupManager.Instance.OpenPopup<LetsTalkEpisodePopupController>(onBindAction: popup =>
            {
                GameData.Study.Episode episode = GameData.Study.FindEpisode(topicNo);
                popup.Bind(episode.letsTalk);

                DisappearTransionScene(() =>
                {

                });
            });
        });
    }

    public void ToLobbyTransition(Action before)
    {
        ShowTransition(before, ()=> {
            DisappearTransionScene(() =>
            {

            });
        });
    }
    public void ShowTransition(Action before, Action after)
    {
        onDisappearTransitionScene = SceneController.Common.ImageInTransition(2, 
        () =>
        {
            before();
            after();
        });
    }

    protected override void OnEscapeFromScene()
    {
        if (!CheckedEscape())
            return;

        // 페이드인아웃 진행 중이면 실행이 안되도록 막는다.
        if (null != SceneController.Common && SceneController.Common.isTransitionScene)
        {
            return;
        }

        // 애니매이션이 진행 중이면 실행이 안되도록 막는다.
        if (null != SceneController.Common && SceneController.Common.isPlayingAnimation)
        {
            return;
        }

        if (TutorialManager.Instance.IsPlaying())
        {
            return;
        }


        if (PopupManager.Instance.ActivatedPopup)
        {
            //if (PopupManager.Instance.IsOpenedPopup("DiaryPopupController"))
            //{
            //    DiaryPopupController diaryPopup = PopupManager.Instance.GetCurrentPopup<DiaryPopupController>();
            //    if (null != diaryPopup)
            //    {
            //        diaryPopup.OnBackButton();
            //        return;
            //    }
            //}
          
       

            PopupManager.Instance.CloseCurrentPopup();
        }
        else
        {
            if (isStarted)
                OpenQuitPopup();
        }
    }

    protected void OnTutorialLobby_Play()
    {
        OnPengsoo();
        SceneController.Common?.ShowText($"펭하~ {GameData.User.userName} 안녕하세요", "Pengha~");
    }


    protected void OnTutorialLobby_Stop()
    {
        OffPengsoo();
    }

    private void OnPengsoo()
    {
        SceneController.Common?.OnBallon();
        lobbyUI.PlayPengsoo("pengha");
    }

    private void OffPengsoo()
    {
        SceneController.Common?.OffBallon();
        lobbyUI.SetDefault();
    }

    public void UpdateBG(bool isPreview = true)
    {
        GameData.Costume_ItemData itemData = getItemData_BG(isPreview);

        if (itemData != null && morning.name.Contains(itemData.itemCd))
            return;

        GameObject.Destroy(morning);
        GameObject.Destroy(afternoon);

        morning = PengsooManger.Instance.SetBG_Equipped_Prefab(false, true, posLobbyBG.transform);
        afternoon = PengsooManger.Instance.SetBG_Equipped_Prefab(false, false, posLobbyBG.transform);
        SetBackground();
    }
    public GameData.Costume_ItemData getItemData_BG(bool isPreview)
    {
        if (isPreview)
            return GameData.costumeinfo.GetPreview_ItemData(GameData.CostumeCategory.BG);
        else
            return GameData.costumeinfo.GetEquipped_ItemData(GameData.CostumeCategory.BG);
    }

    public void UpdatePengsoo(bool isPreview = true)
    {
        lobbyUI.m_PengsooController.UpdatePengsoo(isPreview);
        lobbyUI.PlayPengsoo("pengha");
    }

    protected void OnTutorialLobby(int tutorialId)
    {
        Logger.Log($"OnTutorialLobby {tutorialId}");

        switch (tutorialId)
        {
            case 0:
                {
                    SceneController.Common.ShowText("AI펭톡에 온 걸 환영해요!", "Welcome to AI PENGTALK");   // 220927 - 맞춤법 수정
                }
            break;
            case 1:
                {
                    SceneController.Common.ShowText("AI펭톡에 대해 궁금한 내용은 [도움말]을 통해 확인해보세요",string.Empty);
                }
                break;
            case 2:
                {
                    
                }
                break;      

        }

    }

    public async Task Request_MatchItem(SPEAK_ST_ItemInven matchitem)
    {
        Logger.Log("Request_MatchItem!!");

        var response = await MPServiceFacade.Network.Request(NetworkServiceRequest.MatchItem,
            new SPEAK_REQ_MatchItem()
            {
                item = matchitem
            });

        if (!response.IsSuccess)
        {
            // 230421 - 매치코드 서버에서 내려온 데이터로 갱신하도록 수정
            var coinData = (response.message as SPEAK_RES_MatchItem).tunaItem;
            Logger.LogError(Momo.Localization.Get("system_error_C042004") + " MatchItem Error!!!!");
            Logger.Log("서버에서 넘어온 데이터로 갱신합니다.");
            Logger.Log($"now can number : {GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count} => response can number : {coinData.itmCount}");

            GameData.inventory.SafeRefreshItem(coinData.itmCount);

            // 아래는 기존 코드 백업
            //PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            //{
            //    string message = Momo.Localization.Get("system_error_C042004");
            //    popup.SetSystemPopupController(message);
            //},
            //onCloseAction: () =>
            //{
            //    SceneController.Instance.BackToIntroFromLobby();
            //});

        }
    }

    public async Task Request_AttendPopup()
    {
        if (IsEnableAttendance())
        {
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.Attendance, "{}");
            if (result.IsSuccess)
            {
                SPEAK_RES_AttendPopup response = result.GetContents<SPEAK_RES_AttendPopup>();
                if (response.items.Count != 0)
                {
                    PlayerPrefs.SetString("Talk_Attend_Time",DateTime.Now.Date.Ticks.ToString());

                    GameData.inventory.AddItem(response.items);

                    isShowNotice = true;
                    var taskCompletion = new TaskCompletionSource<bool>();
                    PopupManager.Instance.OpenPopup<AttendancePopupController>(onBindAction: popup =>
                    {
                        popup.Bind(response);
                    },
                        onCloseAction: () =>
                        {
                            taskCompletion.SetResult(true);
                        });

                    await taskCompletion.Task;
                }

            }
        }
    }

    public void ShowImageNotices()
    {

        Toast.Gamebase.GamebaseRequest.ImageNotice.Configuration configuration = new Toast.Gamebase.GamebaseRequest.ImageNotice.Configuration();
        configuration.colorR = 255;
        configuration.colorG = 255;
        configuration.colorB = 255;
        configuration.colorA = 255;
        configuration.timeout = 5000;

        Toast.Gamebase.Gamebase.ImageNotice.ShowImageNotices(
            configuration,
            (error) =>
            {
                if (error != null) Logger.LogError(error.message);
                else
                {
                    string errorMsg = "ShowImageNotices close is error. but error message is null";
#if UNITY_EDITOR
                    Logger.Log(errorMsg); // 유니티 에디터에서 게임 일시정지를 하지 않기 위한 임시 처리 
#else
                    Logger.LogError(errorMsg);
#endif
                }
            },
            (scheme, error) =>
            {
                // Called when custom event occurred.
                Logger.LogError(scheme);
                Logger.LogError(error.message);
            });
    }


    // HGKIM 220929 - 패스워드 입력에 방해되어서 제거 이거 테스트 필요할 때에 열어서 할 것
#if FALSE//UNITY_EDITOR
    public void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            //출석체크
            PopupManager.Instance.OpenPopup<AttendancePopupController>(onBindAction: popup =>
            {
                popup.Bind(true);
            },
            onCloseAction: () =>
            {
            });
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            List<SPEAK_ST_ItemInven> items = new List<SPEAK_ST_ItemInven>() { new SPEAK_ST_ItemInven { no = (int)GameData.ItemId.Coin, itmCount = 30000 } };
            GameData.inventory.AddItem(items);
        }

        if (Input.GetKeyUp(KeyCode.I))
        {
            List<SPEAK_ST_ItemInven> items = new List<SPEAK_ST_ItemInven>() { new SPEAK_ST_ItemInven { no = (int)GameData.ItemId.Coin, itmCount = 300 } };
            GameData.inventory.AddItem(items);
        }
        if (Input.GetKeyUp(KeyCode.O))
        {
            PopupManager.Instance.OpenPopup<WorldPopupController>(onBindAction: popup =>
            {
                popup.WorldBind(21, true);

            });
        }

    }
#endif
}