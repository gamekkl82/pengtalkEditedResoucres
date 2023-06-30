using System;
using System.Threading.Tasks;

using UnityEngine;
using XDR;

public enum MPServiceResultCode
{
	Success = 0,
	NoHandler,
	ServiceUnavailable,
	InvalidUserSession,
	InvalidStageSessionState,
	InvalidStageSessionSequence,
	InvalidZoneNo,
	InvalidStageNo,
	NotEnoughHeartItem,
	NotEnoughPreGameItem,
	InvalidStageContinueSequence,
	InvalidStageFailType,
	InvalidStageContinueTicket,
	NotEnoughItem,
	InvalidProductId,
	NotEnoughIngameItem,
	InvalidBuyItemId,
	AlreadyOpenedZone,
	ZoneNotCompleted,
	InvalidPlayerContext,
	InvalidAchievementType,
	NotAchieved,
	AlreadyReceivedReward,
	InvalidTransportId,
	MailExpired,
	InvalidMailId,
    InvalidZoneCollectionType,
    AlreadyReceivedZoneCollectionReward,
	InvalidEventId,
	GameServerLoginFailed,
	ItemSyncFailed,
	ZoneStageSyncFailed,
	AchievementSyncFailed,
	ConstellationCollectionSyncFailed,
	ItemLogSyncFailed,
	StageLogSyncFailed,
	AchievementLogSyncFailed,
	WithdrawFailed,
	EnterLobbyFailed,
	LoadConfigFailed,
	InvalidLogDatabase,
	GetHeartRequestedFailed,
	GetHeartSentFailed,
	AuthenticationFailed,
	SwitchAccountFailed,
	GetFacebookFriendsIDFailed,
	SendHeartGiftFailed,
	SendHeartRequestFailed,
	MailListFailed,
	SwitchAccountTokenFailed,
	AcceptMailFailed,
	RemoveMappingFailed,
	AccountMappingFailed,
	QueryCoinItemListFailed,
	PurchaseCoinFailed,
	PurchaseConsumeFailed,
	MarketPurchaseFailed,
	//LobbyInfoFailed,
	CoinShopFailed,
	ServicePrerequisiteFailed,
	PlatformInitializationFailed,
	InvalidLoginType,
	LogoutFailed,
	NetworkNotConnected,
	ConfigNotRevisioned,
	OnlyFacebookIDPExists,
	NeedLoginWithAlternativeIDP,
	LinkFacebookFailed,
	UnlinkFacebookFailed,
	GameDataSyncFailed,
	InvalidItemId,
	GetNoticeListFailed,
	GetProfileFailed,
    
    AccountCodeWithDrawFailed,
    //UserStudyDataFailed,
    GameStartFailed,
    OutOfService,
	TotalStudyDataFailed,
	RtcsInfoFailed,
	WorldDataFailed,
	LetsTalkDataFailed,
	StudyGamePlayFailed,
	GamebaseFailed,
	// 2차
	GetGameDataFailed,
	GetPhonicsDataFailed,
	MakePassportFailed,
	AttendanceFailed,
	GetLobbyInfoFailed,
	WorldStartActivityFailed,
	WorldFinishActivityFailed,
	ThemeStartActivityFailed,
	ThemeFinishActivityFailed,
	SpeakingStartActivityFailed,
	SpeakingFinishActivityFailed,
	PhonicsStartActivityFailed,
	PhonicsFinishActivityFailed,
	WorldStartTunaActivityFailed,
	WorldFinishTunaActivityFailed,
	LetsTalkStartEpisodeFailed,
	LetsTalkFinishEpisodeFailed,
	GetStudyRankingFailed,
	GetAlarmFailed,
	ActivityStatusFailed,
	TutorialFailed,
	ScanItSearchFailed,
	ScanItStartFailed,
	RTCS_ReqStartFailed,
	RTCS_ReqSendFailed,
	RTCS_ReqSendSchooltalkFailed,
	STT_AccessTokenFailed,
	SPEECH_AuthAccessTokenFailed,
	SchoolTalk_RoomInfoFailed,
	SchoolTalk_ConversationFailed,
	SchoolTalk_MessageFailed,
	AccountCodeFailed,
	PasswordFailed,
	GameRecordFailed,
	ContentScoreFailed,
	RecordActivityFailed,
	RecordWorldFailed,
	RecordLetsTalkFailed,
	StudyRecordFailed,
	
	MatchItemFailed,
    CostumeInvenListFailed,
    CostumeProductsFailed,
	CostumePurchaseProductFailed, 
    CostumePutOnFailed,
    CostumePutOffFailed,

	// 230510 - 우편함, 획득내역 에러 추가 - 임시로 마지막 번호에서 내려옴.
	SPEAK_RES_RewardTunaFailed = 989,
	SPEAK_RES_Mailbox_ReceiveAllFailed = 990,
	SPEAK_RES_Mailbox_ReceiveFailed = 991,
	SPEAK_RES_MailboxFailed = 992,
	// HGKIM 230215 - 랭킹에러 추가 - 임시로 마지막 번호에서 내려왔다.
	SPEAK_RES_RankingPrize_ReceiveFailed = 993,
	SPEAK_RES_RankingPrizeFailed = 994,
	// HGKIM 221201 - 비밀번호 변경 실패 코드 추가
	SPEAK_REQ_Password_ChangeFailed = (int)0x0C010000,
	// HGKIM 220929 - 임시로 마지막 번호에 등록
	SPEAK_REQ_Password_Change = 995,    //  패스워드 변경
	// HGKIM 220727 - 임시로 마지막 번호에 등록했다.	// 999는 UNKNOWN_ERROR 라서 998부터 역순으로 지정
	SPEAK_REQ_PushNoticeFailed = 996,
	SPEAK_REQ_PushNotice_InsertLog,
	SPEAK_REQ_PushNotice_Study = 998 // HGKIM 220803 - 추가하면서 위에 번호들 하나씩 밀림
}

public struct MPServiceResult
{
	public MPServiceResultCode Code;
	public XDR.IMessage message;
	public string Contents;
	public int GamebaseErrorCode;

	public bool IsSuccess => Code.IsSuccess();
	public T GetContents<T>() => JsonUtility.FromJson<T>(Contents);
	public T GetMessage<T>() where T : XDR.IMessage => message as T;

	public static MPServiceResult Fail(MPServiceResultCode result, int gamebaseErrorCode = Toast.Gamebase.GamebaseErrorCode.UNKNOWN_ERROR)
	{
		return new MPServiceResult
		{
			Code = result,
			Contents = null,
			message = null,
			GamebaseErrorCode = gamebaseErrorCode
		};
	}
	public static MPServiceResult Success()
	{
		return new MPServiceResult
		{
			Code = MPServiceResultCode.Success,
			Contents = null,
			message = null
		};
	}
	public static MPServiceResult Success(string contents)
	{
		return new MPServiceResult
		{
			Code = MPServiceResultCode.Success,
			Contents = contents,
			message = null
		};
	}
	public static MPServiceResult Success(object contents)
	{
		return new MPServiceResult
		{
			Code = MPServiceResultCode.Success,
			Contents = JsonUtility.ToJson(contents, true),
			message = null
		};
	}

	public static MPServiceResult Success(XDR.IMessage msg)
	{
		var result = Success(msg as object);
		result.message = msg;
		return result;
	}
}

public static class MPServiceExtensions
{
	public static bool IsSuccess(this MPServiceResultCode result)
	{
		return result == MPServiceResultCode.Success;
	}

	public async static Task<MPServiceResult> Request<T>(this IMPService service, string serviceName, T obj)
	{
		return await service.Request(serviceName, JsonUtility.ToJson(obj));
	}

	public async static Task<MPServiceResult> Request<T>(this IMPService service, string serviceName, IMessage msg)
	{
		return await service.Request(serviceName, msg);
	}

	//public static IObservable<MPServiceResult> AsObservable(this string serviceName)
	//{
	//	return GameService.Instance.ObserveRequest(serviceName);
	//}

	//public static IObservable<string> WhenSucceeded(this IObservable<MPServiceResult> observable)
	//{
	//	return observable.Where(r => r.IsSuccess).Select(r => r.Contents);
	//}

	//public static IObservable<T> WhenSucceeded<T>(this IObservable<MPServiceResult> observable)
	//{
	//	return observable.Where(r => r.IsSuccess).Select(r => r.GetContents<T>());
	//}

	//public static IObservable<MPServiceResultCode> WhenFailed(this IObservable<MPServiceResult> observable)
	//{
	//	return observable.Where(r => !r.IsSuccess).Select(r => r.Code);
	//}
}

public interface IMPService
{
	Task<MPServiceResult> Request(string serviceName, string contentsJson);
	Task<MPServiceResult> Request(string serviceName, IMessage msg);
	//IObservable<MPServiceResult> ObserveRequest(string serviceName);
}
