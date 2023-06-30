using System.Threading.Tasks;
using UnityEngine;

public partial class NetworkServiceHandler
{
	// << 2차 >>
	// RTCS_ReqStart
	[ServiceHandler(NetworkServiceRequest.RTCS_ReqStart)]
	public async Task<MPServiceResult> RTCS_ReqStart(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_RtcsStart>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.RTCS_ReqStartFailed);

		return MPServiceResult.Success(response.Result);
	}

	// RTCS_SendTalk
	[ServiceHandler(NetworkServiceRequest.RTCS_SendTalk)]
	public async Task<MPServiceResult> RTCS_SendTalk(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_RtcsTalk>(msg);

		if (!response.IsSuccess)
		{
			Debug.LogWarning("RTCS_SendTalk | this response is empty, but response set Failed");
			return MPServiceResult.Fail(MPServiceResultCode.RTCS_ReqSendFailed);
		}

		return MPServiceResult.Success();
	}

	// RTCS_SendSchoolTalk
	[ServiceHandler(NetworkServiceRequest.RTCS_SendSchoolTalk)]
	public async Task<MPServiceResult> RTCS_SendSchoolTalk(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_RtcsSchoolTalk>(msg);

		if (!response.IsSuccess)
		{
			Debug.LogWarning("RTCS_SendSchoolTalk | this response is empty, but response set Failed");
			return MPServiceResult.Fail(MPServiceResultCode.RTCS_ReqSendSchooltalkFailed);
		}

		return MPServiceResult.Success();
	}

	// STT_AccessToken
	[ServiceHandler(NetworkServiceRequest.STT_AccessToken)]
	public async Task<MPServiceResult> STT_AccessToken(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_STTAccessToken>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.STT_AccessTokenFailed);

		return MPServiceResult.Success(response.Result);
	}

	// SPEECH_AuthAccessToken
	[ServiceHandler(NetworkServiceRequest.SPEECH_AuthAccessToken)]
	public async Task<MPServiceResult> SPEECH_AuthAccessToken(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_SpeechAuthAccessToken>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.SPEECH_AuthAccessTokenFailed);

		return MPServiceResult.Success(response.Result);
	}

	// SaveRecord
	[ServiceHandler(NetworkServiceRequest.GameRecord)]
	public async Task<MPServiceResult> GameRecord(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_SaveRecord>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.GameRecordFailed);

		return MPServiceResult.Success(response.Result);
	}

	// ContentScore
	[ServiceHandler(NetworkServiceRequest.ContentScore)]
	public async Task<MPServiceResult> ContentScore(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_ContentsScore>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.ContentScoreFailed);

		return MPServiceResult.Success(response.Result);
	}

	// RecordActivity
	[ServiceHandler(NetworkServiceRequest.RecordActivity)]
	public async Task<MPServiceResult> RecordActivity(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_StudyRecord_Activity>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.RecordActivityFailed);

		return MPServiceResult.Success(response.Result);
	}

	// RecordWorld
	[ServiceHandler(NetworkServiceRequest.RecordWorld)]
	public async Task<MPServiceResult> RecordWorld(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_StudyRecord_World>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.RecordWorldFailed);

		return MPServiceResult.Success(response.Result);
	}

	// RecordLetsTalk
	[ServiceHandler(NetworkServiceRequest.RecordLetsTalk)]
	public async Task<MPServiceResult> RecordLetsTalk(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_StudyRecord_LetsTalk>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.RecordLetsTalkFailed);

		return MPServiceResult.Success(response.Result);
	}

	// StudyRecord
	[ServiceHandler(NetworkServiceRequest.StudyRecord)]
	public async Task<MPServiceResult> StudyRecord(XDR.IMessage msg)
	{
		var response = await request<SPEAK_RES_StudyRecord>(msg);

		if (!response.IsSuccess)
			return MPServiceResult.Fail(MPServiceResultCode.StudyRecordFailed);

		return MPServiceResult.Success(response.Result);
	}
}
