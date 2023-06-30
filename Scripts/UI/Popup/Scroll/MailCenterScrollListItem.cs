using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MailCenterScrollListItemData : IBigScrollItemData
{
    public SPEAK_ST_Mailbox mailBoxData;    // 스크롤 안에 넘어오는 우편함 데이터
    public Action buttonActionDelegate;     // 버튼 클릭시 팝업컨트롤러 쪽으로 리프레시 액션을 보내기 위해
}

public class MailCenterScrollListItem : BigScrollItem
{
    public Text head;                           // 머리말 text
    public Text title;                          // 메인 text
    public Text time;                           // 날자 text

    private MailCenterScrollListItemData itemData;  // 스크롤리스트
    private float width;                            // 계산용

    // 버튼 컨트롤용
    public Button tunaGetButton;
    public Image tunaGetButtonDisableImage; // 버튼 비활성화용 이미지
    public Image tunacanIcon_on;            // 참치캔 Image on
    public Image tunacanIcon_off;           // 참치캔 Image off
    public Text tunaButtonText;             // 버튼 안 글자중 받기 완료 변경용
    public Text tunacanNumber;              // 참치캔 갯수

    private void Awake()
    {
        width = head.rectTransform.rect.width;
    }

    public override void UpdateData(IBigScrollItemData data)
    {
        base.UpdateData(data);
        itemData = (MailCenterScrollListItemData)data;

        // UI 어떤걸 표기할지 결정하는 부분
        switch(itemData.mailBoxData.rewardType) // 보상 종류: T-참치캔
        {
            case "T":
            {
                if(itemData.mailBoxData.isReceived) // 보상 수신 여부
                {
                    if (tunaGetButton.gameObject.activeSelf.Equals(false))  // 버튼 세팅
                        tunaGetButton.gameObject.SetActive(true);
                    // 참치캔 아이콘
                    tunacanIcon_on.enabled = false;
                    tunacanIcon_off.enabled = true;
                    // 참치캔 버튼 비활성화 이미지
                    tunaGetButtonDisableImage.enabled = true;
                    tunaButtonText.text = "완료";
                    tunaGetButton.enabled = false;
                    tunaGetButton.interactable = false;
                    tunacanNumber.text = itemData.mailBoxData.rewardCount.ToString();
                    if (itemData.mailBoxData.rewardCount > 9999)
                    {
                        tunacanNumber.text = "+9999";
                    }
                    head.rectTransform.sizeDelta = new Vector2(width - 105, head.rectTransform.rect.height);
                }
                else // 보상 안받았을 때
                {
                    if (tunaGetButton.gameObject.activeSelf.Equals(false))  // 버튼 세팅
                        tunaGetButton.gameObject.SetActive(true);
                    tunacanIcon_on.enabled = true;
                    tunacanIcon_off.enabled = false;
                    tunaGetButtonDisableImage.enabled = false;
                    tunaButtonText.text = "받기";
                    tunaGetButton.enabled = true;
                    tunaGetButton.interactable = true;
                    tunacanNumber.text = itemData.mailBoxData.rewardCount.ToString();
                    if (itemData.mailBoxData.rewardCount > 9999)
                    {
                        tunacanNumber.text = "+9999";
                    }
                    head.rectTransform.sizeDelta = new Vector2(width - 105, head.rectTransform.rect.height);
                }
            }
            break;

            case "":    // 비어있는 것이 참치캔이 없는 상태이다.
            default:    // 디폴트도 없는 상태로 간다.
                tunaGetButton.gameObject.SetActive(false); // 버튼 그룹 자체를 꺼버린다.
                head.rectTransform.sizeDelta = new Vector2(width, head.rectTransform.rect.height);
                break;
		}

        head.SetTextWithEllipsis($"<color=#fc7c42>[{MailType(itemData.mailBoxData.mailType)}]</color> {itemData.mailBoxData.subject}");
        title.text = head.text;
        head.text = string.Empty;
        time.SetTextWithEllipsis($"{Utility.GetSimpleDate_Day(itemData.mailBoxData.sendDate)} (보관 기간 : {itemData.mailBoxData.storageDays}일)");
    }

    // 서버에서 넘어온 타입을 분류해서 Text를 리턴함
    // 우편 종류: T-선생님, S-시스템, E-이벤트
    public string MailType(string type)
	{
        string rtnStr = "";
		switch (type)
		{
            case "T":
                rtnStr = "선생님";
                break;

            case "S":
                rtnStr = "시스템";
                break;

            case "E":
                rtnStr = "이벤트";
                break;

            default:
                Logger.Log("없는 타입이므로 빈칸으로 출력");
                break;
        }

        return rtnStr;
	}

	public override void OnBtnClicked()
    {
        base.OnBtnClicked();

        Logger.Log($"ItemClicked ==> {itemData.mailBoxData.url}");

#if UNITY_STANDALONE    // HGKIM 230109 - PC버전은 OpenURL로 열도록 변경
		Application.OpenURL(itemData.mailBoxData.url);
#else
        GamebaseController.Platform.OpenWebView(itemData.mailBoxData.url, OnWebViewShow, OnWebViewClose);
#endif
    }

    public void OnWebViewShow()
    {
        TouchController.Instance.BlockedTouch("EventOnWeb");
    }

    public void OnWebViewClose()
    {
        TouchController.Instance.ReleasedTouch("EventOnWeb");
    }

    // 참치캔 받기 버튼 클릭시
    public async void OnClickGetTunaCanBtn()
	{
        Logger.Log("Get Button clicked!!");

        // 참치캔 받기 통신을 여기에 넣는다. - 메일 번호를 담아서 보낸다.
        var result = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_Mailbox_Receive, 
            new SPEAK_REQ_Mailbox_Receive{ mailNo = itemData.mailBoxData.mailNo });
        if(result.IsSuccess)
		{
            int count = (result.message as SPEAK_RES_Mailbox_Receive).addedTunaCount;

            Logger.Log($"Return Count is {count}");
            // 확인 팝업을 띄우고
            var closePopup = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<MailCashGetPopupController>(popup =>
            {
                popup.MailCashGetPopupOpen(count);
            }, onCloseAction: () => { closePopup.SetResult(true); });
            await closePopup.Task;

            // 참치캔을 증가시킨다.
            GameData.inventory.SafeAddItem(GameData.ItemId.Coin, count);
        }
        else
		{
            Logger.LogError("참치캔 통신에 실패했다!!!");
            var closePopup = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController("참치캔 받기에 실패하였습니다!\n잠시 후 다시 시도해 주세요.",
                    okAction: () => { closePopup.SetResult(true); });
            });
            await closePopup.Task;
        }

        // 팝업컨트롤러쪽에 리플레시를 위해
        if (itemData.buttonActionDelegate != null)
            itemData.buttonActionDelegate();
    }
}
