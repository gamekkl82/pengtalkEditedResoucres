using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class NetworkServiceRequest
{
	
	public const string Withdraw = "Withdraw";
	public const string LoadGameConfig = "LoadGameConfig";
	public const string EnterCoinShop = "EnterCoinShop";
	public const string EnterSpecialOfferShop = "EnterSpecialOfferShop";
	public const string AlreadyHeartRequestedFriends = "AlreadyHeartRequestedFriends";
	public const string AlreadyHeartSentFriends = "AlreadyHeartSentFriends";
	public const string GetFriendsIDs = "GetFriendsIDs";
	public const string SendHearts = "SendHearts";
	public const string SendHeartRequests = "SendHeartRequests";
	public const string SyncMailList = "GetMailList";
	public const string AcceptMail = "AcceptMail";
	public const string BuyCoin = "BuyCoin";
	public const string PostProcessCoinPurchase = "PostprocessCoinPurchase";
	public const string LinkFacebook = "linkFacebook";
	public const string UnlinkFacebook = "unlinkFacebook";
	

	public const string SyncAll = "SyncAll";

	public const string SyncGameData = "SyncGameData";
	public const string SyncAllUserData = "SyncAllUserData";
	public const string SyncItemInven = "SyncItemInven";
	public const string SyncZoneAndStages = "SyncZoneAndStages";
	public const string SyncAchievement = "SyncAchievement";
	public const string SyncConstellationCollection = "SyncConstellationCollection";

	public const string SyncAllLogs = "SyncAllLogs";
	public const string SyncItemInvenLogs = "SyncItemInvenLogs";
	public const string SyncStageLogs = "SyncStageLogs";
	public const string SyncBonusStageLogs = "SyncBonusStageLogs";
	public const string SyncAchievementLogs = "SyncAchievementLogs";

	
//	public const string UserStudyData = "userStudyData";
	public const string GameStart = "gameStart";

	public const string TotalStudyData = "TotalStudyData";


	public const string RtcsInfo = "RtcsInfo";
	public const string RtcsUserTalk = "RtcsUserTalk";

	public const string WorldData = "WorldData";
	public const string LetsTalkData = "LetsTalkData";

	public const string StudyGamePlay = "StudyGamePlay";

	// 2차
	public const string Login = "Login";
	public const string AccountCode = "accountCode";
	public const string Key = "password";  // 취약성 이슈로 변수명 변경  22.06.28
	public const string AccountCodeWithdraw = "AccountCodeWithdraw";
	public const string MakePassport = "MakePassport";
	public const string GetProfile = "GetProfile";
	public const string GetGameData = "GetGameData";
	public const string GetPhonicsData = "GetPhonicsData";
	public const string Attendance = "Attedance";
	public const string GetNoticeList = "getNoticeList";
	public const string GetLobbyInfo = "GetLobbyInfo";
	public const string GetStudyRanking = "GetStudyRanking";
	public const string GetAlarm = "GetAlarm";
	public const string ActivityStatus = "ActivityStatus";
	public const string SPEAK_REQ_PushNotice = "SPEAK_REQ_PushNotice";						// HGKIM 220726 - 오늘의 영단어 - 요청
	public const string SPEAK_REQ_PushNotice_InsertLog = "SPEAK_REQ_PushNotice_InsertLog";	// HGKIM 220727 - 오늘의 영단어 - 완료처리
	public const string SPEAK_REQ_PushNotice_Study = "SPEAK_REQ_PushNotice_Study";			// HGKIM 220803 - 오늘의 영단어 - 수집한 단어 체크
	public const string SPEAK_REQ_GradeUp_Check = "SPEAK_REQ_GradeUp_Check";				// HGKIM 220809 - 진급 API - 새학년 체크 조회 요청
	public const string SPEAK_REQ_GradeUp_Store = "SPEAK_REQ_GradeUp_Store";				// HGKIM 220809 - 진급 API - 진급 처리 데이터 저장 요청
	public const string SPEAK_REQ_Password_Change = "SPEAK_REQ_Password_Change";			// HGKIM 220928 - 비밀번호 변경 API
	public const string SPEAK_REQ_RankingPrize = "SPEAK_REQ_RankingPrize";					// 230215 - 지난 랭킹(주간, 월간) 요청
	public const string SPEAK_REQ_RankingPrize_Receive = "SPEAK_REQ_RankingPrize_Receive";	// 230215 - 랭킹 보상 획득 전송
	public const string SPEAK_REQ_Mailbox = "SPEAK_REQ_Mailbox";							// 230510 - 우편함 리스트 수신 요청
	public const string SPEAK_REQ_Mailbox_Receive = "SPEAK_REQ_Mailbox_Receive";            // 230510 - 우편함 받기요청
	public const string SPEAK_REQ_Mailbox_ReceiveAll = "SPEAK_REQ_Mailbox_ReceiveAll";		// 230510 - 우편함 참치캔 모두 받기 요청
	public const string SPEAK_REQ_RewardTuna = "SPEAK_REQ_RewardTuna";						// 230510 - 참치캔 획득내역 요청

	public const string WorldStartActivity = "WorldStartActivity";
	public const string WorldFinishActivity = "WorldFinishActivity";
	public const string WorldStartTunaActivity = "WorldStartTunaActivity";
	public const string WorldFinishTunaActivity = "WorldFinishTunaActivity";
	public const string ThemeStartActivity = "ThemeStartActivity";
	public const string ThemeFinishActivity = "ThemeFinishActivity";
	public const string SpeakingStartActivity = "SpeakingStartActivity";
	public const string SpeakingFinishActivity = "SpeakingFinishActivity";
	public const string PhonicsStartActivity = "PhonicsStartActivity";
	public const string PhonicsFinishActivity = "PhonicsFinishActivity";
	public const string LetsTalkStartEpisode = "LetsTalkStartEpisode";
	public const string LetsTalkFinishEpisode = "LetsTalkFinishEpisode";
	public const string CompleteTutorial = "CompleteTutorial";
	public const string ScanItSearch = "ScanItSearch";
	public const string ScanItStart = "ScanItStart";
	public const string SchoolTalk_RoomInfo = "SchoolTalk_RoomInfo";
	public const string SchoolTalk_Conversation = "SchoolTalk_Conversation";
	public const string SchoolTalk_Message = "SchoolTalk_Message";

	public const string RTCS_ReqStart = "RTCS_ReqStart";
	public const string RTCS_SendTalk = "RTCS_SendTalk";
	public const string RTCS_SendSchoolTalk = "RTCS_SendSchoolTalk";
	public const string STT_AccessToken = "STT_AccessToken";
	public const string SPEECH_AuthAccessToken = "SPEECH_AuthAccessToken";

    public const string MatchItem = "MatchItem";

    public const string GameRecord = "GameRecord";
	public const string ContentScore = "ContentScore";
	public const string RecordActivity = "RecordActivity";
	public const string RecordWorld = "RecordWorld";
	public const string RecordLetsTalk = "RecordLetsTalk";
	public const string StudyRecord = "StudyRecord";

	public const string Costume_InvenList = "Costume_InvenList";
	public const string Costume_Products = "Costume_Products";
	public const string Costume_PurchaseProduct = "Costume_PurchaseProduct";
    public const string Costume_PutOn = "Costume_PutOn";
    public const string Costume_PutOff = "Costume_PutOff";

}