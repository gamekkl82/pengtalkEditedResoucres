using System;
using System.Collections;
using System.Collections.Generic;
using Toast.Gamebase.Internal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading.Tasks;
using UniRx;
public class CommonSceneHandler : SceneHandler
{
    public enum TopGroupType
    {
        Off,
        Left,
        Right,
        All,
    }
    /// <summary>
    /// GameData.EducationCode
    /// </summary>
    public string educationCode;
    /// <summary>
    /// lastTopicNo
    /// </summary>
    public int lastPlayNo = 0;
    public bool justNowCompleteTopic = false;

    protected string playingBGM = string.Empty;
    [Serializable]
    public class TopGroup
    {
        public GameObject TopObject;
        public Text coinText;
        public GameObject Tunacan;

    }

    [SerializeField] private TopGroup topGroup = null;
    [SerializeField] private GameObject topRightGroup = null;
    private TopGroupType activeTopGroup = TopGroupType.Off;

    //[SerializeField] private GameObject transition = null;
    [SerializeField] private GameObject imageTransition = null;
    public bool isTransitionScene = false;
    public bool isPlayingAnimation = false;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Camera commonCamera; // 펭수 대사 팝업(Ballon?)용 카메라
    [SerializeField] private Camera popupCamera;

    [SerializeField] private GameObject ballon;

    [SerializeField] private Text korText;
    [SerializeField] private Text engText;
    [SerializeField] private Text bigText;
    [SerializeField] private ContentSizeFitter sizeFitter;
    public bool isShowAttend = true;
    [SerializeField] private HoleMask holeMask;
    [SerializeField] private TutorialToolTip tip;
    
    private const int AdditionalCommonUICameraRenderDepth = 1;
    
    public override bool InitializeSceneHandler()
    {
        GameData.inventory.ObserveItemCount(GameData.ItemId.Coin).Subscribe(v =>
        {
            if (v > 999999)
            {
                topGroup.coinText.text = "+999,999";
            }
            else
            {
                topGroup.coinText.text = v.ToString("#,0");
            }

            //if (v >= ConstantValues.CommonDefine.MAX_HEART_COUNT)
            //    topObject.HeartChargeTime.text = "FULL";
        }).AddTo(this);
        return base.InitializeSceneHandler();
    }

    public async void GetLobbyMainInfo(Action onBegin = null, Action onFinished = null)
    {
        onBegin?.Invoke();

		// HGKIM 220726 - 오늘의 영단어 통신 추가
		GameData.ResetTodaysWordData();  // 220816 - 오늘의 영단어 데이터를 초기화시킨다.

        Logger.Log($"HGKIM || GetLobbyMainInfo || grdCD : {GameData.User.gradeCode} / Time : {DateTime.Now.ToString("yyyyMMdd")}");
        var resultPushNotice = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_PushNotice,
            new SPEAK_REQ_PushNotice
            {
                grdCd = GameData.User.gradeCode,
                studyDt = DateTime.Now.ToString("yyyyMMdd")
            });

        if (resultPushNotice.IsSuccess)
        {
            var data = resultPushNotice.message as SPEAK_RES_PushNotice;

            // 리턴되어 돌아온 데이터를 입력한다.
            GameData.SetTodaysWordData(data.pushNotice);
        }
        Logger.Log($"HGKIM || GetLobbyMainInfo || RequesSendEnd!!! result is {resultPushNotice.IsSuccess}");
        // HGKIM 220726 - 오늘의 영단어 추가 여기까지

        // HGKIM 220810 - 진급 조회 요청
        Logger.Log("HGKIM || GetLobbyMainInfo || SPEAK_REQ_GradeUp_Check Send");
        var resultGradeUp = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_GradeUp_Check, "{}");
        if (resultGradeUp.IsSuccess)
        {
            var data = resultGradeUp.message as SPEAK_RES_GradeUp_Check;
            
            Logger.Log($"HGKIM || SPEAK_RES_GradeUp_Check || isNewGrade? {data.canGradeUp}");

            // HGKIM 220804 - 진급처리 추가
            if (data.canGradeUp)
            {
                SceneController.Common?.OffBallon();    // 말풍선을 강제로 꺼준다.

                var taskCompletion = new TaskCompletionSource<bool>();

                // HGKIM 220819 - 선생님이 정보 입력을 했는지 여부
                if (data.existGradeUpInfo)
                {
                    PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                    {
                        string message = Momo.Localization.Get("gradeUp_message");
                        popup.SetSystemPopupControllerXButton(message,
                        okAction: async () =>
                        {
                            isOkButtonClicked = true;
                            await CheckedGradeUp();
                            taskCompletion.SetResult(true);
                        },
                        showXButton: true,
                        xAction: () => { taskCompletion.SetResult(true); }
                        );
                    },
                    onCloseAction: () =>
                    {
                    //taskCompletion.SetResult(true);
                    });
                }
                else
				{
                    PopupManager.Instance.OpenPopup<CommonPopupController>(popup=> 
                    {
                        popup.SetSystemPopupController(Momo.Localization.Get("gradeUp_NotGradeUpInfo_message"));
                    },
                    onCloseAction: () => 
                    {
                        taskCompletion.SetResult(true);
                    });
				}

                await taskCompletion.Task;
            }
            // HGKIM 220804 - 진급처리 여기까지
        }
        Logger.Log($"HGKIM || GetLobbyMainInfo || SPEAK_REQ_GradeUp_Check End! result : {resultGradeUp.IsSuccess}");
        // HGKIM 220810 - 진급 여기까지

        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetLobbyInfo, new Contents.LobbyInfo
        {
        });
        if(result.IsSuccess == false)
        {
            
            PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController(
                    Momo.Localization.Get("system_error_lobbyfail"));
            },
            onCloseAction: () =>
            {
                SceneController.Instance.BackToIntroFromLobby();
            });
            Debug.Log("[GetLobbyMainInfo] isSuccess = false");
            return;
        }

        // Common 씬의 UI 카메라 처리
        // ※씬 이동 시에 이벤트 처리를 할 수 있게 처리가 된다면 그쪽으로 옮겨야 맞습니다.
        CameraManager.Instance.AddCamera("Popup", popupCamera, (int)CameraManager.CamDepth.FRONT, false);

        CameraManager.Instance.AddCamera("Common", commonCamera, (int) CameraManager.CamDepth.FRONT + AdditionalCommonUICameraRenderDepth, false);
        // Common UI 렌더링하는 카메라는 항상 나중에 렌더링 해줘야 함

        onFinished?.Invoke();
    }

    // HGKIM 220818 - 진급처리용
    bool isOkButtonClicked = false;
    private async Task CheckedGradeUp()
    {
        var taskCompletion = new TaskCompletionSource<bool>();

        if (isOkButtonClicked)
        {
            PopupManager.Instance.OpenPopup<MyInfoEditPopupController>(
                popup =>
                {
                    popup.Bind();
                },
                onCloseAction: () =>
                {
                    taskCompletion.SetResult(true);
                });
        }

        await taskCompletion.Task;
    }

    public void SetActiveTopGroup(TopGroupType topGroupType)
    {
        activeTopGroup = topGroupType;
        if (activeTopGroup == TopGroupType.Off)
        {
            topGroup.TopObject.gameObject.SetActive(false);
            topRightGroup.SetActive(false);
        }
        else if (activeTopGroup == TopGroupType.All)
        {
            topGroup.TopObject.gameObject.SetActive(true);
            topRightGroup.SetActive(true);
        }
        else if (activeTopGroup == TopGroupType.Left)
        {
            topGroup.TopObject.gameObject.SetActive(true);
            topRightGroup.SetActive(false);
        }
        else if (activeTopGroup == TopGroupType.Right)
        {
            topGroup.TopObject.gameObject.SetActive(false);
            topRightGroup.SetActive(true);
        }
    }

    public GameObject GetCashObj()
    {
        return topGroup.Tunacan;
    }

    // 230316 - 출석 효과를 위해 추가
    public Text GetCashTextObj()
	{
        return topGroup.coinText;
    }

    public Vector3 ConvertPos(Vector3 position)
    {
        Vector3 viewport = popupCamera.WorldToViewportPoint(position);
        Vector3 pos = popupCamera.ViewportToWorldPoint(viewport);
        return pos;
    }

    public TopGroupType GetActiveTopGroup()
    {
        return activeTopGroup;
    }

    public bool GetActiveTopRightGroup()
    {
        return topRightGroup.activeSelf;
    }

    ///TransitionScene
    public System.Action<System.Action> ImageInTransition(int animationIndex, System.Action onComplete = null)
    {
        isTransitionScene = true;
        return CommonInTransition(animationIndex, imageTransition, onComplete);
    }

    public System.Action<System.Action> ImageOutTransition(int animationIndex, System.Action onComplete = null)
    {
        isTransitionScene = true;
        return CommonOutTransition(animationIndex, imageTransition, onComplete);
    }

    public System.Action<System.Action> ImageLobbyTransition(int animationIndex, System.Action onComplete = null)
    {
        isTransitionScene = true;
        return CommonLobbyTransition(animationIndex, imageTransition, onComplete);
    }

    private Action<Action> CommonInTransition(int animationIndex, GameObject trans, Action onComplete)
    {
        trans.SetActive(true);

        StartCoroutine(OnInTransitionScene("TransitionScene_0" + animationIndex, trans, onComplete, 0.5f));

        // Logger.Log( "[CommonSceneHandler/TransitionScene]Appear" );
        System.Action<System.Action> onRealComplete = (onRealRealComplete) =>
        {
            // Logger.Log( "[CommonSceneHandler/TransitionScene]Disappear" );
            StartCoroutine(OnTransitionCompleteScene(trans, onRealRealComplete, 0.5f));

        };

        return onRealComplete;
    }

    private Action<Action> CommonOutTransition(int animationIndex, GameObject trans, Action onComplete)
    {
        trans.SetActive(true);

        StartCoroutine(OnOutTransitionScene("TransitionScene_0" + animationIndex, trans, onComplete));

        // Logger.Log( "[CommonSceneHandler/TransitionScene]Appear" );
        System.Action<System.Action> onRealComplete = (onRealRealComplete) =>
        {
            // Logger.Log( "[CommonSceneHandler/TransitionScene]Disappear" );
            StartCoroutine(OnTransitionCompleteScene(trans, onRealRealComplete, 0.5f));

        };

        return onRealComplete;
    }

    private Action<Action> CommonLobbyTransition(int animationIndex, GameObject trans, Action onComplete)
    {
        trans.SetActive(true);

        StartCoroutine(OnInTransitionScene("TransitionScene_0" + animationIndex, trans, onComplete, .5f));

        // Logger.Log( "[CommonSceneHandler/TransitionScene]Appear" );
        System.Action<System.Action> onRealComplete = (onRealRealComplete) =>
        {
            // Logger.Log( "[CommonSceneHandler/TransitionScene]Disappear" );
            StartCoroutine(OnTransitionCompleteScene(trans, onRealRealComplete, 1.5f));

        };

        return onRealComplete;
    }

    private IEnumerator OnInTransitionScene(string animationName, GameObject trans, Action onComplete, float waitTime)
    {
        TouchController.Instance.BlockedTouch("TransitionScene");
        Animator anim = trans.GetComponentInChildren<Animator>();
        anim.SetBool("in", true);
 
        yield return new WaitForSeconds(waitTime);

        yield return new WaitForEndOfFrame();
        
        TouchController.Instance.ReleasedTouch("TransitionScene");
        if (null != onComplete)
            onComplete();
    }

    private IEnumerator OnOutTransitionScene(string animationName, GameObject trans, Action onComplete)
    {
        TouchController.Instance.BlockedTouch("TransitionScene");
        Animator anim = trans.GetComponentInChildren<Animator>();
        anim.SetBool("out", true);

        yield return new WaitForSeconds(0.5f);

        yield return new WaitForEndOfFrame();

        TouchController.Instance.ReleasedTouch("TransitionScene");
        if (null != onComplete)
            onComplete();
    }

    private IEnumerator OnTransitionCompleteScene(GameObject trans, Action action, float waitTime)
    {
        Animator anim = imageTransition.GetComponentInChildren<Animator>();
        TouchController.Instance.BlockedTouch("TransitionSceneFinished");
        yield return new WaitForSeconds(waitTime);
        anim.SetBool("end", true);
        //yield return new WaitForSeconds(anim.clip.length - 0.53f);
        TouchController.Instance.ReleasedTouch("TransitionSceneFinished");
        
        isTransitionScene = false;
        if (null != action)
            action();

        yield return new WaitForSeconds(0.5f);

        trans.SetActive(false);
    }

    public void EnableEvent()
    {
        eventSystem.enabled = true;
    }

    public void DisableEvent()
    {
        eventSystem.enabled = false;
    }

    public void PlayBGM(string bgmKey)
    {
        if(playingBGM == bgmKey)
        {
            return;
        }

        playingBGM = bgmKey;
        AudioController.StopMusic(.2f);
        AudioController.PlayMusic(playingBGM);
		Logger.Log("PlayBGM : " + playingBGM);
	}

    public void StopBGM()
    {
		Logger.Log("StopBGM");
		AudioController.StopMusic(.2f);
		playingBGM = string.Empty;
    }

    public void OnClickCash()
    {
        // 230405 - 기존코드 주석처리
        //PopupManager.Instance.OpenPopup<CashPopupController>( onBindAction: popup =>
        //    {
        //        // 230309 - 꾸미기 화면에서 Popup 떴을때에 펭수 뒤로 팝업이 가는 현상 해결을 위해 추가
        //        if (PopupManager.Instance.IsOpenedPopup("CostumePopupController"))
        //            CameraManager.Instance.SetCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
        //        popup.Bind();
        //    },
        //    onCloseAction: () =>
        //    {
        //        // 230309 - 꾸미기 화면에서 Popup 떴을때에 펭수 뒤로 팝업이 가는 현상 해결을 위해 추가
        //        if (PopupManager.Instance.IsOpenedPopup("CostumePopupController"))
        //            CameraManager.Instance.SetCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);
        //    });
        // 230405 - 캐쉬버튼 용도 변경
        if (SceneController.Lobby.OnClickTunaRecord().Equals(false))
            return;

        PopupManager.Instance.OpenPopup<TunaCanRecordPopupController>(onBindAction: popup =>
        {
            // 230309 - 꾸미기 화면에서 Popup 떴을때에 펭수 뒤로 팝업이 가는 현상 해결을 위해 추가
            if (PopupManager.Instance.IsOpenedPopup("CostumePopupController"))
                CameraManager.Instance.SetCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
            popup.Bind();
        },
        onCloseAction: () =>
        {
            // 230309 - 꾸미기 화면에서 Popup 떴을때에 펭수 뒤로 팝업이 가는 현상 해결을 위해 추가
            if (PopupManager.Instance.IsOpenedPopup("CostumePopupController"))
                CameraManager.Instance.SetCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);
        });
    }

    public void OnCamera()
    {
        //characterCamera.SetActive(true);
    }

    public void OffCamera()
    {
        //characterCamera.SetActive(false);
    }

    public void OnBallon()
    {
        ballon.SetActive(true);
    }

    public void OffBallon()
    {
        ballon.SetActive(false);
    }

    public void ShowText(string kor, string eng, string soundFile = "")
    {
        bool isEmpty = string.IsNullOrEmpty(eng);


        korText.text = kor;
        engText.text = eng;
        bigText.text = kor;

        korText.gameObject.SetActive(isEmpty == false);
        engText.gameObject.SetActive(isEmpty == false);
        bigText.gameObject.SetActive(isEmpty);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)sizeFitter.transform);

        if (string.IsNullOrEmpty(soundFile))
        {
            return;
        }

        AudioController.Play(soundFile);

    }

    public void OnClickSetting()
    {
        SceneController.Lobby.OnClickSetting();
    }

    public void OnClickNotice()
    {
        SceneController.Lobby.OnClickNotice();
    }

    public void OnClickHelp()
    {
        SceneController.Lobby.OnClickHelp();
    }

    // 230317 - 메일
    public void OnClickMail()
	{
        SceneController.Lobby.OnClickMail();
	}

    #region tutorial
    public void ShowHoleMask(RectTransform transform, bool isCircle = false, bool onFinger = true)
    {
        holeMask.ShowHoleMask_Target(transform, isCircle, onFinger:onFinger);
    }

    public void CloseHoleMask()
    {
        holeMask.gameObject.SetActive(false);
    }

    public void ShowTip(RectTransform rectTrans, eTipAlignment alignment, string msg, eTutorialAction actionType)
    {
        tip.Show(rectTrans, alignment, msg);
    }

    public void CloseTip()
    {
        tip.gameObject.SetActive(false);
    }

    #endregion
}
