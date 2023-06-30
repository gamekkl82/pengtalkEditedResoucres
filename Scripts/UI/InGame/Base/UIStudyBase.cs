
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

public class UIStudyBase : UIBase
{
	[Header("System (voice recognition, rtcs network)")]
	public bool useSTT;
	public bool useRTCS;

	protected const float PushTime1st = 6.0f;
	protected const float PushTime2nd = 12.0f;

	// handler
	protected SttHandler _sttHandler => useSTT ? StudyScene.sttHandler : null;
	protected RTCSHandler _rtcsHandler => useRTCS ? StudyScene.rtcsHandler : null;

	protected bool _muting = false;
	protected float _recordTime;

	protected SimpleTimer _timer = new SimpleTimer();

	// recorder
	private xUIRecorder _mainRecorder { get; set; } = null;
	private System.Action _onBeforeStartSpeech = null;
	private bool _evalution = false;
	private string _withScript = "";

	// scene
	protected bool _sceneLoaded = false;

	protected StudySceneHandler _studyScene;
	protected StudySceneHandler StudyScene
	{
		get
		{
			if (_studyScene == null)
				_studyScene = SceneController.Instance.GetSceneHandler<StudySceneHandler>("Study");
			return _studyScene;
		}
	}

	//----- base --------------------------------------------------

	protected override void OnInitialize()
	{
		Logger.Log("UIStudyBase | OnIntialize");
	}

	protected virtual void OnDestroy()
	{
		_timer.Cancel();
		CancelWaitToken();
	}

	public override void Show()
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		// init data & set callback
		ShowAfter();

		if (_sceneLoaded)
			StartPlay();
	}

	public async Task Show(bool withReady, bool sceneLoaded = true)
	{
		if (withReady)
		{
			if (await ReadyToStart() == false) 
				return;
		}

		_sceneLoaded = sceneLoaded;
		Show();
	}

	//
	protected override void ShowAfter()
	{
		if (StudyScene == null)
		{
			Logger.LogError("This scene is not StudyScene.");
			return;
		}

		//----- init ui callback
		StudyScene.onEscapeFromScene = OnEscapeFromScene;
	}

	//
	public void OnSceneLoadedComplete()
	{
		Logger.Log($"UIStudyBase | <b>OnSceneLoadedComplete = {_sceneLoaded}</b>");
		if (!_sceneLoaded)
		{
			_sceneLoaded = true;
			StartPlay();
		}
	}

	public virtual async Task<bool> ReadyToStart()
	{
		return true;
	}

	//----- UI --------------------------------------------------
	//
	protected virtual void StartPlay()
	{

	}

	public virtual void OnClick_Close()
	{
		CommonPopupController.ShowOkCancelMessage(
			Momo.Localization.Get("popup_stop_activity_message"),
			Momo.Localization.Get("popup_stop_activity_title"),
			Momo.Localization.Get("button_continue"), Momo.Localization.Get("button_stop"),
			false,
			() => { Logger.Log("계속하기"); }, () => StudyScene.StopStudy());
	}

	public void OnEscapeFromScene()
	{
		if (OnEscapeEvent())
			return;

		OnClick_Close();
	}

	public virtual bool OnEscapeEvent()
	{
		return false;
	}

	// record button

	protected void InitRecorder(xUIRecorder recorder, System.Action onBeforeStartSpeech = null)
	{
		_mainRecorder = recorder;
		_onBeforeStartSpeech = onBeforeStartSpeech;
	}

	protected void SetSpeechInfo(bool eval, string withScript = "")
	{
		_evalution = eval;
		_withScript = withScript;
	}

	protected bool CheckMicrophone()
	{
		if (Microphone.devices.Length > 0)
			return true;

		PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
				{
					popup.SetSystemPopupController(
						Momo.Localization.Get("popup_microphone_setting_title"),
#if UNITY_ANDROID || UNITY_IOS
						Momo.Localization.Get("popup_mobile_microphone_setting_message"));
#else
						Momo.Localization.Get("popup_microphone_setting_message"));
#endif
				});

		return false;
	}

	public void OnClick_Recorder()
	{
		if (_mainRecorder == null)
			return;

		if (_sttHandler == null)
			return;

		Logger.Log($"OnClick_Recorder | {_mainRecorder.CurrentStatusName}");
		
		switch (_mainRecorder.CurrentStatusIndex)
		{
		case xUIRecorder.RecorderStatus.Normal:
			{
				//마이크 켜져있는지 확인
				if (CheckMicrophone() == false)
					return;

				//녹음 하기 전에 다른 환경 셋팅 (소리를 끈다던지)
				_onBeforeStartSpeech?.Invoke();

				//시작음 들려주고
				float delay = _mainRecorder.PlayStartSound();
				//버튼 이미지를 연결중 이미지로 변경
				_mainRecorder.Connecting();

				_muting = false;
				_timer.SetTimer(delay, delegate ()
				{
					// start recording...
					//따라하기라면 _evalution = true, _withScript = 따라할 단어 또는 문장
					_sttHandler.StartSpeech(_evalution, _withScript, onStartRecording: delegate
					{
						//녹음중 이미지로 변경
						_mainRecorder.Recording();
						//녹음 시간을 측정하기 위한 현재 시간 저장
						_recordTime = Time.realtimeSinceStartup;
					});
				});
			}
			break;
		case xUIRecorder.RecorderStatus.Recording:
		case xUIRecorder.RecorderStatus.Push:
			{
				// stop
				_mainRecorder.Scanning();
				_sttHandler.StopSpeech();
			}
			break;
		}
	}

	//----- stt handler --------------------------------------------------

	// receive complete
	public void OnReceiveResult(SttHandler.ReceivedResult result, AudioClip clip)
	{
		//----- 긴 묵음으로 인한 종료이면..
		if (_muting && !result.isSuccess)
		{
			_muting = false;
			_mainRecorder?.Ready();
			OnReceiveComplete(result, clip, true);
		}
		else
		{
			if (result.isSuccess)
				Logger.Log("UIStudyBase | OnReceiveComplete sentenceLevel.text : " + result.evalResult.sentenceLevel.text);
			else
			{
				Logger.Log($"UIStudyBase | OnReceiveComplete sttMessage = {result.sttMessage}\nsttError = {result.sttError.reason}");

				if (StudyData.Current.educationCode != GameData.EducationCode.LetsTalk)
				{
					PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
					{
						popup.SetSystemPopupController(
							Momo.Localization.Get("popup_stt_title"),
							Momo.Localization.Get("popup_stt_message"),
							() =>
								{
									_mainRecorder?.Ready();
									OnReceiveComplete(result, clip, true);
								});
					});
					return;
				}
            }

			// HGKIM 230510 - 예외처리 NO_TOKEN
			if(result.evalResult.sentenceLevel.text.Equals("NO_TOKEN"))// && result.evalResult.sentenceLevel.proficiencyScore.Count == 0)
			{
				//Logger.Log($"UIStudyBase | OnReceiveComplete sttMessage = {result.sttMessage}\nsttError = {result.sttError.reason}");
				Logger.Log("NO_TOKEN!!! NULL ERROR");

				//if (StudyData.Current.educationCode != GameData.EducationCode.LetsTalk)
				{
					PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
					{
						popup.SetSystemPopupController(
							Momo.Localization.Get("popup_stt_title"),
							Momo.Localization.Get("popup_stt_message"),
							() =>
							{
								_mainRecorder?.Ready();
								OnReceiveComplete(result, clip, true);
							});
					});
					return;
				}
			}

			_mainRecorder?.NotReady();
			OnReceiveComplete(result, clip, false);
		}
	}

	public virtual async void OnReceiveComplete(SttHandler.ReceivedResult result, AudioClip clip, bool isMute)
	{
		
	}

	public virtual void OnReceiveMute(bool isMute)
	{
		if (isMute)
		{
			float muteTime = Time.realtimeSinceStartup - _recordTime;

			if (!_muting)
			{
				if (muteTime > PushTime1st)
				{
					_mainRecorder?.Push();
					_muting = true;
					_recordTime = Time.realtimeSinceStartup;
				}
			}
			else
			{
				if (muteTime > PushTime2nd)
				{
					_sttHandler.StopSpeech();
				}
			}
		}
		else
		{
			if (_muting)
			{
				_mainRecorder?.Recording();
				_muting = false;
			}
			_recordTime = Time.realtimeSinceStartup;
		}
	}

	public virtual void OnConnectRTCS()
	{

	}

	public virtual void OnDisconnectRTCS()
	{

	}

	public virtual void OnMessageRTCS(string message)
	{

	}


	//----------------------
	#region -- WaitStatus ----------------------------------------------------------------------
	public enum WaitStatus
	{
		None,
		FirstWait,
		WaitTalk,
		Done,
		DoneFailed,
	}
	protected WaitStatus status = WaitStatus.None;
	public WaitStatus Status
	{
		get => status;
		protected set
		{
			status = value;
			Logger.Log($"<color=white>WaitStatus</color> = {status}");
		}
	}
	public IEnumerator WaitMessage()
	{
		Logger.Log("UIStudyBase | RTCSHandler talk WaitConnect start");
		while (Status < WaitStatus.Done)
			yield return null;

		Logger.Log("UIStudyBase | RTCSHandler talk WaitMessage finish : " + Status.ToString());
	}
	#endregion

	#region -- waitToken ----------------------------------------------------------------------
	protected CancellationTokenSource _waitToken;
	protected CancellationToken CreateWaitToken(int milliSec)
	{
		if (_waitToken != null && !_waitToken.IsCancellationRequested)
		{
			_waitToken.Cancel();
			_waitToken.Dispose();
		}

		_waitToken = new System.Threading.CancellationTokenSource();
		_waitToken.CancelAfter(milliSec);
		return _waitToken.Token;
	}

	protected void CancelWaitToken()
	{
		if (_waitToken == null)
			return;

		if (_waitToken.IsCancellationRequested)
		{
			Logger.Log("UIStudyBase | already _waitToken IsCancellationRequested !!");
			return;
		}

		Logger.Log("UIStudyBase | _waitToken CancelWaitToken");

		_waitToken.Cancel();
		_waitToken.Dispose();
	}
	#endregion
}
