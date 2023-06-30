using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TunaCanRecordCenterScrollListItemData : IBigScrollItemData
{
    public SPEAK_ST_RewardTuna rewardTunaData;
}


public class TunaCanRecordCenterScrollListItem : BigScrollItem
{
    public Text substanceTextSizeFit;
    public Text substance;                      // 내용 text
    public Text time;                           // 날자 text

    private float width;

    private TunaCanRecordCenterScrollListItemData itemData;  // 스크롤리스트
                
    public Text tunacanNumber;

    private void Awake()
    {
        width = substanceTextSizeFit.rectTransform.rect.width;
    }

    public override void UpdateData(IBigScrollItemData data)
    {
        base.UpdateData(data);
        itemData = (TunaCanRecordCenterScrollListItemData)data;

		// 내용 text를 가로 - 200 사이즈에 맞춰서 fit 한다.
		substanceTextSizeFit.rectTransform.sizeDelta = new Vector2(width - 200, substanceTextSizeFit.rectTransform.rect.height);
		substanceTextSizeFit.SetTextWithEllipsis(itemData.rewardTunaData.title);

        substance.text = substanceTextSizeFit.text;
        substance.SetTextWithEllipsis(substance.text);
        substanceTextSizeFit.text = string.Empty;

        time.text = GetSimpleDate(itemData.rewardTunaData.receiveDate, "yyyyMMdd");

        if (itemData.rewardTunaData.tunaCoinCount > 9999)
            tunacanNumber.text = "+9999개";
        else
            tunacanNumber.text = $"{itemData.rewardTunaData.tunaCoinCount.ToString()}개";
    }

    // string 로 날자를 받아서 형식으로 리턴해주는거
    string GetSimpleDate(string date, string pattern = "yyyyMMdd")
	{
        string result;

        DateTime parsedDate = Utility.GetDateTime(date, pattern);
        //result = $"{parsedDate.ToString("yyyy.MM.dd HH:mm")}";// 디자인 변경됨
        result = $"{parsedDate.ToString("yyyy.MM.dd")}";

        return result;
    }

    public void OnWebViewShow()
    {
        TouchController.Instance.BlockedTouch("EventOnWeb");
    }

    public void OnWebViewClose()
    {
        TouchController.Instance.ReleasedTouch("EventOnWeb");
    }
}
