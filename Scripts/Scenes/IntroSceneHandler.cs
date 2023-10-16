using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Toast.Gamebase;
using Toast.SmartDownloader;
using UnityEngine.UI;

using System;
using System.Threading.Tasks;

using UniRx;
using UniRx.Async;
using NHNEdu;
using System.IO;

public enum IntroStatus
{
    None,

    Verifying,
    ConnectingGameServer,

}

public enum eConnectStatus
{
    CONNECT_NONE = 0,

    CONNECT_VERIFYING = 1,
    CONNECT_VERIFYING_SUCCESS,
    CONNECT_VERIFYING_FAIL,

    CONNECT_SERVER,
    CONNECT_SERVER_SUCCESS,
    CONNECT_SERVER_FAIL,

    SHOW_START_BUTTON,

    CONNECT_FACEBOOK,
    CONNECT_FACEBOOK_SUCCESS,
    CONNECT_FACEBOOK_FAIL,

    CONNECT_LOADING_FRIENDS,
    CONNECT_LOADING_FRIENDS_SUCCESS,
    CONNECT_LOADING_FRIENDS_FAIL,

    CONNECT_DOWNLOAD_ASSETS,
    CONNECT_DOWNLOAD_ASSETS_SUCCESS,
    CONNECT_DOWNLOAD_ASSETS_FAIL,

    CONNECT_ENTER_LOBBY,
    CONNECT_ENTER_LOBBY_SUCCESS,
    CONNECT_ENTER_LOBBY_FAIL,

    CONNECT_COMPLETED,
}
public class IntroSceneHandler : SceneHandler
{
    [SerializeField]
    private GameObject headUpButtonGroup;
    [SerializeField]
    private GameObject loadingStatusGroup = null;
    [SerializeField]
    private Slider loadingStatusSlider = null;
    [SerializeField]
    private Text loadingStatusLabel = null;
    [SerializeField]
    private Animation splashAnimation = null;

    private eConnectStatus _connectStatus;
    public eConnectStatus connectStatus
    {
        get
        {
            return _connectStatus;
        }
        set
        {
            _connectStatus = value;
            switch (_connectStatus)
            {
                case eConnectStatus.SHOW_START_BUTTON:
                    ShowStartButton();
                    break;
                default:
                    ShowLoadingBar();
                    break;
            }
        }
    }
    private void UpdateConnectSign()
    {
        headUpButtonGroup.SetActive(false);
        loadingStatusGroup.gameObject.SetActive(false);
        loadingStatusLabel.gameObject.SetActive(true);

        switch (connectStatus)
        {
            case eConnectStatus.CONNECT_VERIFYING:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_1");
                break;
            case eConnectStatus.CONNECT_SERVER:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_2");
                break;
            case eConnectStatus.CONNECT_FACEBOOK:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_3");
                break;
            case eConnectStatus.CONNECT_LOADING_FRIENDS:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_3");
                break;
            case eConnectStatus.CONNECT_DOWNLOAD_ASSETS:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_3");
                break;
            case eConnectStatus.CONNECT_ENTER_LOBBY:
                //loadingStatusLabel.text = Momo.Localization.Get("system_load_4");
                break;
            default:
                break;
        }
    }

    public override void PreStart()
    {
        connectStatus = eConnectStatus.CONNECT_NONE;
        base.PreStart();

        //debug - 지우지 말고 푸시 테스트 할 때 마다 주석 풀고 테스트
        //Gamebase.Push.SetSandboxMode(true);
    }
    public override bool InitializeSceneHandler()
    {
        base.InitializeSceneHandler();

        TouchController.Instance.Reset();
        

        GamebaseController.Instance.LinkSceneHandler(this);

        NetworkClient.Instance.LinkSceneHandler(this);

        headUpButtonGroup.SetActive(false);
        loadingStatusGroup.gameObject.SetActive(false);
        loadingStatusLabel.gameObject.SetActive(false);

        //platformSelectionObject.gameObject.SetActive(true);

        InitializeFirebaseAnalytics();

        // 스플래쉬 연출 시작
        playSplashAnimation();

        LoadCommonScene(async () =>
        {
            PlatformRequestResult result = await MPServiceFacade.Platform.Initialize();
            while (result.IsSuccess == false)
            {

                if (result.ErrorCode == GamebaseErrorCode.SOCKET_ERROR)
                {
                    var closePopup = new TaskCompletionSource<bool>();
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                    popup =>
                    {
                        popup.SetSystemPopupController(
                            Momo.Localization.Get("popup_network_title"),
                            "서비스 접속이 원활하지 않습니다.\n잠시 후 다시 이용해 주세요.");    // 230228 - 처음 통신 에러시 문구 변경(아래 주석이 기존 문구)
                            //Momo.Localization.Get("popup_network_message"));
                    },
                    onCloseAction: () =>
                    {
                        closePopup.SetResult(true);
                    });
                    await closePopup.Task;
                    await Task.Delay(1000);
                    result = await MPServiceFacade.Platform.Initialize();
                }
            }
            
            NetworkClient.Instance.Initialize();
            LaunchingConfigurations configuration = LaunchingManager.LaunchingConfig();
            bool response = await LaunchingManager.Instance.GetLaunchingInfo<GameData.ConfigInfo>(configuration,
                 (data) =>
                 {
                     GameData.SetConfig(data);
                 });
            
            if (response == false)
            {
                PopupManager.Instance.OpenQuitPopup();
                return;
            }

            if (false == CheckMaintenance())
            {
                return;
            }

            Utility.Gamebase_AddEventHandler();

            Gamebase.Terms.ShowTermsView((data, error) => 
            {
                if (Gamebase.IsSuccess(error) == true)
                {
                    Logger.Log("ShowTermsView succeeded.");
                    GamebaseResponse.Push.PushConfiguration pushConfiguration = GamebaseResponse.Push.PushConfiguration.From(data);
                }
                else
                {
                    Logger.Log(string.Format("ShowTermsView failed. error:{0}", error));
                }
            });

            //유저 가방 세팅하기
            //todo jin
            //if (AudioController.Instance.musicEnabled
            //    && null == AudioController.GetCurrentMusic()
            //    && !CommonSceneHandler.PlayingBgm.IsNullOrEmpty())
            //{
            //    CommonSceneHandler.PlayLinkedBgm();
            //}
            //else
            //{
            //    CommonSceneHandler.PlayBgm(AudioNames.BGM.Intro);
            //}

            //File.WriteAllText($"{Application.persistentDataPath}/agreement.html", Resources.Load<TextAsset>("GameData/agreement.html").text);
            //File.WriteAllText($"{Application.persistentDataPath}/privacy.html", Resources.Load<TextAsset>("GameData/privacy.html").text);

            var loginType = GamebaseAsync.GetLastLoggedInProvider();
            Logger.Log($"[IntroSceneHandler] Previouly logged-in type: {loginType}");
            
            if (loginType != GamebaseAuthProvider.FACEBOOK)
                loginType = GamebaseAuthProvider.GUEST;
            
            GamebaseController.DontShowLoading = true;
            loadingStatusGroup.SetActive(true);
            loadingStatusSlider.value = 0.0f;
            MPServiceCallback.Progress = (curr, max, serviceName) =>
            {
                if (string.Equals(MPServiceName.Login, serviceName))
                    loadingStatusSlider.value = (float)curr / (float)max;
            };
            
            //await startLogin(loginType, true);
            GamebaseController.DontShowLoading = false;
            MPServiceCallback.Progress = null;
            loadingStatusGroup.SetActive(false);

            // Sync
            connectStatus = eConnectStatus.SHOW_START_BUTTON;
            //permission
            //CheckPermission();
            
        });

        //todo console command
        //MomoPop.Console.ConsoleCommand.RegisterConsoleCommands();



        return true;
    }

    private void InitializeFirebaseAnalytics()
    {
#if !PLATFORM_STANDALONE
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                var app = Firebase.FirebaseApp.DefaultInstance;
            }
            else
            {
                Debug.LogError(string.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
#endif
    }

    public override void ReleaseSceneHandler()
    {
        GamebaseController.Instance.UnlinkSceneHandler(name);
        NetworkClient.Instance.UnlinkSceneHandler(name);
        base.ReleaseSceneHandler();
    }


    private void ShowStartButton()
    {

        //connectStatus = eConnectStatus.SHOW_START_BUTTON;
        headUpButtonGroup.SetActive(true);
        loadingStatusGroup.gameObject.SetActive(false);
        loadingStatusLabel.gameObject.SetActive(false);

    }

    private void ShowLoadingBar()
    {
        headUpButtonGroup.SetActive(false);
        loadingStatusGroup.gameObject.SetActive(true);
    }

    public void SetProgress(float value)
    {
        loadingStatusSlider.value = value;
    }

    private async Task playSplashAnimation()
    {
        Logger.LogAppTaskTime("Splash Animation Start");

        //splashAnimation.Play("Intro_Splash_Appear_02");

        //await Task.Delay((int)(splashAnimation.clip.length * 1000));

        //splashAnimation.Play("Intro_Splash_Loop_02");

        Logger.LogAppTaskTime("Splash Animation Finished");
    }
    private void LoadCommonScene(System.Action onLoadComplete)
    {
        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        if (null != commonHandler)
        {
            onLoadComplete();
            return;
        }

        SceneController.Instance.SceneLoadAdditive("Common", () =>
        {
            //todo jin
            commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
            if (null != commonHandler)
            {
                commonHandler.SetActiveTopGroup(CommonSceneHandler.TopGroupType.Off);
            }
            //commonHandler.ResetCameraState();
            onLoadComplete();
        });
    }

    ////2022-07-18 jjh 수정
    //public async void OnStartButton()
    //{
    //    headUpButtonGroup.SetActive(false);
    //    loadingStatusLabel.gameObject.SetActive(true);

    //    if (NetworkClient.Instance.ApplyPatch == true)
    //    {
    //        ResultCode code = await downloadAssets();
    //        if (code == ResultCode.SUCCESS_NO_DIFFERENCE)
    //        {
    //        }
    //        else if(code == ResultCode.SUCCESS)
    //        {
    //            ShowStartButton();
    //            return;
    //        }
    //        else
    //        {
    //            PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
    //            popup =>
    //            {
    //                popup.SetSystemPopupController("업데이트", $"업데이트에 실패하였어요.({(int)code})");
    //            });

    //            ShowStartButton();
    //            return;
    //        }
    //    }

    //    //에셋 번들로 다운받은 테이블들 로드합니다.
    //    GameData.Initialize_NickFilter();
    //    GameData.Initialize_costumeList();
    //    ///===============================================

    //    if (!GamebaseController.IsLoggedIn || !NetworkServiceHandler.IsSessionValid)
    //    {
    //        StopAni();
    //        bool isLogin = await startLogin(GamebaseAuthProvider.GUEST, false);
    //        if (isLogin == false)
    //        {
    //            headUpButtonGroup.SetActive(true);
    //            loadingStatusLabel.gameObject.SetActive(false);
    //            Logger.Log("[IntroSceneHandler/OnStartButton] guestLogin <color=red>Failed</color>");
    //            return;
    //        }
    //    }

    //    Utility.Firebase_SetUserID(GameData.User.loginId);
    //    Utility.Firebase_SetUserProperty("school_name", GameData.User.schoolName);
    //    Utility.Firebase_SetUserProperty("grade_name", GameData.User.gradeName);
    //    Utility.Firebase_SetUserProperty("class_name", GameData.User.classNo.ToString());

    //    //await downloadAssets();
    //    //EnterLobby();
    //    //AssetBundleManager.Instance.CheckDownload();

    //    EnterLobby();

    //}


    //2022-07-18 jjh 수정
    public async void OnStartButton()
    {
        headUpButtonGroup.SetActive(false);
        loadingStatusLabel.gameObject.SetActive(true);

        if (NetworkClient.Instance.ApplyPatch == true)
        {
            ResultCode code = await downloadAssets();
            if (code == ResultCode.SUCCESS_NO_DIFFERENCE)
            {
            }
            else if (code == ResultCode.SUCCESS)
            {
                ShowStartButton();
                return;
            }
            else if (code == ResultCode.ERROR_METAFILE_DOWNLOAD)
            {
                Toast.SmartDownloader.Example.FileUtil.DeleteFilesInDownloadPath();

                PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                popup =>
                {
                    popup.SetSystemPopupController("업데이트", $"업데이트에 실패하였어요.({(int)code}) \n 다시 시도해주세요.");
                });

                ShowStartButton();
                return;
            }
            else
            {
                PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                popup =>
                {
                    popup.SetSystemPopupController("업데이트", $"업데이트에 실패하였어요.({(int)code})");
                });

                ShowStartButton();
                return;
            }
        }

        //에셋 번들로 다운받은 테이블들 로드합니다.
        GameData.Initialize_NickFilter();
        GameData.Initialize_costumeList();
        ///===============================================

        if (!GamebaseController.IsLoggedIn || !NetworkServiceHandler.IsSessionValid)
        {
            StopAni();
            bool isLogin = await startLogin(GamebaseAuthProvider.GUEST, false);
            if (isLogin == false)
            {
                headUpButtonGroup.SetActive(true);
                loadingStatusLabel.gameObject.SetActive(false);
                Logger.Log("[IntroSceneHandler/OnStartButton] guestLogin <color=red>Failed</color>");
                return;
            }
        }

        Utility.Firebase_SetUserID(GameData.User.loginId);
        Utility.Firebase_SetUserProperty("school_name", GameData.User.schoolName);
        Utility.Firebase_SetUserProperty("grade_name", GameData.User.gradeName);
        Utility.Firebase_SetUserProperty("class_name", GameData.User.classNo.ToString());

        //await downloadAssets();
        //EnterLobby();
        //AssetBundleManager.Instance.CheckDownload();

        EnterLobby();

    }

    private async Task<bool> startLogin(string loginType, bool silentLogin)
    {
        connectStatus = eConnectStatus.CONNECT_VERIFYING;
    
        // autologin이 아닐때는 오류 메시지만 보여준다.
        var loginResult = await GamebaseController.Instance.Login(loginType, !silentLogin, !silentLogin);
        if (!loginResult.IsSuccess)
        {
            connectStatus = eConnectStatus.SHOW_START_BUTTON;
            Logger.Log($"[IntroSceneHandler] Failed to login with {loginType}");
        }
        return loginResult.IsSuccess;
    }
   
    private async Task<ResultCode> downloadAssets()
    {
        connectStatus = eConnectStatus.CONNECT_DOWNLOAD_ASSETS;

        loadingStatusGroup.SetActive(true);
        loadingStatusSlider.value = 0;

        string version = Application.version;
        Logger.Log("[IntroSceneHandler/DownloadAssetsStatus]app version: " + version);

        var result =  await AssetBundleManager.Instance.CheckDownload();
        return result.Code;
     }

    private void EnterLobby()
    {
        connectStatus = eConnectStatus.CONNECT_ENTER_LOBBY;
        UpdateConnectSign();

        SceneController.Instance.LoadLobbyFromIntro();
    }

    private bool CheckMaintenance()
    {
        switch (MPServiceFacade.Platform.MaintenanceCode)
        {
            case GamebaseLaunchingStatus.INSPECTING_SERVICE:        // 서비스 점검 
            case GamebaseLaunchingStatus.INSPECTING_ALL_SERVICES:   // 서비스 전체 점검
                {
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                        popup =>
                        {
                            popup.SetSystemPopupController(
                                Momo.Localization.Get("maintenance_inspecting_title"),
                                Momo.Localization.Get("maintenance_inspecting_desc"));
                        },
                        onCloseAction: () =>
                        {
                            Application.Quit();
                        });
                    return false;
                }

            case GamebaseLaunchingStatus.TERMINATED_SERVICE:        // 서비스 종료
                {
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                        popup =>
                        {
                            popup.SetSystemPopupController(
                                Momo.Localization.Get("maintenance_close_title"),
                                Momo.Localization.Get("maintenance_close_desc"));
                        },
                        onCloseAction: () =>
                        {
                            Application.Quit();
                        });
                    return false;
                }

            case GamebaseLaunchingStatus.RECOMMEND_UPDATE:          // 업데이트 권장
                {
#if !UNITY_WEBGL
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                        popup =>
                        {
                            popup.SetSystemPopupController(
                                Momo.Localization.Get("maintenance_recommend_title"),
                                Momo.Localization.Get("maintenance_recommend_desc"));
                        });
#endif

                    return true;
                }
            case GamebaseLaunchingStatus.REQUIRE_UPDATE:            // 업데이트 필수
                {
#if !UNITY_WEBGL
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                        popup =>
                        {
                            popup.SetSystemPopupController(
                                Momo.Localization.Get("maintenance_recommend_title"),
                                Momo.Localization.Get("maintenance_require_desc"));
                        },
                        onCloseAction: () =>
                        {
                            Application.Quit();
                        });
#endif
                    return false;
                }

            case GamebaseLaunchingStatus.BLOCKED_USER:            //접속 차단 사용자
                {
                    PopupManager.Instance.OpenPopup<CommonPopupController>("CommonPopupController",
                        popup =>
                        {
                            popup.SetSystemPopupController(
                                Momo.Localization.Get("maintenance_ban_title"),
                                Momo.Localization.Get("maintenance_ban_desc"));
                        },
                        onCloseAction: () =>
                        {
                            Application.Quit();
                        });
                    return false;
                }
            case GamebaseLaunchingStatus.IN_SERVICE:            //정상 서비스
            case GamebaseLaunchingStatus.IN_TEST:               //테스트(개발) 서비스
            case GamebaseLaunchingStatus.IN_BETA:               //베타(QA) 서비스
            case GamebaseLaunchingStatus.IN_REVIEW:             //리뷰(마켓 심사) 서비스
            case GamebaseLaunchingStatus.IN_SERVICE_BY_QA_WHITE_LIST:        //점검중 테스트 단말 서비스
                return true;

            default:
                {

                    return false;
                }
        }
    }

    public void PlayAni()
    {
        splashAnimation.Play();
    }

    public void StopAni()
    {
        splashAnimation.Stop();
    }
}
