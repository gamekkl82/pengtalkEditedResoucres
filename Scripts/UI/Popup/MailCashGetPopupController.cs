using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailCashGetPopupController : PopupController
{
    public Text text;

    public override void OnOpen()
    {
        base.OnOpen();
    }

    public override void OnClosePopup()
    {   
        base.OnClosePopup();
    }
    public void Bind()
    {
    }

    public void MailCashGetPopupOpen(int cashNumber)
	{
        text.text = $"참치캔 {cashNumber.ToString("#,##0")}개를 받았어요!";
	}

    public void OnClick()
    {
        Debug.Log("[MailCashGetPopupController/OnClick]");
        
        OnClosePopup();
    }
    public void OnWebViewShow()
    {
        TouchController.Instance.BlockedTouch("EventOnWeb");
    }

    public void OnWebViewClose()
    {
        TouchController.Instance.ReleasedTouch("EventOnWeb");
    }

    public void OnToggle(bool on)
    {
    }
}

