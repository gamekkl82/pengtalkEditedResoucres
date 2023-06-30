using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class DailyWordPopupController : PopupController
{
    [SerializeField] private PengsooUIController lobbyUI;

    //toggle
    [Header("Toggles")]
    [SerializeField] public Toggle toggle1;
    [SerializeField] public Toggle toggle2;
    [SerializeField] private GameObject todayWordOnText;
    [SerializeField] private GameObject todayWordOffText;
    [SerializeField] private GameObject collectWordOnText;
    [SerializeField] private GameObject collectWordOffText;
    [SerializeField] private GameObject todayWordUI;
    [SerializeField] private GameObject collectWordUI;
    [SerializeField] private GameObject popupDlg;

    // First page
    [Header("Page1")]
    [SerializeField] private GameObject word1;
    [SerializeField] private GameObject word2;
    [SerializeField] private GameObject navi1;
    [SerializeField] private GameObject navi2;
    [SerializeField] private GameObject feedBack;
    [SerializeField] private RawImage image;    // 이미지
    [SerializeField] private Text engWord;      // 영단어
    [SerializeField] private Text korWord;      // 번역
    [SerializeField] private Text engSentence;  // 문장
    [SerializeField] private Text korSentence;  // 번역
    [SerializeField] private xUIAudioPlayer _uiNativePlayer;  // play native button
    [SerializeField] private GameObject nextButton;

    // Second page
    [Header("Page2 calender")]
    [SerializeField] Transform dateGroup;
    [SerializeField] DailyWordAttendInfo dateGo;
    [SerializeField] GameObject emptyGo;

    [SerializeField] private Text monthText;

    [SerializeField] private RectTransform calendarTr;
    [SerializeField] private RectTransform dateGroupTr;
    [SerializeField] private RectTransform calendarBgTr;

    // Second page popup
    [Header("Page2 popup")]
    [SerializeField] private GameObject popupWord1;
    [SerializeField] private GameObject popupWord2;
    [SerializeField] private GameObject popupNavi1;
    [SerializeField] private GameObject popupNavi2;

    [SerializeField] private RawImage popupImage;    // 이미지
    [SerializeField] private Text popupEngWord;      // 영단어
    [SerializeField] private Text popupKorWord;      // 번역
    [SerializeField] private Text popupEngSentence;  // 문장
    [SerializeField] private Text popupKorSentence;  // 번역

    [SerializeField] private xUIAudioPlayer _popup_uiNativePlayer;  // play native button

    int calenderYear;   // 220803 - UI에 표기는 안되는데 연도가 필요해서 저장용 추가 
    int calenderMonth;  // 달력에 표기되는 월의 int 저장용

    private List<GameObject> dateList = new List<GameObject>();

    [Header("")]
    [SerializeField] private GameObject greatUI;

    private SPEAK_ST_PushNotice todayWordData = null;   // 오늘의 워드 데이터 여기에 들어감
    private SPEAK_ST_PushNotice clickedWordData = null; // 클릭한 워드 데이터 여기에 들어감

    List<SPEAK_ST_PushNotice_Study> nowPageData;  // 수집한 영단어 달력 표기할 데이터

    public PengsooController m_PengsooController;

    private enum TodayWord
    {
        word = 0,
        sentence,
    }

    private TodayWord stage = TodayWord.word;

    private bool isPopWordPage = true;      // 팝업 페이지 체크용

    private bool isPopupOn = false;

    // 페이지 Opne 처리 - 동기
    public void Bind(SPEAK_RES_PushNotice d)
    {
        todayWordData = d.pushNotice;   // 통신으로 받은 데이터 넣어줌 (GameData에 저장되어있는거 써도 됨)

        // BGM을 끈다
        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        commonHandler.StopBGM();

        //PengsooVisibleManager.Instance.PushPengsooModel(lobbyUI);
        //CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);

        toggle1.isOn = true;
        OnToggle1(true);

        SetPage1Data(todayWordData);    // 오늘의 데이터는 변하지 않으니 여기서 세팅
        SetPage2Data();                 // 캘린더도 변하지 않으니 여기서 일단 세팅

        stage = TodayWord.word;
    }

    /// <summary>
    /// 비동기로 페이지 Open 처리
    /// </summary>
    /// <param name="d">PushNotice Data</param>
    public async void Bind_(SPEAK_RES_PushNotice d)
    {
        todayWordData = d.pushNotice;   // 통신으로 받은 데이터 넣어줌 (GameData에 저장되어있는거 써도 됨)

        lobbyUI.m_PengsooController.Init(true);
        PengsooVisibleManager.Instance.PushPengsooModel(lobbyUI);
        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);

        // BGM을 끈다
        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        commonHandler.StopBGM();

        await DownloadResource(todayWordData);

		toggle1.isOn = true;
        OnToggle1(true);

        calenderYear = DateTime.Now.Year;   // 연도 처음에는 지금걸로 
        calenderMonth = DateTime.Now.Month; // 일단 처음에는 지금달로 저장함

        SetPage1Data(todayWordData);    // 오늘의 데이터는 변하지 않으니 여기서 세팅
        SetPage2Data();                 // 캘린더 데이터를 통신으로 받아오고 바로 세팅한다.

        stage = TodayWord.word;
    }

    // 메인 창을 닫는다.
    public override void OnClosePopup()
    {
        //mileageToggle.isOn = false;
        //starToggle.isOn = false;
        //classToggle.isOn = false;
        //totalToggle.isOn = false;
        //guideMessage.Close();
        
        base.OnClosePopup();
        ClearCalendar();

        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
        PengsooVisibleManager.Instance.PopPengsooModel();

        // BGM 다시 틀어줌
        CommonSceneHandler commonHandler = SceneController.Instance.GetSceneHandler<CommonSceneHandler>("Common");
        commonHandler.PlayBGM(AudioNames.BGM.Lobby);
    }

    /// <summary>
    /// 비동기 리소스 데이터 다운로드
    /// </summary>
    /// <param name="data">pushNotice data 중 audio파일, image파일 정보를 쓴다</param>
    /// <returns>없음</returns>
    private async Task DownloadResource(SPEAK_ST_PushNotice data)
    {
        // 오디오 다운로드 준비
        string audioUrl = GameData.Config.launching.client.AudioUrl();
        Logger.Log("audioUrl : " + audioUrl);

        List<ResourceManager.DownloadAudioInfo> audioList = new List<ResourceManager.DownloadAudioInfo>();

        if (!audioUrl.IsNullOrEmpty())
        {
            Logger.Log("WordAudioFile : " + data.pnWdAudio);
            // 단어 오디오 로드 준비
            if (!data.pnWdAudio.IsNullOrEmpty() && !ResourceManager.AudioClips_Exist(data.pnWdAudio))
            {
                audioList.Add(new ResourceManager.DownloadAudioInfo { url = audioUrl, path = data.pnWdAudio });
            }

            Logger.Log("SentenceAudioFile : " + data.pnPhrAudio);
            // 문장 오디오 로드 준비
            if (!data.pnPhrAudio.IsNullOrEmpty() && !ResourceManager.AudioClips_Exist(data.pnPhrAudio))
            {
                audioList.Add(new ResourceManager.DownloadAudioInfo { url = audioUrl, path = data.pnPhrAudio });
            }
        }

        // 이미지 다운로드 준비
        string imageUrl = GameData.Config.launching.client.ImageUrl();
        Logger.Log("imageUrl : " + imageUrl);

        List<ResourceManager.DownloadImageInfo> imageList = new List<ResourceManager.DownloadImageInfo>();
        string imgOption = $"?{image.rectTransform.sizeDelta.x}x{image.rectTransform.sizeDelta.y}";

        if (!imageUrl.IsNullOrEmpty())
        {
            if (!data.pnWdImage.IsNullOrEmpty() && !ResourceManager.Textures_Exist(data.pnWdImage))
            {
                imageList.Add(new ResourceManager.DownloadImageInfo { url = imageUrl, path = data.pnWdImage, option = imgOption });
            }
        }

        if (audioList.Count > 0)
            await ResourceManager.Instance.DownloadAudios_OGGorMPEG(audioList);
        if (imageList.Count > 0)
            await ResourceManager.Instance.DownloadImages(imageList);
    }


    #region 오늘의영단어페이지
    public void SetPage1Data(SPEAK_ST_PushNotice data)
    {
        // 리소스 이미지
        Texture texture = ResourceManager.Textures_Get(data.pnWdImage);
        if (texture != null)
            image.texture = texture;

        // 영단어 텍스트
        engWord.text = data.pnWdText;
        // 단어 번역
        korWord.text = data.pnWdTrans;

        SetAudio(data, true);

        // 영문장 텍스트
        engSentence.text = data.pnPhrText;
        // 문장 번역
        korSentence.text = data.pnPhrTrans;

        // 클리어 한거면 버튼을 켠다
        ShowNextButton(GameData.isTodayWordClear);
    }

    public void ClearPage1Data()
    {
        image = null;

        engWord.text = "";
        korWord.text = "";

        engSentence.text = "";
        korSentence.text = "";
    }

    // 오디오 클립을 변경한다. 뒤의 bool 값으로 인해 바뀐다.
    private void SetAudio(SPEAK_ST_PushNotice data, bool isWord)
    {
        if (isWord)
        {
            Logger.Log($"HGKIM || SetAudio || 단어 Audio name is {data.pnWdAudio}");
            _uiNativePlayer.clip = ResourceManager.AudioClips_Get(data.pnWdAudio);
        }
        else
        {
            Logger.Log($"HGKIM || SetAudio || 문장 Audio name is {data.pnPhrAudio}");
            _uiNativePlayer.clip = ResourceManager.AudioClips_Get(data.pnPhrAudio);
        }

        //_uiNativePlayer.Play(() => { if(!isPopupOn && !isClear) ShowNextButton(true); }, 0.3f);
    }

    private void ShowNextButton(bool isShow)
    {
        nextButton.SetActive(isShow);
    }
    #endregion

    #region 모은영단어페이지
    // 캘린더 데이터를 요청해서 캘린더를 세팅한다.
    private async Task GetCalenderData()
    {
        string studyYm = $"{calenderYear.ToString("D4")}{calenderMonth.ToString("D2")}";
        Logger.Log($"HGKIM || GetCalenderData || YMData : {studyYm}");
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_PushNotice_Study,
            new SPEAK_REQ_PushNotice_Study
            {
                grdCd = GameData.User.gradeCode,
                studyDtYm = studyYm
            });
        if (result.IsSuccess)
        {
            var data = result.message as SPEAK_RES_PushNotice_Study;

            // 리턴되어 돌아오는 데이터 입력한다.
            nowPageData = data.studyInfos;
        }
    }

    private async void SetPage2Data()  // API가 나오면 넣는다.
    {
        await GetCalenderData();
        SetCalendar();
    }

    private void ClearPage2Data()
    {
        ClearCalendar();
    }

    private void ClearCalendar()
    {
        foreach (var go in dateList)
            GameObject.DestroyImmediate(go);

        dateList.Clear();
	}

    // HGKIM 220818 - 학년코드 추가
    private class CalenderData
	{
        public bool StudyYn { get; set; }
        public string gradeCode { get; set; }
        public bool isBonusDay { get; set; }    // 230419 - 보너스 표기일 추가
    };

	private void SetCalendar()
    {
        ClearCalendar();

        int today = DateTime.Now.Day;
        int month = DateTime.Now.Month;

        //Dictionary<string, bool> attendDate = new Dictionary<string, bool>();
        Dictionary<string, CalenderData> attendData = new Dictionary<string, CalenderData>();   // HGKIM 220818 - 학년코드 추가
        List<string> gradeCodeList = new List<string>();
        foreach (var item in nowPageData)
		{
            if (item.studyYn.Equals("Y"))
            {
                //attendDate.Add(item.pnDate, true);
                attendData.Add(item.pnDate, new CalenderData { StudyYn = true, gradeCode = item.grdCd, isBonusDay = item.isBonusDay }); // HGKIM 220818 - 학년코드 추가   // 230419 - 보너스일 여부 추가
            }
            //공부하지 않은 날이라도 클릭해서 단어를 볼 수 있게 grade를 과거의 날짜에 넣어준다
            gradeCodeList.Add(item.grdCd);
        }

		// 연월만 가지고 첫째날은 지정한다.
		DateTime standardDay = new DateTime(calenderYear, calenderMonth, 1);
        Logger.Log($"HGKIM || standardDay :: {string.Format(standardDay.ToString("MMdd"))}");

        monthText.text = string.Format(Momo.Localization.Get("date_month"), calenderMonth);
        int emptyCnt = 0;
        switch (standardDay.DayOfWeek)
        {
            case DayOfWeek.Saturday: emptyCnt = 6; break;
            case DayOfWeek.Friday: emptyCnt = 5; break;
            case DayOfWeek.Thursday: emptyCnt = 4; break;
            case DayOfWeek.Wednesday: emptyCnt = 3; break;
            case DayOfWeek.Tuesday: emptyCnt = 2; break;
            case DayOfWeek.Monday: emptyCnt = 1; break;
        }

        int lastday = DateTime.DaysInMonth(calenderYear, calenderMonth);

        for (int i = 0; i < emptyCnt; i++)
        {
            dateList.Add(Instantiate<GameObject>(emptyGo, dateGroup));
        }

        for (int i = 0; i < lastday; i++)
        {
            var go = Instantiate<DailyWordAttendInfo>(dateGo, dateGroup);
            int day = i + 1;
            string d = $"{string.Format("{0:00}", standardDay.Month)}{string.Format("{0:00}", day)}";

            bool isBonusday = false;  // 보너스 받는 날인지 여부
            bool isClear = false;
            string gradeCode = "";          // HGKIM 220818 - 학년코드 추가
            if (attendData.ContainsKey(d))
            {
				isClear = attendData[d].StudyYn;
				gradeCode = attendData[d].gradeCode;  // 추가코드
                isBonusday = attendData[d].isBonusDay;   // 230419 - 보너스일 추가
			}

            //지난 달이고 지난 날이면 grade를 넣어주어서 단어를 볼 수 있게 하자
            if (calenderMonth <= month && day <= today)
            { 
                gradeCode = gradeCodeList[i];
            }

            go.SetDate(day, isClear, isBonusday);
            go.SetGradeCode(gradeCode);  // HGKIM 220818 - 학년코드 추가
            go.gameObject.SetActive(true);
            dateList.Add(go.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(dateGroupTr);
        calendarTr.sizeDelta = new Vector2(calendarTr.rect.width, calendarBgTr.rect.height);
    }
    #endregion

    #region 모은영단어팝업
    public async void ShowCalenderPopup(DailyWordAttendInfo info)//(string date)    // HGKIM 220818 - 학년코드 추가
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_PushNotice,
            new SPEAK_REQ_PushNotice
            {
                grdCd = info.GetGradeCode(),
                studyDt = $"{string.Format(calenderMonth.ToString("D2"))}{string.Format(info.GetDay().ToString("D2"))}"//date//DateTime.Now.ToString("MMdd")    // HGKIM 220818 - 학년코드 추가
            });

        if (result.IsSuccess)
        {
            var data = result.message as SPEAK_RES_PushNotice;
            clickedWordData = data.pushNotice;

            await DownloadResource(clickedWordData);

            SetWord2PopupData(clickedWordData);
            ShowPage2Popup();
        }
    }

	public void SetWord2PopupData(SPEAK_ST_PushNotice data)
	{
        // 리소스 이미지
        Texture texture = ResourceManager.Textures_Get(data.pnWdImage);
        if (texture != null)    popupImage.texture = texture;    // 이미지

        popupEngWord.text = data.pnWdText;      // 영단어
        popupKorWord.text = data.pnWdTrans;      // 번역

        popupEngSentence.text = data.pnPhrText;  // 문장
        popupKorSentence.text = data.pnPhrTrans;  // 번역

        isPopWordPage = true;

        SetWord2PopupAudio(data);
    }

    private void SetWord2PopupAudio(SPEAK_ST_PushNotice data)
    {
        if (isPopWordPage)
        {
            Logger.Log($"HGKIM || SetPopupAudio || 단어 Audio name is {data.pnWdAudio}");
            _popup_uiNativePlayer.clip = ResourceManager.AudioClips_Get(data.pnWdAudio);
        }
        else
        {
            Logger.Log($"HGKIM || SetPopupAudio || 문장 Audio name is {data.pnPhrAudio}");
            _popup_uiNativePlayer.clip = ResourceManager.AudioClips_Get(data.pnPhrAudio);
        }
    }

    public void ClearWord2PopupData()
	{
        popupImage.texture = null;

        popupEngWord.text = "";
        popupKorWord.text = "";

        popupEngSentence.text = "";
        popupKorSentence.text = "";

        _popup_uiNativePlayer.clip = null;
    }

    public void ShowPage2Popup()
    {
        isPopupOn = true;
        isPopWordPage = true;       // 첫 페이지는 무조건 단어 페이지이다
        popupDlg.SetActive(true);
    }

    public void ClosePage2Popup()
    {
        ClearWord2PopupData();

        isPopupOn = false;
        isPopWordPage = true;       // 초기화 값이 true

        popupWord1.SetActive(true);
        popupWord2.SetActive(false);
        popupNavi1.SetActive(true);
        popupNavi2.SetActive(false);

        popupDlg.SetActive(false);
    }
    #endregion

    #region 클릭이벤트모음
    /// <summary>
    /// 캘린더에서 모은 영단어 버튼 눌렀을 때 여기로 들어옴
    /// </summary>
    public void OnClickDay()
    {
        string date = "";

        GameObject clickObject = EventSystem.current.currentSelectedGameObject;
        DailyWordAttendInfo info = clickObject.GetComponent<DailyWordAttendInfo>();

        //grade가 없으면 아직 지나지 않은 시일이라서 볼 수 없다
        if (info.GetGradeCode().Length == 0) return;

        date = $"{string.Format(calenderMonth.ToString("D2"))}{string.Format(info.GetDay().ToString("D2"))}";
        Logger.Log($"HGKIM || OnClickDay || date : {date}");

        //ShowCalenderPopup(date);
        ShowCalenderPopup(info);    // HGKIM 220818 - 학년코드 추가
    }

    /// <summary>
    /// 오늘의 영단어 페이지 음원재생
    /// </summary>
    public void OnClickPlayButton()
    {
        _uiNativePlayer.Play(() => { if (!isPopupOn && !GameData.isTodayWordClear) ShowNextButton(true); }, 0.1f, null);
    }

    /// <summary>
    /// 팝업페이지 음원재생
    /// </summary>
    public void OnClickPopupPlaybutton()
	{
        _popup_uiNativePlayer.Play(null, 0.1f, null);
	}

    public void OnToggle1(bool isOn)
    {
        stage = TodayWord.word;

        if (toggle1.isOn)
        {
            SetPage1Data(todayWordData);

            todayWordOnText.SetActive(true);
            todayWordOffText.SetActive(false);
            collectWordOnText.SetActive(false);
            collectWordOffText.SetActive(true);
            todayWordUI.SetActive(true);
            collectWordUI.SetActive(false);

            word1.SetActive(true);
            word2.SetActive(false);
            navi1.SetActive(true);
            navi2.SetActive(false);
        }
    }

    public void OnToggle2(bool isOn)
    {
        stage = TodayWord.sentence;

        if (toggle2.isOn)
        {
            todayWordOnText.SetActive(false);
            todayWordOffText.SetActive(true);
            collectWordOnText.SetActive(true);
            collectWordOffText.SetActive(false);
            todayWordUI.SetActive(false);
            collectWordUI.SetActive(true);

        }
    }

    public void OnClickNextBtn()
    {
        if (stage == TodayWord.word)
        {
            word1.SetActive(false);
            word2.SetActive(true);
            navi1.SetActive(false);
            navi2.SetActive(true);

            stage = TodayWord.sentence;

            SetAudio(todayWordData, false);
            ShowNextButton(GameData.isTodayWordClear);
        }

        else if (stage == TodayWord.sentence)
        {
            // 클리어 했어도 펭수 팝업은 연다고 함
            //if (isClear)
            //{
            //    OnClickConfirmBtn();
            //}
            //else
            {
                Curtain.instance.Attach(feedBack, 0.8f);

                feedBack.SetActive(true);

                m_PengsooController.Play(Pengsoo.ANI.Good_loop); // 펭수 애니메이션 설정

                AudioController.Play("SFX_UI_Continue");
            }
        }
    }

    /// <summary>
    /// 오늘의 영단어 완료 버튼
    /// </summary>
    public async void OnClickConfirmBtn()
    {
        greatUI.SetActive(false);

        toggle2.isOn = true;
        OnToggle2(true);

        Curtain.instance.Detach(feedBack);

        if (GameData.isTodayWordClear.Equals(false))
        {
            // 이 아래에 완성시키는통신 보내기(+참치캔)
            Logger.Log($"HGKIM || BagPopupCtrl || pnNo : {GameData.User.gradeCode} / studyDt : {DateTime.Now.ToString("MMdd")}");
            var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_PushNotice_InsertLog,
                new SPEAK_REQ_PushNotice_InsertLog
                {
                    pnNo = todayWordData.pnNo,
                    studyDt = DateTime.Now.ToString("yyyyMMdd")
                });
            Logger.Log($"HGKIM || SendRequest || RequesSendEnd!!! result is {result.message}");

            // 완료 플래그 처리 
            if (result.IsSuccess)
            {
                GameData.ClearTodaysWord(); // 통신 받으면 클리어처리 되겠지만 일단 강제로 해본다.

                // 230419 - 참치캔 갱신 통신으로 받는걸로 변경됨
                int coin = (result.message as SPEAK_RES_PushNotice_InsertLog).addedTunaCount;

                GameData.inventory.SafeAddItem(GameData.ItemId.Coin, coin);
                Logger.Log($"오늘의 영단어 참치캔 증가 : {coin.ToString()}");
            }
            // 에러 발생시 예외처리, 오늘의 단어 창을 닫아버린다
			else
                OnClosePopup();
        }

        SetPage2Data(); // 클리어 되었으면 페이지를 다시 세팅한다.
    }

    public void OnClickPopupPre()
    {
        popupWord1.SetActive(true);
        popupWord2.SetActive(false);
        popupNavi1.SetActive(true);
        popupNavi2.SetActive(false);

        isPopWordPage = true;

        SetWord2PopupAudio(clickedWordData);
    }

    public void OnClickPopupNext()
    {
        popupWord1.SetActive(false);
        popupWord2.SetActive(true);
        popupNavi1.SetActive(false);
        popupNavi2.SetActive(true);

        isPopWordPage = false;

        SetWord2PopupAudio(clickedWordData);
    }

    public void OnCloseWordPopup()
	{
        ClosePage2Popup();
    }

    // 달력 이전 버튼
    public void OnClickPrevMonth()
    {
        // 현재가 1월이면 12월로 바꿔주고 연도를 줄여줌
        if (calenderMonth == 1)
        {
            calenderYear--;
            calenderMonth = 12;
        }
        else
		{
            calenderMonth--;
        }

        SetPage2Data();
    }

    // 달력 다음 버튼
    public void OnClickNextMonth()
    {
        // 현재가 12월이면 1월로 바꿔주고 연도를 늘려줌
        if (calenderMonth == 12)
        {
            calenderYear++;
            calenderMonth = 1;
        }
        else
		{
            calenderMonth++;
        }

        SetPage2Data();
    }
    #endregion
}