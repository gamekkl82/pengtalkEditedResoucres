using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class RankingPopupController : PopupController
{
    public enum RankingType
    {
        mileage = 0,
        star,
    }
    public enum RankingListType
    {
        Class = 0,
        Total,
    }

    [SerializeField] private GameObject classToggleGroup;
    [SerializeField] private GameObject allToggleGroup;
    [SerializeField] private GameObject starCheckText;
    [SerializeField] private GameObject mileageCheckText;
    [SerializeField] private GameObject mileageGroup;
    [SerializeField] private GameObject starGroup;
    [SerializeField] private Image stampImage;
    [SerializeField] private RawImage myImage;
    [SerializeField] private Text nickName;
    [SerializeField] private Text desc;
    [SerializeField] private Text ranking;
    [SerializeField] private Text mileagePoint;
    [SerializeField] private Text starPoint;
    [SerializeField] private GuideMessage guideMessage;
    [SerializeField] private Toggle mileageToggle;
    [SerializeField] private Toggle starToggle;
    [SerializeField] private Toggle classToggle;
    [SerializeField] private Toggle totalToggle;

    [SerializeField] private GameObject OpenLoginToggleOff; // HGKIM 230118 - 오픈형때 하단 토글탭 가리는 용도

    public BigScroll scroll;

    private RankingType rankingPageType = RankingType.mileage;
    private RankingListType rankingListType = RankingListType.Class;
    private SPEAK_RES_StudyRanking data = null;
    private string mileageMessage;
    private string starMessage;
    private int receivedCanCount;

    public void Bind(SPEAK_RES_StudyRanking d)
    {
        data = d;
        mileageMessage = Momo.Localization.Get("popup_ranking_mileage_message");
        starMessage = Momo.Localization.Get("popup_ranking_star_message");
        mileageToggle.isOn = true;
        classToggle.isOn = true;

        // HGKIM 230118 - 오픈 될 때에 개방형만 바뀌어야 하는 것 추가
        if (GameData.isOpenPublicLogin)
        {
            // 도움말에 들어가는 메시지 수정
            mileageMessage = "영어 활동을 한 시간이 많을 수록 마일리지 랭킹이 올라가요.";
            starMessage = "영어 활동에서 획득한 별점이 많을 수록 별점 랭킹이 올라가요.";

            OpenLoginToggleOff.SetActive(true); // 토글 버튼 위를 가린다.

            // Update 에서 Text가 반영되니까 한박자 느리게 반영되어서 여기에 초기화값을 추가함
            // 이번주 라는 문구 삭제
            if (rankingPageType == RankingType.mileage)
            {
                desc.text = "<color=#FFEC10>마일리지 랭킹</color>을 확인해보세요!";
                mileagePoint.text = "0";
            }
            else if (rankingPageType == RankingType.star)
            {
                desc.text = "<color=#FFEC10>별점 랭킹</color>을 확인해보세요!";
                starPoint.text = "0";
            }

            // 랭킹은 전체 랭킹만 표기
            ranking.text = "0";
        }
        // 추가사항 여기까지
    }

    private void SetGuideMessage(RankingType type)
    {
        if (type == RankingType.mileage)
            guideMessage.SetMessage(mileageMessage);
        else if (type == RankingType.star)
            guideMessage.SetMessage(starMessage);
    }

    private async void GetStudyRanking()
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.GetStudyRanking, new SPEAK_REQ_StudyRanking { rankType = rankingPageType.ToString() });
        if (result.IsSuccess)
        {
            data = result.message as SPEAK_RES_StudyRanking;

            SetGuideMessage(rankingPageType);
            UpdateMyInfo();
            if(classToggle.isOn)
                classToggle.isOn = false;
            classToggle.isOn = true;
        }
        else
        {
            OnClosePopup();
        }
    }

    public override void OnClosePopup()
    {
        mileageToggle.isOn = false;
        starToggle.isOn = false;
        classToggle.isOn = false;
        totalToggle.isOn = false;
        guideMessage.Close();
        base.OnClosePopup();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        CheckNewWeek();
        Utility.Firebase_SetCrrentScreen("랭킹", "ProfilePopupController");
    }

    private async void CheckNewWeek()
    {
        if(NewMarkManager.Instance.IsNewMark(NewMarkManager.NewMarkType.NewMark_Ranking))
        {
            // HGKIM 230215 - 통신 추가
            string text1 = ""; // 반 마일리지
            string text2 = ""; // 반 별
            string text3 = ""; // 학교 마일리지
            string text4 = ""; // 학교 별
            string can = "0";    // 획득한 캔

            var rankResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_RankingPrize, "{}");
            if (rankResult.IsSuccess)
            {
                SPEAK_ST_RankingPrize resultClass;
                SPEAK_ST_RankingPrize resultSchool;

                var resultData = rankResult.message as SPEAK_RES_RankingPrize;

                // 예외처리
                if (resultData.rankingPrizes.Count > 1)
                {
                    // 순서대로 안 올 경우를 대비
                    // C : class | S : school | W : weekly | M : Month  || (W, M 은 개방형 사용자만)
                    if (resultData.rankingPrizes[0].rankingCategory.Equals("C") || resultData.rankingPrizes[0].rankingCategory.Equals("c"))
                    {
                        resultClass = resultData.rankingPrizes[0];
                        resultSchool = resultData.rankingPrizes[1];
                    }
                    else
                    {
                        resultClass = resultData.rankingPrizes[1];
                        resultSchool = resultData.rankingPrizes[0];
                    }

                    text1 = (resultClass.mileageRank == 0) ? "-" : resultClass.mileageRank.ToString();
                    text2 = (resultClass.starRank == 0) ? "-" : resultClass.starRank.ToString();
                    text3 = (resultSchool.mileageRank == 0) ? "-" : resultSchool.mileageRank.ToString();
                    text4 = (resultSchool.starRank == 0) ? "-" : resultSchool.starRank.ToString();
                    can = (resultClass.prizeCnt + resultSchool.prizeCnt).ToString();

                    // UI의 참치캔 갯수가 업데이트 되지 않는 현상 수정 [운영-0-OC고객센터/3936]
                    receivedCanCount = resultClass.prizeCnt + resultSchool.prizeCnt;
                }
                else
				{
                    Logger.Log("SPEAK_RES_RankingPrize ERROR!!");
                    RequestError();
                }
            }
            else
			{
                RequestError();
                return;
			}

            // HGKIM 230209 - 사양 변경으로 팝업 컨트롤러 수정
            //PopupManager.Instance.OpenPopup<ConfirmPopupController>("NewRankingPopupController", popup =>
            PopupManager.Instance.OpenPopup<RankingResultPopupController>("RankingResultPopupController", popup =>
            {
                popup.Bind(null, text1, text2, text3, text4, can, 
                    () => 
                    { 
                        SendRankingPrizeGet();
                        popup.OnClosePopup();
                    });
            });

            NewMarkManager.Instance.RemoveNewMark(NewMarkManager.NewMarkType.NewMark_Ranking);
        }
    }

    // 통신했을때 에러처리 하는부분 추가 (메시지 팝업 후 랭킹창을 닫는다.)
    public void RequestError()
    {
        PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController("랭킹 정보를 가져오는데 실패하였습니다.\n잠시 후 다시 확인해 주세요.", okAction: () => { OnClose(); });
            },
            onCloseAction: () =>
            {
                OnClosePopup();
            });
    }

    /// <summary>
    /// 보상을 받았다고 확인 요청을 보낸다.
    /// </summary>
    public async void SendRankingPrizeGet()
    {
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_RankingPrize_Receive, new SPEAK_REQ_RankingPrize_Receive { rankingCategory = "" });
        
        if (result.IsSuccess.Equals(false))
        {
            RequestError();

            return;
        }
        
        // UI의 참치캔 갯수가 업데이트 되지 않는 현상 수정 [운영-0-OC고객센터/3936]
        GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count += receivedCanCount;
        SceneController.Common.GetCashTextObj().text =
            GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count >= 999999 ?
                "+999,999" :
                GameData.inventory.itemInfo[(int)GameData.ItemId.Coin].Count.ToString("#,0");
    }

    public void OnToggle1(bool tf)
    {
        mileageCheckText.SetActive(tf);        
        if (false == tf)
            return;

        rankingPageType = RankingType.mileage;
        Utility.Firebase_LogEvent("랭킹_마일리지_메뉴클릭");
        GetStudyRanking();
    }

    public void OnToggle2(bool tf)
    {
        starCheckText.SetActive(tf);
        if (false == tf)
            return;

        rankingPageType = RankingType.star;
        Utility.Firebase_LogEvent("랭킹_별점_메뉴클릭");
        GetStudyRanking();
    }

    public void OnToggle3(bool tf)
    {
        classToggleGroup.SetActive(tf);
        if (false == tf)
            return;
        
        // HGKIM 230118 - 개방형은 전체 리스트를 뿌린다.
        if (GameData.isOpenPublicLogin)
            rankingListType = RankingListType.Total;
        else
            rankingListType = RankingListType.Class;        
        UpdateRankingList();
    }

    public void OnToggle4(bool tf)
    {
        allToggleGroup.SetActive(tf);
        if (false == tf)
            return;

        rankingListType = RankingListType.Total;
        UpdateRankingList();
    }

    public void OnDescBtn()
    {
        guideMessage.OnGuide();
    }

    private void UpdateMyInfo()
    {
        if (data == null)
            return;

        if (data.myRank == null)
            return;

        ResourceManager.Instance.LoadProfile(myImage, GameData.User.imageNo);
        nickName.text = data.myRank.indvNickNm;
        mileageGroup.SetActive(rankingPageType == RankingType.mileage);
        starGroup.SetActive(rankingPageType == RankingType.star);

        // HGKIM 230118 - 개방형 분리
        if (GameData.isOpenPublicLogin)
        {
            // 이번주 라는 문구 삭제
            if (rankingPageType == RankingType.mileage)
            {
                desc.text = "<color=#FFEC10>마일리지 랭킹</color>을 확인해보세요!";
                mileagePoint.text = data.myRank.score.ToString();                   
            }
            else if (rankingPageType == RankingType.star)
            {
                desc.text = "<color=#FFEC10>별점 랭킹</color>을 확인해보세요!";
                starPoint.text = data.myRank.score.ToString();
            }

            // 랭킹은 전체 랭킹만 표기
            ranking.text = data.myRank.indvAllRank.ToString();
        }
        // 아래는 기존 로그인
		else
        {
            if (rankingPageType == RankingType.mileage)
            {
                desc.text = string.Format(Momo.Localization.Get("popup_ranking_mileage_week_message"), "<color=#FFEC10>", "</color>");
                mileagePoint.text = data.myRank.score.ToString();
            }
            else if (rankingPageType == RankingType.star)
            {
                desc.text = string.Format(Momo.Localization.Get("popup_ranking_star_week_message"), "<color=#FFEC10>", "</color>");
                starPoint.text = data.myRank.score.ToString();
            }

            ranking.text = string.Format(Momo.Localization.Get("popup_ranking_rank"), data.myRank.indvClsRank, data.myRank.indvAllRank);
        }
    }

    private void UpdateRankingList()
    {
        scroll.ClearList();

        if (rankingListType == RankingListType.Total)
        {
            foreach(var r in data.allClassRanks)
            {
                scroll.AddItem(new RankingScrollItemData { rankType = rankingPageType, index = r.stdetNo, rank = r.rank, nickName = r.indvNickNm, score = r.score, listType = rankingListType, profileIdx = r.imgIdx });
            }
        }
        else if(rankingListType == RankingListType.Class)
        {
            foreach (var r in data.classRanks)
            {
                scroll.AddItem(new RankingScrollItemData { rankType = rankingPageType, index = r.stdetNo, rank = r.rank, nickName = r.indvNickNm, score = r.score, listType = rankingListType, profileIdx = r.imgIdx });
            }
        }
    }
}
