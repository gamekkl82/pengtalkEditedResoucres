using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class UITotalResult : UIStudyBase
{
	#region -- declare ---------------------------------------------------------------
	[SerializeField] private GameObject _rootNormal;
	[SerializeField] private GameObject _rootTheme;
	[SerializeField] private GameObject _rootTunaCash;

	[Header("Text & Text Image")]
	[SerializeField] private Text _uiTuna;
	[SerializeField] private Text _uiStage;
	[SerializeField] private xUIStatusIndicator _uiGrade;
	[SerializeField] private Text _uiTheme;
	[SerializeField] private Text _uiTopic;
	[SerializeField] private xUIStatusIndicator _uiThemeGrade;
	[SerializeField] private xUIStatusGroup _uiFx;
	[SerializeField] private PengsooController _pengsoo;

	[Header("Activity")]
	[SerializeField] private GameObject _boardActivity;
	[SerializeField] private Text _uiScore;

	[Header("Episode")]
	[SerializeField] private GameObject _boardEpisode;
	[SerializeField] private List<xUISwitch> _switchMissions;
	[SerializeField] private List<Text> _textMissions;

	[Header("Star")]   // star
	[SerializeField] private List<GameObject> _stars;
	[Tooltip("별 연출 시작 Delay (milliseconds)")]
	[SerializeField] private int _startDelay = 1000;
	[Tooltip("다음 별 연출 Delay (milliseconds)")]
	[SerializeField] private int _nextDelay = 500;

	[Header("Tuna")]   // star
	[Tooltip("Tuna Delay (milliseconds)")]
	[SerializeField] private int _startTunaDelay = 2000;
	[SerializeField] private xUIGoalFX _tunaFX;

	[Header("Detail Popup")]
	[SerializeField] private Image _uiDetail;
	[SerializeField] private RectTransform _uiDetailPopup;
	[SerializeField] private RectTransform _uiDetailTop;
	[SerializeField] private float _playTime = 0.5f;

	[SerializeField] private Text _uiPopupScore;

	[Header("    Activity")]
	[SerializeField] private GameObject _detailActivity;
	[SerializeField] private UIResultScrollItem _itemResultPrefab;
	[Space(5)]
	[SerializeField] private GameObject _rootNormalDetail;
	[SerializeField] private GameObject _scrollRootResult;
	[Space(5)]
	[SerializeField] private GameObject _rootDialogueDetail;
	[SerializeField] private GameObject _scrollRootDialogueResult;
	[SerializeField] private xUISwitch _dialogueFollow;
	[SerializeField] private xUISwitch _dialogueRole;

	[Header("    Episode")]
	[SerializeField] private GameObject _detailEpisode;
	[SerializeField] private GameObject _scrollRootDialogue;
	[SerializeField] private UILetstalkScrollItem _itemDialoguePrefab;

	[Header("Bottom Buttons")]

	[SerializeField] private GameObject _buttonStop;
	[SerializeField] private GameObject _buttonContinue;
	[SerializeField] private GameObject _buttonComplete;

	[SerializeField] private GameObject _buttonDetailStop;
	[SerializeField] private GameObject _buttonDetailContinue;
	[SerializeField] private GameObject _buttonDetailComplete;

	[Header("Evaluation Popup")]
	[SerializeField] private GameObject _rootEvaluation;
	
	private UIEvaluationPopup _popupEvaluation = null;
	#endregion

	protected System.Threading.CancellationTokenSource _destroyToken;

	// detail popup settings
	private bool showDetail = false;
	private float startHeight = 0;
	private Vector2 startAnchoredPosition = Vector2.zero;

	private List<UIResultScrollItem> _slots = new List<UIResultScrollItem>();
	private List<UILetstalkScrollItem> _dialogueSlots = new List<UILetstalkScrollItem>();

	private xUIStatusIndicator _currentGradeUI;
	private string _gradeText; 

	private bool isStart = false;

	// study data
	private StudyData _studyData;
	private GameData.Study.Activity _activity;
	private GameData.Study.Episode _episode;
	private UIStudyBase _studyUI;

	private bool _isActivity = false;

	public void SetStudyData(UIStudyBase studyUI)
	{
		_studyData = StudyData.Current;
		_activity = StudyData.Current.GetTarget<GameData.Study.Activity>();
		_episode = StudyData.Current.GetTarget<GameData.Study.Episode>();

		_studyUI = studyUI;
	}

	#region -- start -----------------------------------------------------------------
	//
	public override async Task<bool> ReadyToStart()
	{
		isStart = false;
		Logger.Log("UIResult | ReadyToStart "+ GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count);

		_destroyToken = new System.Threading.CancellationTokenSource();
		_destroyToken.RegisterRaiseCancelOnDestroy(this);

		// download resource
		await DownloadResource();
		Logger.Log("UIResult | ReadyToStart DownloadResource");
		if (this == null)
			return false;

		// init ui
		bool isWorld = _studyData.IsStageMode;
		bool isTheme = _studyData.IsThemeMode;
		bool isSpeaking = _studyData.IsSpeaking;
		bool isPhonics = _studyData.IsPhonics;

		bool continuable = isWorld;

		_currentGradeUI = _uiGrade;

		// stage
		int tunaCount = GameData.inventory[GameData.ItemId.Coin].Count - _studyData.rewardTotalTuna;
		RefreshTunaText(tunaCount);

		if (_activity != null)
		{
			Logger.Log("UIResult | ReadyToStart _activity != null");

			_boardActivity.SetActive(true);
			_boardEpisode.SetActive(false);
			_detailActivity.SetActive(true);
			_detailEpisode.SetActive(false);

			_buttonStop.SetActive(continuable);
			_buttonContinue.SetActive(continuable);
			_buttonComplete.SetActive(!continuable);

			_buttonDetailStop.SetActive(continuable);
			_buttonDetailContinue.SetActive(continuable);
			_buttonDetailComplete.SetActive(!continuable);

			Logger.Log("UIResult | ReadyToStart _activity 2");
			if (_popupEvaluation == null)
				_popupEvaluation = UIEvaluationPopup.CreateUI(_rootEvaluation.transform);
			_popupEvaluation.gameObject.SetActive(false);

			if (isWorld)
				_uiStage.text = $"STAGE {_activity.stage}";
			else if (isSpeaking)
			{
				var scene = GameData.Study.FindScene(_activity.topicNo);
				_uiStage.text = scene.label;
			}
			else if (isPhonics)
			{
				var scene = GameData.Study.FindScene(_activity.topicNo);
				_uiStage.text = scene.label;
			}
			else
			{
				var theme = GameData.Study.FindTheme(_activity.worldNo);
				var topic = GameData.Study.FindTopic(_activity.topicNo);
				_uiTheme.text = $"{theme.label}. {theme.name}";
				_uiTopic.text = $"{topic.name}";

				_currentGradeUI = _uiThemeGrade;
			}

			_uiScore.text = $"{_activity.Score}";

			_gradeText = GameData.Grade.GetActivityGrade(_activity.Score);
			Logger.Log($"    grade = {_gradeText}, score = {_activity.Score}");

			// detail popup
			_uiPopupScore.text = _uiScore.text;

			if (GameData.ContentType.IsDialogue(_studyData.contents[0].contentType))
			{
				GameData.Study.DialogueContent follow = _studyData.contents[UIDialogue.Follow] as GameData.Study.DialogueContent;
				GameData.Study.DialogueContent roleA = _studyData.contents[UIDialogue.RoleA] as GameData.Study.DialogueContent;
				GameData.Study.DialogueContent roleB = _studyData.contents[UIDialogue.RoleB] as GameData.Study.DialogueContent;

				CreateDialogueSlot(follow.talks.Count * 2);

				int talkIdx = 0;
				for (int idx = 0; idx < _slots.Count; ++idx)
				{
					UIResultScrollItem slot = _slots[idx];
					if (idx < follow.talks.Count)
					{
						slot.SetDialogueData(follow, idx, _popupEvaluation);
						slot.gameObject.SetActive(true);
					}
					else if (idx < follow.talks.Count * 2)
					{
						talkIdx = idx - follow.talks.Count;
						if (talkIdx % 2 > 0)
							slot.SetDialogueData(roleA, talkIdx, _popupEvaluation);
						else
							slot.SetDialogueData(roleB, talkIdx, _popupEvaluation);

						slot.gameObject.SetActive(false);
					}
					else
						slot.gameObject.SetActive(false);
				}

				_rootNormalDetail.SetActive(false);
				_rootDialogueDetail.SetActive(true);
			}
			else
			{
				CreateSlot(_studyData.contents.Count);

				for (int idx = 0; idx < _slots.Count; ++idx)
				{
					UIResultScrollItem slot = _slots[idx];
					if (idx < _studyData.contents.Count)
					{
						slot.SetContentData(_studyData.contents[idx], _popupEvaluation);
						slot.gameObject.SetActive(true);
					}
					else
						slot.gameObject.SetActive(false);
				}

				_rootNormalDetail.SetActive(true);
				_rootDialogueDetail.SetActive(false);
			}
		}
		else if (_episode != null)
		{
			Logger.Log("UIResult | ReadyToStart _episode");
			_boardActivity.SetActive(false);
			_boardEpisode.SetActive(true);
			_detailActivity.SetActive(false);
			_detailEpisode.SetActive(true);
			_buttonStop.SetActive(false);
			_buttonContinue.SetActive(false);
			_buttonComplete.SetActive(true);

			_uiStage.text = _episode.label;

			_gradeText = GameData.Grade.GetEpisodeGrade(_episode.Star);
			Logger.Log($"    grade = {_gradeText}, star = {_episode.Star}");

			int idx = 0;
			GameData.Study.MissionContent missionContent = StudyData.Current.contents[0] as GameData.Study.MissionContent;
			foreach (GameData.Study.Mission mission in missionContent.missions.Values)
			{
				_textMissions[idx].text = mission.title;
				_switchMissions[idx].SwitchOn(missionContent.recordMissions.Find(m => m == mission.itemName) != null);
				++idx;
			}

			CreateEpisodeSlot(missionContent.recordTalks.Count);

			for (int slotIdx = 0; slotIdx < _dialogueSlots.Count; ++slotIdx)
			{
				UILetstalkScrollItem slot = _dialogueSlots[slotIdx];
				if (slotIdx < missionContent.recordTalks.Count)
				{
					GameData.Study.TalkRecord talkRecord = missionContent.recordTalks[slotIdx];
					if (talkRecord.talkerCode == GameData.Talker.Chatbot)
						slot.SetPengsoo(talkRecord.stt);
					else
						slot.SetMine(talkRecord.stt, talkRecord.bestAnswer);

					slot.gameObject.SetActive(true);
				}
				else
					slot.gameObject.SetActive(false);
			}
		}
		else 
			return false;

		Logger.Log("UIResult | ReadyToStart grade");
		// grade
		_currentGradeUI.SetStatus(_gradeText);
		_uiFx.SetStatus(_gradeText);

		_rootNormal.SetActive(!isTheme);
		_rootTheme.SetActive(isTheme);
		_rootTunaCash.SetActive(_episode == null);

		Logger.Log("UIResult | ReadyToStart end~~~~~~");
		return true;
	}
	//
	protected override void StartPlay()
	{
		Logger.Log("UIResult | StartPlay~~~~~~~");

		// popup
		if (startHeight == 0)
		{
			float height = _uiDetailPopup.rect.height;
			startHeight = _uiDetailTop.rect.height * 1.8f;
			_uiDetailPopup.SetTop(height - startHeight);
			_uiDetailPopup.SetBottom(startHeight - height);
			startAnchoredPosition = _uiDetailPopup.anchoredPosition;

			//Logger.LogError($"    detail popup  startHeight = {startHeight}, startAnchoredPosition = {startAnchoredPosition}, height = {_uiDetailPopup.rect.height}");
		}

		switch (_gradeText)
		{
		case GameData.Grade.Excellent:
		case GameData.Grade.Good:
			AudioController.Play("SFX_UI_Continue");
			break;
		case GameData.Grade.TryHarder:
		case GameData.Grade.Oops:
			AudioController.Play("SFX_Item_Cancel");
			break;
		}

		ShowStars(_activity != null ? _activity.Star : _episode.Star);

		ShowTuna();

		_pengsoo.PlayByGrade(_gradeText);

		isStart = true;
		OnClick_ShowDetail(false);
	}
	#endregion

	#region -- ui --------------------------------------------------------------------
	
	private void CreateSlot(int count)
	{
		int makeCount = count - _slots.Count;
		if (makeCount > 0)
		{
			for (int idx = 0; idx < makeCount; ++idx)
			{
				UIResultScrollItem slot = Instantiate(_itemResultPrefab);
				slot.transform.SetParent(_scrollRootResult.transform);
				slot.transform.localPosition = Vector3.zero;
				slot.transform.localScale = Vector3.one;
				slot.gameObject.name = string.Format("Result{0}", _slots.Count.ToString("0#"));

				_slots.Add(slot);
			}
		}
	}

	private void CreateDialogueSlot(int count)
	{
		int makeCount = count - _slots.Count;
		if (makeCount > 0)
		{
			for (int idx = 0; idx < makeCount; ++idx)
			{
				UIResultScrollItem slot = Instantiate(_itemResultPrefab);
				slot.transform.SetParent(_scrollRootDialogueResult.transform);
				slot.transform.localPosition = Vector3.zero;
				slot.transform.localScale = Vector3.one;
				slot.gameObject.name = string.Format("Result{0}", _slots.Count.ToString("0#"));

				_slots.Add(slot);
			}
		}
	}

	private void CreateEpisodeSlot(int count)
	{
		int makeCount = count - _dialogueSlots.Count;
		if (makeCount > 0)
		{
			for (int idx = 0; idx < makeCount; ++idx)
			{
				UILetstalkScrollItem slot = Instantiate(_itemDialoguePrefab);
				slot.transform.SetParent(_scrollRootDialogue.transform);
				slot.transform.localPosition = Vector3.zero;
				slot.transform.localScale = Vector3.one;
				slot.gameObject.name = string.Format("Dialogue{0}", _dialogueSlots.Count.ToString("0#"));
				slot.boxWidth = 493;
				_dialogueSlots.Add(slot);
			}
		}
	}

	private async void ShowStars(int starCount)
	{
		if (_studyData.IsThemeMode)
			return;

		Logger.Log($"UITotalResult | ShowStars - {starCount}");

		if (starCount > _stars.Count)
			starCount = _stars.Count;

		for (int idx = 0; idx < _stars.Count; ++idx)
			_stars[idx].SetActive(false);

		try
		{
			await Task.Delay(_startDelay, _destroyToken.Token);
			for (int idx = 0; idx < starCount; ++idx)
			{
				_stars[idx].SetActive(true);
				AudioController.Play("SFX_UI_Star_2");
				await Task.Delay(_nextDelay, _destroyToken.Token);
			}
		}
		catch
		{
			Logger.Log("UITotalResult | Stop ShowStars");
		}
	}

	private async void ShowTuna()
	{
		if (_studyData.rewardTunas.IsNullOrEmpty() || _studyData.rewardTotalTuna == 0)
			return;

		try
		{
			if (_studyData.rewardTunas.Count == 1)
			{
				Logger.Log($"Reward !!! play  start >>>>>>>>>>");
				await Task.Delay(_startTunaDelay, _destroyToken.Token);
				Logger.Log($"Reward !!! play tuna = +{_studyData.rewardTotalTuna}");

				if (_tunaFX != null)
				{
					int startCan = (_studyData.rewardTotalTuna / 10) + 1;
					_tunaFX.Play(startCan, startCan/2);

					float fxTime = _tunaFX.GetStartDuration();
					Logger.Log($"        play fxTime = +{fxTime}");
					if (fxTime > 0)
						await Task.Delay((int)(fxTime*1000.0f), _destroyToken.Token);
				}

				int tunaCount = GameData.inventory[GameData.ItemId.Coin].Count;
				RefreshTunaText(tunaCount);
				Logger.Log($"    >> result tuna = {_uiTuna.text}");
			}
		}
		catch (System.Exception ex)
		{
			Logger.Log($"UITotalResult | Stop ShowTuna {ex}");
		}
	}

	private void RefreshTunaText(int tunaCount)
	{
		if (tunaCount > 999999)
			_uiTuna.text = "+999,999";
		else
			_uiTuna.text = tunaCount.ToString("#,0");
	}
	#endregion


	#region -- on click --------------------------------------------------------------

	public override bool OnEscapeEvent()
	{
		if (_popupEvaluation != null && _popupEvaluation.gameObject.activeSelf)
		{
			Logger.Log("UIResult | Close popupEvaluation");
			_popupEvaluation.gameObject.SetActive(false);
			return true;
		}
		return false;
	}

	public override void OnClick_Close()
	{
		Logger.Log("UIResult | OnClick_Close");
		StudyScene.StopStudy();
	}

	public async void OnClick_Retry()
	{
		Logger.Log("UIResult | OnClick_Retry");
		if (!isStart)
			return;

		StudyScene.uiBlock.SetActive(true);

		_studyData.Continue = false;
		_studyData.ResetRecord();

		//_studyData.RetryStudy();

		await _studyUI.Show(true);

		Hide();
		StudyScene.uiBlock.SetActive(false);
	}

	public void OnClick_Continue()
	{
		Logger.Log("UIResult | OnClick_Continue");
		if (!isStart)
			return;

		// todo:  _studyScene.uiBlock.SetActive(true);
		if (_studyData.contents[0].contentType == GameData.ContentType.Express)
		{
			GameData.Study.Topic topic = GameData.Study.FindTopic(_activity.topicNo);
			if (topic.Finish)
				PopupManager.Instance.OpenPopup<GameStartPopupController>("BonusGameStartPopupController", popup =>{ popup.SetGameStartPopupController(topic); });
			else
				StudyScene.StopStudy();
		}
		else if (_activity.Next == null)
			StudyScene.StopStudy();
		else
		{
			Logger.Log($"UIResult | OnClick_Continue => OpenPopup<GameStartPopupController>");
			if (_activity.Next != null)
				Logger.Log($"UIResult | OnClick_Continue => {_activity.Next.no}");

			PopupManager.Instance.OpenPopup<GameStartPopupController>(popup => { popup.SetGameStartPopupController(_activity.Next); });
		}
	}

	public void OnClick_ShowDetail(bool show)
	{
		Logger.Log("UIResult | OnClick_ShowDetail");
		if (!isStart)
			return;

		showDetail = show;
		_uiDetailPopup.DOAnchorPos(showDetail ? Vector2.zero : startAnchoredPosition, _playTime);
		_uiDetail.enabled = showDetail;

		//Logger.Log("UIResult | ShowDetail - " + showDetail);
	}

	public void OnClick_Cash()
	{
		Logger.Log("UIResult | OnClick_Cash");

		if (!isStart)
			return;
		// 230413 - 참치캔 히스토리 작업 - 여기서는 눌려도 아무것도 보이지 않게 한다.
		//PopupManager.Instance.OpenPopup<CashPopupController>(onBindAction: popup => popup.Bind());
	}

	//
	public void OnClick_DialogueFollow()
	{
		Logger.Log("UIResult | OnClick_DialogueFollow");

		if (!isStart)
			return;

		_dialogueFollow.SwitchOn(true);
		_dialogueRole.SwitchOn(false);

		if (_studyData.contents[UIDialogue.Follow] is GameData.Study.DialogueContent follow)
		{
			for (int idx = 0; idx < _slots.Count; ++idx)
			{
				UIResultScrollItem slot = _slots[idx];
				if (idx < follow.talks.Count)
					slot.gameObject.SetActive(true);
				else if (idx < follow.talks.Count * 2)
					slot.gameObject.SetActive(false);
				else
					slot.gameObject.SetActive(false);
			}
		}
	}

	public void OnClick_DialogueRole()
	{
		Logger.Log("UIResult | OnClick_DialogueRole");

		if (!isStart)
			return;

		_dialogueFollow.SwitchOn(false);
		_dialogueRole.SwitchOn(true);

		if (_studyData.contents[UIDialogue.Follow] is GameData.Study.DialogueContent follow)
		{
			for (int idx = 0; idx < _slots.Count; ++idx)
			{
				UIResultScrollItem slot = _slots[idx];
				if (idx < follow.talks.Count)
					slot.gameObject.SetActive(false);
				else if (idx < follow.talks.Count * 2)
					slot.gameObject.SetActive(true);
				else
					slot.gameObject.SetActive(false);
			}
		}
	}

	#endregion

	#region -- DownloadResource --------------------------------------------------------------

	private async Task DownloadResource()
	{
		if (StudyScene.SpeechToken.IsNullOrEmpty())
			await StudyScene.Request_SpeechAuthAccessToken();

		_studyData = StudyData.Current;
		_activity = StudyData.Current.GetTarget<GameData.Study.Activity>();
		_episode = StudyData.Current.GetTarget<GameData.Study.Episode>();

		//Logger.Log("DownloadResource >>>");
		string audioUrl = GameData.Config.launching.client.AudioUrl();
		string ttsUrl = GameData.Config.launching.client.TTSUrl();
		string speechUrl = GameData.Config.launching.client.SpeechUrl();

		Logger.Log("audioUrl : " + audioUrl);
		Logger.Log("ttsUrl : " + GameData.Config.launching.client.TTSUrl());
		Logger.Log("speechUrl : " + speechUrl);

		List<ResourceManager.DownloadAudioInfo> audioList = new List<ResourceManager.DownloadAudioInfo>();

		if (_activity != null)
		{

			for (int idx = 0; idx < _studyData.contents.Count; ++idx)
			{
				if (_studyData.contents[idx] is GameData.Study.FollowContent followContent)
				{
					if (!followContent.recordSttAudioPath.IsNullOrEmpty())
					{
						Logger.Log($"DownloadResource >>> followContent {idx}) {followContent.recordSttAudioPath}");
						audioList.Add(new ResourceManager.DownloadAudioInfo
						{ url = speechUrl, path = followContent.recordSttAudioPath, auth = StudyScene.SpeechToken });
					}
				}
				else if (_studyData.contents[idx] is GameData.Study.ExpressContent expressContent)
				{
					if (!expressContent.recordSttAudioPath.IsNullOrEmpty())
					{
						Logger.Log($"DownloadResource >>> expressContent {idx}) {expressContent.recordSttAudioPath}");
						audioList.Add(new ResourceManager.DownloadAudioInfo
						{ url = speechUrl, path = expressContent.recordSttAudioPath, auth = StudyScene.SpeechToken });
					}
				}
				else if (_studyData.contents[idx] is GameData.Study.DialogueContent dialogueContent)
				{
					for (int talkIdx = 0; talkIdx < dialogueContent.talks.Count; ++talkIdx)
					{
						var talkData = dialogueContent.talks[talkIdx];
						if (talkData != null && !talkData.recordSttAudioPath.IsNullOrEmpty())
						{
							Logger.Log($"DownloadResource >>> dialogueContent {idx} {talkIdx}) {talkData.recordSttAudioPath}");
							audioList.Add(new ResourceManager.DownloadAudioInfo
							{ url = speechUrl, path = talkData.recordSttAudioPath, auth = StudyScene.SpeechToken });
						}
					}
				}
			}
		}
		Logger.Log("DownloadResource >>> Start download");
		if (audioList.Count > 0)
			await ResourceManager.Instance.DownloadAudios(audioList);

		Logger.Log("DownloadResource >>> Done");
	}
	#endregion











	#region -- test code --------------------------------------------------
	public void OnClickTest()
	{
		Logger.Log("OnClickTest");

		RectTransform rectTransform = _uiDetailPopup.GetComponent<RectTransform>();
		rectTransform.DOAnchorPos(Vector2.zero, _playTime);


		/*bigScroll.ClearList();
		for (int idx = 0; idx < 10; ++idx)
			bigScroll.AddItem(new UITestItemData(idx));*/
	}
	public void OnClickTestBool(bool isRunable)
	{
		Logger.Log("OnClickTestBool : isRunable = " + isRunable);


		RectTransform rectTransform = _uiDetailPopup.GetComponent<RectTransform>();
		if (isRunable)
		{
			float height = rectTransform.rect.height;
			rectTransform.SetTop(height - startHeight);
			rectTransform.SetBottom(startHeight - height);
			startAnchoredPosition = rectTransform.anchoredPosition;
		}
		else
		{
			rectTransform.DOAnchorPos(startAnchoredPosition, _playTime);
		}
	}

	public void OnClickTestString(string strMessage)
	{
		Logger.Log($"OnClickTestString : str={strMessage} isRunable = ");
	}
	#endregion

}
