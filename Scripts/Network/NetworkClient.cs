#define DEV_APP_LOG
using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using UniRx.Async;
using MomoPop.Util;
using XDR;
using Toast.Gamebase;

public class NetworkClientException : Exception
{
    public STAR_ErrCode errCode;
    public NetworkClientException(STAR_ErrCode errCode, string msg)
        : base(msg)
    {
        this.errCode = errCode;
    }
}
public sealed class NetworkClient : Singleton<NetworkClient>
{
    private static Dictionary<int, Type> RequestTxProtocolTypes { get; set; }
    private static Dictionary<int, Type> RequestProtocolTypes { get; set; }
    private static Dictionary<int, Type> PushProtocolTypes { get; set; }

    static NetworkClient()
    {
        RequestTxProtocolTypes = ProtocolExporter.GetReqTxProtocolTypes();
        RequestProtocolTypes = ProtocolExporter.GetResProtocolTypes();
        PushProtocolTypes = ProtocolExporter.GetPushProtocolTypes();
    }


    public bool ServerConnected { get; private set; } = false;

    private global::EBS.Dispatcher _dispatcher = new global::EBS.Dispatcher();
    public global::EBS.Dispatcher dispatcher
    {
        get { return _dispatcher; }
    }

    // 패킷 전송 시작, 종료를 알리기 위한 씬 핸들러.    
    private List<SceneHandler> linkedSceneHandlerList = new List<SceneHandler>();

    public void LinkSceneHandler(SceneHandler sceneHandler)
    {
        linkedSceneHandlerList.Add(sceneHandler);
    }

    public void UnlinkSceneHandler(string sceneHandlerName)
    {
        linkedSceneHandlerList.RemoveByUnityObjectName<SceneHandler>(sceneHandlerName);
    }

    private string commonURL;
    public string serverVersion = "/v1_0/";

    public static bool IsNetworkOffline
    {
        get { return false; }
    }

    [SerializeField]
    private bool applyPatchForEditor;
    public bool ApplyPatchForEditor { get { return applyPatchForEditor; } }
    public bool ApplyPatch
    {
        get
        {
#if UNITY_EDITOR
            return ApplyPatchForEditor;
#else
            return true;
#endif
        }
    }

    public string serverAddress
    {
        get
        {
            return GamebaseController.Platform?.GameServerAddress ?? "";
        }
    }


    [SerializeField] private int retryCount = 1;
    private readonly TimeSpan timeout = TimeSpan.FromSeconds(3);
    private readonly TimeSpan syncTimeout = TimeSpan.FromSeconds(5);
    private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    private readonly Dictionary<string, string> httpHeader = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
#if !UNITY_WEBGL
            { "Accept-Encoding", "gzip" }
#endif
        };
    private SPEAK_REQ_Header requestHeader = new SPEAK_REQ_Header();

    public void ResetTransaction()
    {
        Logger.Log("[ObservableNetworkClinet] Reset network transaction...");
        txID = -1;
    }
    private int txID = -1;
    private int TransactionID
    {
        get { return txID; }
        set
        {
            if (txID < value)
                txID = value;
        }
    }

    // 게임 로그인 시, 서버로부터 리뷰 상태 여부를 전달 받는다.
    private bool isReviewState = false;

    public static bool IsReviewState
    {
        get
        {
            // 오프라인 상태를 리뷰 상태와 동일하게 취급한다.
            if (NetworkClient.IsNetworkOffline || Instance.isReviewState)
            {
                return true;
            }
            return false;
        }
    }

    // 게임에 처음 들어온 유저
    private bool isNewUser = false;
    public static bool IsNewUser
    {
        get
        {
            // 오프라인 상태를 리뷰 상태와 동일하게 취급한다.
            return Instance.isNewUser;
        }
    }

    // 게임을 시작한 이후 지나간 일 수.
    public int PlayDays { get; private set; }

    // 현재 실 서비스 운영중인지 여부 확인.
    public static bool IsRealServiceState
    {
        get
        {
            if (Instance == null || string.IsNullOrEmpty(Instance.commonURL))
                return false;

            return
                !Instance.commonURL.Contains("dev")
                && !Instance.commonURL.Contains("alpha")
                && !Instance.commonURL.Contains("beta");
        }
    }

    // 로그인 여부.
    public static bool IsLoggedIn
    {
        get
        {
            if (!string.IsNullOrEmpty(Instance.requestHeader.userNo) &&
                Instance.requestHeader.sessionId != 0)
            {
                return true;
            }
            return false;
        }
    }

    // 요청을 처리중인지 여부.
    public static bool Processing { get; private set; }

    public Dictionary<Type, Action<IMessage>> AdditionalPushMessageHandler = new Dictionary<Type, Action<IMessage>>();

    private readonly Dictionary<Type, Action<IMessage>> pushMessageHandlers = new Dictionary<Type, Action<IMessage>>
        {
            { typeof(STAR_PUSH_MailCount), msg =>
            {
                var mailCountMsg = msg as STAR_PUSH_MailCount;
                if (mailCountMsg != null)
                {
                    //MomoPop.Service.GameService.MailBox.MailCount = mailCountMsg.friendMailCount + mailCountMsg.giftMailCount;
                }
            }},
        };

    public override void Awake()
    {
        base.Awake();
    }

    public void Initialize()
    {
        
    }

    public void InitCommonURL(string hspServerAddress)
    {
        commonURL = GamebaseController.Platform.GameServerAddress;
        if (string.IsNullOrEmpty(commonURL) && !string.IsNullOrEmpty(hspServerAddress))
            commonURL = hspServerAddress;
    }

    public void SetUserNo(string userID)
    {
        Logger.Log($"SetUserNo({userID ?? ""})");

        requestHeader.userNo = userID;

        //if (!Application.isEditor)
        //{
        //	SingularSDK.SetCustomUserId(userID);
        //	ObservableNetworkClient.Instance.SendEventToSingular("Create SNO");
        //}
    }

    public void ClearRequestHeader()
    {
        ResetTransaction();
        requestHeader = new SPEAK_REQ_Header();
    }

    private async Task<Response> postMessage(IMessage message, int timeout)
    {

        var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            requestHeader.txId = RequestTxProtocolTypes.ContainsKey(message.GetID()) ? TransactionID : -1;
            requestHeader.Save(bw);
            message.Save(bw);
            bw.Flush();
        }

        var msgUrl = $"{commonURL}{message.GetName()}";
#if UNITY_EDITOR
        Logger.Log($"<color=green><b>{message.GetName()}</b></color> {JSON.ToJson(requestHeader)}\n<b>CONTENTS</b> {JSON.ToJson(message)}\n<b>URL</b> {commonURL}");
#elif BUILD_DEV
        //if (GameConfig.Instance.useLogViewer) // 위에서 Define 로 나눴으니 여기서는 체크 안한다.
        {
            if(message.GetName() != "SPEAK_REQ_ScanItSearch")
                Logger.Log($"<color=green><b>{message.GetName()}</b></color> {JSON.ToJson(requestHeader)}\n<b>CONTENTS</b> {JSON.ToJson(message)}\n<b>URL</b> {commonURL}");
        }
#endif


        using (var request = new UnityWebRequest(msgUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.timeout = timeout;
            request.uploadHandler = new UploadHandlerRaw(ms.ToArray());
            request.uploadHandler.contentType = "application/octet-stream";
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            sw.Restart();
            await request.SendWebRequest();
            await waitForDownload(request);

            return TranslateResponse(request);
        }
    }


    private IEnumerator waitForDownload(UnityWebRequest www)
    {
        while (www.downloadHandler.isDone == false)
        {
            if(www.isNetworkError || www.isHttpError)
                break;

            yield return null;
        }
    }

    private IEnumerator waitForDownloadTexture(UnityWebRequest www)
    {
        while (www.downloadHandler.isDone == false)
        {
            if (www.isNetworkError || www.isHttpError)
                break;

            yield return null;
        }
    }

    public async void LoadImage(string url, System.Action<Texture2D> call)
    {
        using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            request.downloadHandler = new DownloadHandlerTexture();
            await request.SendWebRequest();
            await waitForDownloadTexture(request);

            Texture2D mTex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);

            if (request.isNetworkError)
            {
                return;
            }

            //SaveCache(imageUrl, mTex);

            if (call != null)
                call(mTex);

        }
    }

    public async Task<Response> SendRequest(IMessage message, bool notOpenLoading = false, TimeSpan? timeoutOverride = null)
    {
        Processing = true;

        if (!notOpenLoading)
        {
            linkedSceneHandlerList.ForEach((handler) => handler.OnStartNetworkLoading());
        }

        var response = await postMessage(message, (int)(timeoutOverride ?? timeout).TotalSeconds);
        if (response != null && response.header != null)
        {
            while (response.header.errCode == -1)
            {
                GamebaseController.Instance.FinishLoading(true);
                var closePopup = new TaskCompletionSource<bool>();
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                   {
                       string title = Momo.Localization.Get("popup_network_title");
                       string errMsg = $"{Momo.Localization.Get("popup_network_message")}({response.header.errCode})";
                       popup.SetSystemPopupController(title, errMsg);
                   },
                    onCloseAction: () =>
                    {
                        closePopup.SetResult(true);
                    });
                await closePopup.Task;
                response = await postMessage(message, (int)(timeoutOverride ?? timeout).TotalSeconds);
            }

            if (!notOpenLoading)
            {
                linkedSceneHandlerList.ForEach((handler) => handler.OnFinishNetworkLoading(false));
            }

            if (message.GetID() == SPEAK_REQ_Login.nMsgID)
            {
                var loginMsg = response.GetMessage<SPEAK_RES_Login>();
                if (loginMsg != null)
                {
                    requestHeader.sessionId = loginMsg.sessionId;
                    GameData.sessionId = requestHeader.sessionId;
                    isReviewState = loginMsg.isReviewServer;
                    Toast.ToastSdk.UserId = MPServiceFacade.Platform.UserID;
//#if DEV_APP_LOG
//                Gamebase.Logger.Initialize(new GamebaseRequest.Logger.Configuration("Vcw04gH1QIjXbHZB"));
//#else
                    Gamebase.Logger.Initialize(new GamebaseRequest.Logger.Configuration("FT7pRcpeElmqqZxp"));
//#endif

                    GlobalSetting.Instance.cdn_url = loginMsg.cdnUrl;
                    GlobalSetting.Instance.review_url = loginMsg.reviewUrl;
                    GlobalSetting.Instance.support_url = loginMsg.supportUrl;
                    GlobalSetting.Instance.oc_url = loginMsg.ocUrl;
                }
            }
        }
        Processing = false;
        return response;
    }

    private Response TranslateResponse(UnityWebRequest www)
    {
        sw.Stop();
        var response = new Response();
        if (!www.error.IsNullOrEmpty())
            Debug.Log("www.errror:" + www.error);
        if (www.isNetworkError || www.isHttpError)
        {
            response.header = new SPEAK_RES_Header { errCode = -1, errMsg = www.error };
        }
        else
        {
            try
            {
                var compression = !string.IsNullOrEmpty(www.GetResponseHeader("gzip"));
                var ms = (compression) ? new MemoryStream(decompress(www.downloadHandler.data)) : new MemoryStream(www.downloadHandler.data);
                string timeText = $"<color=blue>[{sw.Elapsed:ss':'fff}]</color>";
                using (var br = new BinaryReader(ms))
                {
                    IPAddress.NetworkToHostOrder(br.ReadInt32());
                    ms.Seek(-sizeof(int), SeekOrigin.Current);

                    // Translate header
                    response.header = new SPEAK_RES_Header();
                    response.header.Load(br);

                    TransactionID = response.header.nextTxId;

                    var errCode = (STAR_ErrCode)response.header.errCode;
                    if (errCode != STAR_ErrCode.ERR_SUCCESS)
                    {
                        Logger.Log($"{JSON.ToJson(response.header)}{timeText} : <color=red>{(STAR_ErrCode)response.header.errCode}({response.header.errCode:X8})</color>");
                        if (errCode != STAR_ErrCode.ERR_UNMATCH_ITEM_COUNT)  // 230421 - 매치아이템 에러일때는 메시지를 살리기 위해 익셉션쓰로우 하지 않는다.)
                        {
                            throw new NetworkClientException(errCode, response.header.errMsg);
                        }
                    }


                    while (ms.Length > ms.Position)
                    {
                        int msgLen = IPAddress.NetworkToHostOrder(br.ReadInt32());
                        int msgId = IPAddress.NetworkToHostOrder(br.ReadInt32());
                        ms.Seek(-sizeof(int), SeekOrigin.Current);

                        bool isPush = false;
                        Type type = null;
                        if (RequestProtocolTypes.ContainsKey(msgId))
                        {
                            type = RequestProtocolTypes[msgId];
                        }
                        else if (PushProtocolTypes.ContainsKey(msgId))
                        {
                            type = PushProtocolTypes[msgId];
                            isPush = true;
                        }
                        else
                        {
                            throw new NetworkClientException(STAR_ErrCode.ANS_COMMON_FATAL_ERROR, $"Unregistered Protocol: {msgId}");
                        }
                        //Logger.Log(type.Name + ", " + msgId + ", " + msgLen);


                        IMessage message = (IMessage)type.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null);
                        message.Load(br);
#if UNITY_EDITOR
                        Logger.Log($"<color=green><b>{message.GetName()}</b></color> {JSON.ToJson(requestHeader)}\n<b>CONTENTS</b> {JSON.ToJson(message)}\n<b>URL</b> {commonURL}");
#elif BUILD_DEV
                        //if (GameConfig.Instance.useLogViewer) // 위에서 Define 로 나눴으니 여기서는 체크 안한다.
                            Logger.Log($"<color=green><b>{message.GetName()}</b></color> {JSON.ToJson(requestHeader)}\n<b>CONTENTS</b> {JSON.ToJson(message)}\n<b>URL</b> {commonURL}");
#endif

                        if (isPush)
                        {
                            Action<IMessage> handler = null;
                            if (pushMessageHandlers.TryGetValue(message.GetType(), out handler))
                            {
                                handler.Invoke(message);
                            }

                            if (AdditionalPushMessageHandler.TryGetValue(message.GetType(), out handler))
                            {
                                handler.Invoke(message);
                            }
                        }
                        else
                        {
                            response.message = message;
                        }
                    }
                }
            }
            catch (NetworkClientException ex)
            {
                Logger.LogError(ex.Message);
                response.header = new SPEAK_RES_Header
                {
                    errCode = (int)ex.errCode,
                    errMsg = ex.Message,
                };
            }
        }

        return response;
    }

    private byte[] decompress(byte[] data)
    {
        using (var zipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];

            using (var resultStream = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = zipStream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        resultStream.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return resultStream.ToArray();
            }
        }
    }
}
