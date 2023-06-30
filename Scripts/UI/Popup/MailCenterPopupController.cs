using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MailCenterPopupController : PopupController
{
    public BigScroll mailScroll;

    public Button allTunaGetButton; // 버튼 활성화 비활성화
    public Text allTunaCanNumber;   // 전체 받기 표기용
    public Image CurtionImage;

    public int allTunaNum;          // 받을 수 있는 최대 갯수

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshList();

        NewMarkManager.Instance.RemoveNewMark(NewMarkManager.NewMarkType.NewMark_mail);
    }
    public override void OnClosePopup()
    {
        allTunaCanNumber.text = ""; // 전체 참치캔 갯수를 지운다.
        mailScroll.ClearList(); // 스크롤 데이터를 지운다.
        base.OnClosePopup();
    }

	public void Bind()
	{
        //CurtionOn(false);
    }

    public void OnClose()
	{

        OnClosePopup();
	}

	public async void RefreshList()
    {
        mailScroll.ClearList();

        bool isSuccess = await MailListGet();   // 이 안에서 통신한다.

        if (isSuccess)
        {
            allTunaNum = 0; // 받을 수 있는 전체 캔 갯수를 초기화 하고 시작한다.
            applyMailData();

            // 전체 캔 갯수를 넣어준다.
            allTunaCanNumber.text = allTunaNum.ToString();

            if (allTunaNum > 99999)
            {
                allTunaCanNumber.text = "+99999";
            }
        }
		else    // 통신에 실패했을 경우
		{
            MailListGetError();
        }
    }

    // 통신으로 메일 리스트를 가져온다.
    public async Task<bool> MailListGet()
	{
        var mailListResult = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_Mailbox, new SPEAK_REQ_Mailbox());
        if (mailListResult.IsSuccess)
        {
            var mailListData = mailListResult.message as SPEAK_RES_Mailbox;

            if (mailListData.mailboxes.Count > 0)
                GameData.mailDataList = mailListData.mailboxes;
            else
			{
                Logger.LogError($"메일 리스트에 데이터가 없다!!!");
                
                var closePopup = new TaskCompletionSource<bool>();
                PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
                {
                    popup.SetSystemPopupController("받을 참치캔 보상이 없어요!",
                        okAction: () => { closePopup.SetResult(true); });
                });
                await closePopup.Task;

                OnClosePopup(); // 창을 닫는다.
                
                return true;
            }
        }
        else    // mailListResult.IsSuccess == false
        {
            Logger.LogError($"SPEAK_RES_Mailbox Fail! ==> ErrCode : {mailListResult.Code}");
            return false;
        }

        return true;
    }

    // 메일 리스트를 가져오는게 실패했을경우 이 쪽으로 보낸다.
    // 에러 팝업 확인하면 메일함이 닫힘
    public async void MailListGetError()
	{
        var closePopup = new TaskCompletionSource<bool>();
        PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
        {
            popup.SetSystemPopupController("메일 정보 획득에 실패하였습니다!\n잠시 후 다시 시도해 주세요.",
                okAction: () => { closePopup.SetResult(true); });
        });
        await closePopup.Task;

        OnClosePopup(); // 창을 닫는다.
	}


    public async void OnClickAllGet()
	{
        Logger.Log("On Click AllGet Button!!!");

        // 전부 받는다는 통신 보내고
        var receiveAll = await MPServiceFacade.Network.Request(NetworkServiceRequest.SPEAK_REQ_Mailbox_ReceiveAll, new SPEAK_REQ_Mailbox_ReceiveAll());
        // 성공하면
        if(receiveAll.IsSuccess)
		{
            int count = (receiveAll.message as SPEAK_RES_Mailbox_ReceiveAll).addedTunaCount;

            Logger.Log($"Return Count is {count}");
            
            // 확인 팝업을 띄우고
            var closePopup = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<MailCashGetPopupController>(popup =>
            {
                popup.MailCashGetPopupOpen(count);
            }, onCloseAction: () => {
                closePopup.SetResult(true);
            });
            await closePopup.Task;

            // 참치캔을 증가시킨다.
            GameData.inventory.SafeAddItem(GameData.ItemId.Coin, count);
        }
        else // 통신에 실패했을 경우
		{
            var closePopup = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController("참치캔 받기에 실패하였습니다!\n잠시 후 다시 시도해 주세요.",
                    okAction: () => { closePopup.SetResult(true); });
            });
            await closePopup.Task;
        }
        // 리스트를 다시 갱신한다.
        RefreshList();
    }
    
    /// <summary>
    /// 입력 방해용 커튼 켜고 끄기
    /// </summary>
    /// <param name="isOn">true : 커튼 켜짐</param>
    public void CurtionOn(bool isOn)
	{
        CurtionImage.enabled = isOn;
	}


    public void applyMailData ()
    {
        List<int> _mailIdList = new List<int>();
        foreach (var data in GameData.mailDataList)
        {
            _mailIdList.Add(data.mailNo);

            mailScroll.AddItem(new MailCenterScrollListItemData()
            {
                mailBoxData = data,
                buttonActionDelegate = RefreshList,
            });

            // 받을 수 있는 전체 캔 갯수를 안넘겨줘서 여기서 계산한다.
            if (data.isReceived) // 이미 받은 보상이면
            {
                continue;
            }
            
            allTunaNum += data.rewardCount; // 갯수를 추가
        }

        if (allTunaNum.Equals(0)) // 전체 받기 할 보상의 갯수가 0이면
        {
            allTunaGetButton.interactable = false; // 전체받기 하는 버튼을 사용할 수 없게 함
        }

        GameData.Mails.saveMailIdList(_mailIdList);
    }
}
