using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UniRx.Async;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using System.Threading.Tasks;
using Toast.Gamebase;

public class Utility
{
    // 권한 타입 enum
    public enum PermissionType
    { 
        CAMERA = 0,
        MIC,
        GALLERY
    }

    static public T AddNewChild<T>(GameObject parent, T prefab) where T : Component
    {
        T inst = GameObject.Instantiate(prefab) as T;

        if (inst != null && parent != null)
        {
            Transform t = inst.gameObject.transform;
            t.SetParent(parent.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        return inst;
    }

    public static void OpenAppSetting()
    {
        try
        {
#if UNITY_ANDROID
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string packageName = currentActivityObject.Call<string>("getPackageName");

                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null))
                using (var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject))
                {
                    intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                    intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
                    currentActivityObject.Call("startActivity", intentObject);
                }
            }
#elif UNITY_IOS
            string url = NativeiOSPlugins.GetSettingsURL();
            Application.OpenURL(url);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public static TEnum ParseEnum<TEnum>(string item, bool ignorecase = default(bool))
            where TEnum : struct
    {
        TEnum tenumResult = default(TEnum);
        return Enum.TryParse<TEnum>(item, ignorecase, out tenumResult) ?
               tenumResult : default(TEnum);
    }

    public static DateTime GetDateTime(string date, string pattern = "yyyyMMddHHmmss")
    {
        DateTime parsedDate;
        if (DateTime.TryParseExact(date, pattern, null, DateTimeStyles.None, out parsedDate))
            return parsedDate;
        else
        {
            Debug.Log($"Parse Error.{date}");
            return DateTime.MaxValue;
        }
    }

    public static string GetSecondToSimpleTime(int sec)
    {
        int min = sec / 60;
        int second = sec % 60;
        return $"{min.ToString("D2")}:{second.ToString("D2")}";
    }

    public static string GetSimpleDate(string date, string pattern = "yyyyMMddHHmmss")
    {
        string result;
        DateTime parsedDate = GetDateTime(date, pattern);
        TimeSpan subTime;
        string v;
        if (DateTime.Now > parsedDate)
        {
            subTime = DateTime.Now - parsedDate;
            v = "전";
        }
        else
        {
            subTime = parsedDate - DateTime.Now;
            v = "후";
        }

        if (subTime.Days > 1)
        {
            result = $"{parsedDate.ToString("yyyy.MM.dd")}";
        }
        else if (subTime.Days == 1)
        {
            result = $"{subTime.Days}일 {v}";
        }
        else if (subTime.Hours > 1)
        {
            result = $"{subTime.Hours}시간 {v}";
        }
        else
        {
            result = $"{subTime.Minutes}분 {v}";
        }

        return result;
    }

    // 연월일만 나오는걸 추가 - 우편함 표기용
    public static string GetSimpleDate_Day(string date, string pattern = "yyyyMMdd")
    {
        string result;
        DateTime parsedDate = GetDateTime(date, pattern);
        TimeSpan subTime;
        string v;
        if (DateTime.Now > parsedDate)
        {
            subTime = DateTime.Now - parsedDate;
            v = "전";
        }
        else
        {
            subTime = parsedDate - DateTime.Now;
            v = "후";
        }

        if (subTime.Days > 1)
        {
            result = $"{parsedDate.ToString("yyyy.MM.dd")}";
        }
        else if (subTime.Days == 1)
        {
            result = $"{subTime.Days}일 {v}";
        }
        else
        {
            result = $"1일 이하 {v}";
        }

        return result;
    }

    public static string SecondToSimpleDate(float seconds)
    {
        TimeSpan timespan = TimeSpan.FromSeconds(seconds);
        int hour = timespan.Hours;
        int min = timespan.Minutes;
        int sec = timespan.Seconds;

        string time = string.Empty;
        if (hour > 0)
        {
            time = $"{hour}시간 ";
        }

        if (hour > 0 || min > 0)
        {
            time += $"{min}분";
        }
        else
        {
            time = "0분";
        }

        return time;
    }

    public static void PlayAni(DOTweenAnimation ani)
    {
        ani.tween.Rewind();
        ani.tween.Kill();
        if (ani.isValid)
        {
            ani.CreateTween();
            ani.tween.Play();
        }
    }

    public static string GetPriceDisplay(int tuna)
    {
        return tuna == 0 ? "Free" : $"{tuna}";
    }

    public static bool EnoughtTuna(int tuna)
    {
        int invenTuna = GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count;
        return tuna <= invenTuna;
    }

    public static bool UseTuna(int tuna)
    {
        if (EnoughtTuna(tuna))
        {
            GameData.inventory.SubItem(GameData.ItemId.Coin, tuna);
            return true;
        }


        return false;
    }

    public static IEnumerator Start()
    {
        findWebCams();

        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("webcam found");
        }
        else
        {
            Debug.Log("webcam not found");
        }

        findMicrophones();

        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("Microphone found");
        }
        else
        {
            Debug.Log("Microphone not found");
        }
    }

    public static void findWebCams()
    {
        foreach (var device in WebCamTexture.devices)
        {
            Debug.Log("Name: " + device.name);
        }
    }

    public static void findMicrophones()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }
    }

    public static bool HasCameraPermission()
    {
        bool permission = false;
#if UNITY_ANDROID
        permission = Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS
        permission = Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
        permission = Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
        return permission;
    }
    public static bool HasMicrophonePermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#elif UNITY_IPHONE
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
    }

    public static bool HasGalleryWritePermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
#elif UNITY_IOS
        return NativeGallery.CheckPermission(NativeGallery.PermissionType.Write) == NativeGallery.Permission.Granted;
#else
        return NativeGallery.CheckPermission(NativeGallery.PermissionType.Write) == NativeGallery.Permission.Granted;
#endif
    }


    public static IEnumerator RequestMicrophone()
    {
#if UNITY_ANDROID
        Permission.RequestUserPermission(Permission.Microphone);
        yield return null;
#elif UNITY_IPHONE
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
#else
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
    }

    public static IEnumerator RequestCamera()
    {
#if UNITY_ANDROID
        Permission.RequestUserPermission(Permission.Camera);
        yield return null;
#elif UNITY_IOS
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#else
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
    }

    public static IEnumerator RequestGallery()
    {
#if UNITY_ANDROID
        Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        yield return null;
#elif UNITY_IOS
        yield return NativeGallery.RequestPermission(NativeGallery.PermissionType.Write);
#else
        yield return NativeGallery.RequestPermission(NativeGallery.PermissionType.Write);
#endif
    }

    public static async void RequestPermissionAndMoveSetting(PermissionType type)
    {
        bool hasPermission = CheckPermissionByType(type);

        if (hasPermission == false)
        {
            switch (type)
            {
                case PermissionType.CAMERA:
                    await RequestCamera();
                    break;
                case PermissionType.MIC:
                    await RequestMicrophone();
                    break;
                case PermissionType.GALLERY:
                    await RequestGallery();
                    break;
            }

            await UniRx.Async.UniTask.Delay(200);
            await UniRx.Async.UniTask.WaitUntil(() => Application.isFocused == true);

            hasPermission = CheckPermissionByType(type);

            if (hasPermission == false)
            {
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                {
                    popup.SetSystemPopupController(
                        Momo.Localization.Get("popup_permission_title"),
                        GetPrmissionRequireText(type));
                },
                onCloseAction: () =>
                {
                    OpenAppSetting();
                });
            }
        }
    }

    public static string GetPrmissionRequireText(PermissionType type)
    {
        switch (type)
        {
            case PermissionType.CAMERA: return Momo.Localization.Get("popup_permission_camera");
            case PermissionType.MIC: return Momo.Localization.Get("popup_permission_microphone");
            case PermissionType.GALLERY: return Momo.Localization.Get("popup_permission_gallery_write");
            default: return "Invalid Permission Type";
        }
    }

    public static bool CheckPermissionByType(PermissionType type)
    {
        switch (type)
        {
            case PermissionType.CAMERA: return HasCameraPermission();
            case PermissionType.MIC: return HasMicrophonePermission();
            case PermissionType.GALLERY: return HasGalleryWritePermission();
            default: return false;
        }
    }

    public static void UI_MoveToKeyboardMode(RectTransform rt, bool isMove)
    {
        Vector3 oldPos = rt.localPosition;
        if (isMove)
        {
            rt.anchorMin = new Vector2(0, 0.4f);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 0);
        }

        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.one;

        Vector3 newPos = rt.localPosition;
        rt.localPosition = oldPos;
        iTween.MoveTo(rt.gameObject, iTween.Hash("position", newPos, "islocal", true, "time", 0.2f, "easetype", iTween.EaseType.easeOutCubic));
    }

    public static void CopyToClipboard(string text)
    {
#if !PLATFORM_STANDALONE
        UniClipboard.SetText(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }


    #region Firebase

    public static void Firebase_SetUserID(string userId)
    {
#if !PLATFORM_STANDALONE
        Firebase.Analytics.FirebaseAnalytics.SetUserId(userId);
#endif
    }

    public static void Firebase_SetUserProperty(string name, string property)
    {
#if !PLATFORM_STANDALONE
        Firebase.Analytics.FirebaseAnalytics.SetUserProperty(name, property);
#endif
    }

    public static void Firebase_SetCrrentScreen(string screenName, string screenClass)
    {
#if !PLATFORM_STANDALONE
        Firebase.Analytics.FirebaseAnalytics.SetCurrentScreen(screenName, screenClass);
#endif
    }

    public static void Firebase_LogEvent(string name)
    {
#if !PLATFORM_STANDALONE
        Firebase.Analytics.FirebaseAnalytics.LogEvent(name);
#endif
    }
    #endregion

    #region ToastLogNCrash
    public static void SendLogInfo(string message)
    {
        Toast.Logger.ToastLogger.Log(Toast.Logger.ToastLogLevel.INFO, message);
    }

    public static void SendLogWarn(string message, Dictionary<string, string> userFields = null)
    {
        Toast.Logger.ToastLogger.Log(Toast.Logger.ToastLogLevel.WARN, message, userFields);
    }
    #endregion ToastLogNCrash

    #region ToastGamebase
    public static void Gamebase_AddEventHandler()
    {
        Gamebase.RemoveAllEventHandler();

        Gamebase.AddEventHandler(GamebaseObserverHandler);
    }

    private static void GamebaseObserverHandler(GamebaseResponse.Event.GamebaseEventMessage message)
    {
        switch (message.category)
        {
            case GamebaseEventCategory.SERVER_PUSH_APP_KICKOUT:
            case GamebaseEventCategory.SERVER_PUSH_TRANSFER_KICKOUT:
                {
                    GamebaseResponse.Event.GamebaseEventServerPushData serverPushData = GamebaseResponse.Event.GamebaseEventServerPushData.From(message.data);
                    if (serverPushData != null)
                    {
                        Gamebase_CheckServerPush(message.category, serverPushData);
                    }
                    // HGKIM 220824 - iOS 에서 message.data 가 null로 날아와서 예외처리 추가
                    else
					{
#if UNITY_IOS           // iOS 일경우 data 가 null이 들어오면 그냥 꺼버린다.
                        Application.Quit();
#endif
                    }
                    break;
                }
			// HGKIM 220812 - 옵저버 사용하기 위해 주석 품
			case GamebaseEventCategory.OBSERVER_LAUNCHING:
				{
                    Logger.Log("HGKIM || OBSERVER_LAUNCHING");
					GamebaseResponse.Event.GamebaseEventObserverData observerData = GamebaseResponse.Event.GamebaseEventObserverData.From(message.data);
					if (observerData != null)
					{
						CheckLaunchingStatus(observerData);
					}
					break;
				}
			// HGKIM 220812 - 옵저버 사용하기 위해 주석 품
			//case GamebaseEventCategory.OBSERVER_NETWORK:
			//    {
			//        GamebaseResponse.Event.GamebaseEventObserverData observerData = GamebaseResponse.Event.GamebaseEventObserverData.From(message.data);
			//        if (observerData != null)
			//        {
			//            CheckNetwork(observerData);
			//        }
			//        break;
			//    }
			//case GamebaseEventCategory.OBSERVER_HEARTBEAT:
			//    {
			//        GamebaseResponse.Event.GamebaseEventObserverData observerData = GamebaseResponse.Event.GamebaseEventObserverData.From(message.data);
			//        if (observerData != null)
			//        {
			//            CheckHeartbeat(observerData);
			//        }
			//        break;
			//    }
			//case GamebaseEventCategory.OBSERVER_WEBVIEW:
			//    {
			//        GamebaseResponse.Event.GamebaseEventObserverData observerData = GamebaseResponse.Event.GamebaseEventObserverData.From(message.data);
			//        if (observerData != null)
			//        {
			//            CheckWebView(observerData);
			//        }
			//        break;
			//    }
			//case GamebaseEventCategory.OBSERVER_INTROSPECT:
			//    {
			//        // Introspect error
			//        GamebaseResponse.Event.GamebaseEventObserverData observerData = GamebaseResponse.Event.GamebaseEventObserverData.From(message.data);
			//        int errorCode = observerData.code;
			//        string errorMessage = observerData.message;
			//        break;
			//    }
			//case GamebaseEventCategory.PURCHASE_UPDATED:
			//    {
			//        GamebaseResponse.Event.PurchasableReceipt purchasableReceipt = GamebaseResponse.Event.PurchasableReceipt.From(message.data);
			//        if (purchasableReceipt != null)
			//        {
			//            // If the user got item by 'Promotion Code',
			//            // this event will be occurred.
			//        }
			//        break;
			//    }
			// HGKIM 220712 - 주석되어있는것 풀었음. 푸쉬 관련 동작 하는 부분
			case GamebaseEventCategory.PUSH_RECEIVED_MESSAGE:
				{
					GamebaseResponse.Event.PushMessage pushMessage = GamebaseResponse.Event.PushMessage.From(message.data);
					if (pushMessage != null)
					{
                        // When you received push message.
                        Logger.Log("HGKIM || GAMEBASE || PUSH_RECEIVED_MESSAGE!!!");

                        Dictionary<string, object> extras = Toast.Gamebase.LitJson.JsonMapper.ToObject<Dictionary<string, object>>(pushMessage.extras);
						// There is 'isForeground' information.
						if (extras.ContainsKey("isForeground") == true)
						{
							bool isForeground = (bool)extras["isForeground"];
                            //GameData.isTodayWordPush = false;

                            Logger.Log("HGKIM || GAMEBASE || PUSH_RECEIVED_MESSAGE is foreground!!!");
                        }
                        else
						{
                            //GameData.isTodayWordPush = true;
                            Logger.Log("HGKIM || GAMEBASE || PUSH_RECEIVED_MESSAGE is foreground false!!!");
                        }

                        Logger.Log($"HGKIM || GAMEBASE || PUSH_RECEIVED_MESSAGE : {pushMessage.extras}");
					}
					break;
				}
			case GamebaseEventCategory.PUSH_CLICK_MESSAGE:
				{
					GamebaseResponse.Event.PushMessage pushMessage = GamebaseResponse.Event.PushMessage.From(message.data);
					if (pushMessage != null)
					{
                        // When you clicked push message.
                        Logger.Log($"HGKIM || GAMEBASE || PUSH_CLICK_MESSAGE!!! : {message.data}");
					}
					break;
				}
			case GamebaseEventCategory.PUSH_CLICK_ACTION:
				{
					GamebaseResponse.Event.PushAction pushAction = GamebaseResponse.Event.PushAction.From(message.data);
					if (pushAction != null)
					{
                        // When you clicked action button by 'Rich Message'.
                        Logger.Log($"HGKIM || GAMEBASE || PUSH_CLICK_ACTION userText!!! : {pushAction.userText}");
                        Logger.Log($"HGKIM || GAMEBASE || PUSH_CLICK_ACTION body!!! : {pushAction.message.body}");
                        Logger.Log($"HGKIM || GAMEBASE || PUSH_CLICK_ACTION extras!!! : {pushAction.message.extras}");
                    }
					break;
				}
            // HGKIM 220712 - 여기까지
		}
    }

    // HGKIM 220812 - 주석 안의 함수가 없어서 추가
    private static void Gamebase_CheckServerPush(string category, GamebaseResponse.Event.GamebaseEventServerPushData data)
    {
        if (category.Equals(GamebaseEventCategory.SERVER_PUSH_APP_KICKOUT) == true)
        {
            // Kicked out from Gamebase server.(Maintenance, banned or etc.)
            // Return to title and initialize Gamebase again.
            Logger.Log("app kickdata :" + data.extras);
            //PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            //{
            //    popup.SetSystemPopupController("서버와 연결이 끊어졌습니다. 어플리케이션을 종료합니다.");
            //},
            //onCloseAction: () =>
            //{
            //    Application.Quit();
            //});
            // 220816 - 킥아웃 메시지가 뜨기 때문에 팝업을 한 번 더 띄울 필요가 없다.
            Application.Quit();
        }
        else if (category.Equals(GamebaseEventCategory.SERVER_PUSH_TRANSFER_KICKOUT) == true)
        {
            // If the user wants to move the guest account to another device,
            // if the account transfer is successful,
            // the login of the previous device is released,
            // so go back to the title and try to log in again.
            Logger.Log("transfer kickdata :" + data.extras);
        }
    }
    
    private static void CheckLaunchingStatus(GamebaseResponse.Event.GamebaseEventObserverData observerData)
    {
        switch (observerData.code)
        {
            case GamebaseLaunchingStatus.IN_SERVICE:
            {
                // Service is now normally provided.
                Logger.Log($"HGKIM || IN_SERVICE!!!!");
                break;
            }
            // 업데이트가 있을 경우
            case GamebaseLaunchingStatus.REQUIRE_UPDATE:
                Logger.Log($"HGKIM || REQUIRE_UPDATE!!!!");
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                {
                    popup.SetSystemPopupController("새로운 업데이트가 있습니다. 어플리케이션을 종료합니다.");
                },
                onCloseAction: () =>
                {
                    Application.Quit();
                });
                break;
            // 점검중일 경우
            case GamebaseLaunchingStatus.INSPECTING_SERVICE:
            case GamebaseLaunchingStatus.INSPECTING_ALL_SERVICES:
                Logger.Log($"HGKIM || INSPECTING_SERVICE!!!!");
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                {
                    popup.SetSystemPopupController("점검이 시작되었습니다. 어플리케이션을 종료합니다.");
                },
                onCloseAction: () =>
                {
                    Application.Quit();
                });
                break;
            // ... 
            case GamebaseLaunchingStatus.INTERNAL_SERVER_ERROR:
            {
                // Error in internal server.
                // HGKIM 220816 - 서버 에러시 발동
                Logger.Log($"HGKIM || INTERNAL_SERVER_ERROR!!!!");
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                {
                    popup.SetSystemPopupController("서버와의 연결이 끊어졌습니다. 어플리케이션을 종료합니다.");
                },
                onCloseAction: () =>
                {
                    Application.Quit();
                });
                    break;
            }
        }
    }

    #endregion ToastGamebase
}
