using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TunaCanRecordPopupController : PopupController
{
	[SerializeField] private GameObject blockObject;

	[SerializeField] private Toggle mainToggleGroup_Default;

	[SerializeField] private GameObject thisWeekObject;
	[SerializeField] private BigScroll thisWeekScroll;
	[SerializeField] private GameObject lastWeekObject;
	[SerializeField] private BigScroll lastWeekScroll;
	[SerializeField] private GameObject monthObject;
	[SerializeField] private BigScroll monthScroll;
	[SerializeField] private GameObject threeMonthObject;
	[SerializeField] private BigScroll threeMonthScroll;

	enum MainTab
	{
		thisWeek,
		lastWeek,
		month,
		threeMonth,
	}

	MainTab tab = MainTab.thisWeek;

	public BigScroll recordScroll;

	public override void OnOpen()
	{
		base.OnOpen();
		RefreshList();
		UpdateUI();
	}

	public override void OnClosePopup()
	{
		thisWeekScroll.ClearList();
		lastWeekScroll.ClearList();
		monthScroll.ClearList();
		threeMonthScroll.ClearList();

		base.OnClosePopup();
	}

	public void Bind()
	{
	}

	void UpdateUI()
	{
		thisWeekObject.SetActive(tab == MainTab.thisWeek);
		lastWeekObject.SetActive(tab == MainTab.lastWeek);
		monthObject.SetActive(tab == MainTab.month);
		threeMonthObject.SetActive(tab == MainTab.threeMonth);
	}

	/// <summary>
	/// 통신에러가 발생했을 때 여기서 처리한다.
	/// 팝업 메시지를 띄우고 확인을 누르면 창과 함께 닫힌다.
	/// </summary>
	private async void ConnectError()
	{
		var closePopup = new TaskCompletionSource<bool>();
		PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
		{
			popup.SetSystemPopupController("정보 획득에 실패하였습니다!\n잠시 후 다시 시도해 주세요.",
				okAction: () => { closePopup.SetResult(true); });
		});
		await closePopup.Task;

		OnClosePopup(); // 획득정보 창을 닫는다.
	}

	private async void RefreshList()
	{
		blockObject.SetActive(true); // 통신해서 리스트 받아오는 중에 뭐 못누르게 하기 위해

		await DataRequest(1, thisWeekScroll);
		await DataRequest(2, lastWeekScroll);
		await DataRequest(3, monthScroll);
		await DataRequest(4, threeMonthScroll);

		blockObject.SetActive(false); // 다 끝나면 꺼준다.
	}

	private async Task DataRequest(int period, BigScroll targetScrollView)
	{
		var response = await MPServiceFacade.Network.Request(
			NetworkServiceRequest.SPEAK_REQ_RewardTuna,
			new SPEAK_REQ_RewardTuna
			{
				period = period
			});
		var rewards = (response.message as SPEAK_RES_RewardTuna)?.rewardTunas;

		if (rewards is null)
		{
			return;
		}

		if (response.IsSuccess)
		{
			SetScrollViewItems(rewards, targetScrollView);

			return;
		}

		Logger.LogError("데이터를 가져오는데 실패!!!!");

		ConnectError();
	}

	private void SetScrollViewItems(List<SPEAK_ST_RewardTuna> list, BigScroll targetScrollView)
	{
		foreach (var data in list)
		{
			targetScrollView.AddItem(new TunaCanRecordCenterScrollListItemData
			{
				rewardTunaData = data
			});
		}
	}

	#region 버튼 클릭

	public void OnClickThisWeekTab(bool isOn)
	{
		if (tab == MainTab.thisWeek) return;

		tab = MainTab.thisWeek;
		Logger.Log("OnClick This Week Tab");
		UpdateUI();
	}

	public void OnClickLastWeekTab(bool isOn)
	{
		if (tab == MainTab.lastWeek) return;

		tab = MainTab.lastWeek;
		Logger.Log("OnClick Last Week Tab");
		UpdateUI();
	}

	public void OnClickMonthTab(bool isOn)
	{
		if (tab == MainTab.month) return;

		tab = MainTab.month;
		Logger.Log("OnClick Month Tab");
		UpdateUI();
	}

	public void OnClick3MonthTab(bool isOn)
	{
		if (tab == MainTab.threeMonth) return;

		tab = MainTab.threeMonth;
		Logger.Log("OnClick 3Month Tab");
		UpdateUI();
	}

	#endregion
}