using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static GameData;

public class NewMarkManager : Singleton<NewMarkManager>
{
    public enum NewMarkType
    {
        None = -1,
        NewMark_Notice,        
        NewMark_NoticeItem,
        NewMark_TeacherItem,
        NewMark_MyBag,
        NewMark_Stamp,
        NewMark_StampItem,
        NewMark_Ranking,
        NewMark_ScanItItem,
        NewMark_mail,       // 230510 - 메일 뉴마크를 위해 등록
    }

    public delegate void OnNewMarkEvent(NewMarkType newMarkType, bool isActive, string index = default(string), NotiTeacherType notiTeacherType = NotiTeacherType.None);
    public delegate void OnNewStamp(int index);
    public event OnNewMarkEvent onNewMarkEvent;
    public static event OnNewStamp onNewStampEvent;

#if UNITY_EDITOR
    [SerializeField] private NewMarkType testNewMarkType = NewMarkType.NewMark_StampItem;
    [SerializeField] private int testIndex = 0;
    [SerializeField] private NotiTeacherType teacherType = NotiTeacherType.None;
#endif

    private List<Stamp> newStampList = new List<Stamp>();
    private const string hasStampKey = "hasStamp_";
    private Dictionary<NewMarkType, List<string>> newMarkList = new Dictionary<NewMarkType, List<string>>();
    private Dictionary<NewMarkType, List<string>> checkedNewMarkList = new Dictionary<NewMarkType, List<string>>();

    public override void Awake()
    {
        base.Awake();

        newMarkList.Add(NewMarkType.NewMark_NoticeItem, PlayerPrefsManager.GetArrayList<string>(NewMarkType.NewMark_NoticeItem.ToString()));
        newMarkList.Add(NewMarkType.NewMark_TeacherItem, PlayerPrefsManager.GetArrayList<string>(NewMarkType.NewMark_TeacherItem.ToString()));
        newMarkList.Add(NewMarkType.NewMark_StampItem, PlayerPrefsManager.GetArrayList<string>(NewMarkType.NewMark_StampItem.ToString()));
        newMarkList.Add(NewMarkType.NewMark_ScanItItem, PlayerPrefsManager.GetArrayList<string>(NewMarkType.NewMark_ScanItItem.ToString()));
        newMarkList.Add(NewMarkType.NewMark_mail, PlayerPrefsManager.GetArrayList<string>(NewMarkType.NewMark_mail.ToString()));

        checkedNewMarkList.Add(NewMarkType.NewMark_NoticeItem, PlayerPrefsManager.GetArrayList<string>($"Checked_{NewMarkType.NewMark_NoticeItem}"));
        checkedNewMarkList.Add(NewMarkType.NewMark_TeacherItem, PlayerPrefsManager.GetArrayList<string>($"Checked_{NewMarkType.NewMark_TeacherItem}"));
        checkedNewMarkList.Add(NewMarkType.NewMark_StampItem, PlayerPrefsManager.GetArrayList<string>($"Checked_{NewMarkType.NewMark_StampItem}"));
        checkedNewMarkList.Add(NewMarkType.NewMark_ScanItItem, PlayerPrefsManager.GetArrayList<string>($"Checked_{NewMarkType.NewMark_ScanItItem}"));
        checkedNewMarkList.Add(NewMarkType.NewMark_mail, PlayerPrefsManager.GetArrayList<string>($"Checked_{NewMarkType.NewMark_mail}"));
    }

    public void AddNewMark(NewMarkType newMarkType, int index = 0)
    {
        AddNewMark(newMarkType, index.ToString());
    }

    public void RemoveNewMark(NewMarkType newMarkType, int index = 0)
    {
        RemoveNewMark(newMarkType, index.ToString());
    }

    public void RemoveNewMark(NotiTeacherType notiTeacherType, int index)
    {
        string idx = $"{index}-{notiTeacherType}";
        RemoveNewMark(NewMarkType.NewMark_TeacherItem, idx);
    }

    public void AddNewMark(NewMarkType newMarkType, string index)
    {
        if (newMarkType == NewMarkType.NewMark_Ranking)
        {
            PlayerPrefsManager.SetInt(newMarkType.ToString(), 1);
        }
        else if (newMarkType == NewMarkType.NewMark_mail)
        {
            onNewMarkEvent?.Invoke(newMarkType, true);
            return;
        }
        else
        {
            if (newMarkList[newMarkType].Contains(index) == false)
            {
                Debug.Log($"AddNewMark:{newMarkType}:{index}");
                newMarkList[newMarkType].Add(index);
            }
        }

        onNewMarkEvent?.Invoke(newMarkType, true, index);
        InvokeTopGroup(newMarkType);
    }

    public void RemoveNewMark(NewMarkType newMarkType, string index)
    {
        if (newMarkType == NewMarkType.NewMark_Ranking)
        {
            PlayerPrefsManager.SetInt(newMarkType.ToString(), 0);
        }
        else
        {
            newMarkList[newMarkType].Remove(index);
        }

        onNewMarkEvent?.Invoke(newMarkType, false, index);

        if (newMarkType == NewMarkType.NewMark_NoticeItem || newMarkType == NewMarkType.NewMark_TeacherItem || newMarkType == NewMarkType.NewMark_StampItem || newMarkType == NewMarkType.NewMark_ScanItItem)
        {
            if(false == checkedNewMarkList[newMarkType].Contains(index))
            {
                checkedNewMarkList[newMarkType].Add(index);
            }
        }

        InvokeTopGroup(newMarkType);
    }

    private void InvokeTopGroup(NewMarkType newMarkType)
    {
        if (newMarkType == NewMarkType.NewMark_Ranking)
        {
            onNewMarkEvent?.Invoke(NewMarkType.NewMark_MyBag, IsNewMark(NewMarkType.NewMark_MyBag));
        }
        else if (newMarkType == NewMarkType.NewMark_StampItem)
        {
            onNewMarkEvent?.Invoke(NewMarkType.NewMark_MyBag, IsNewMark(NewMarkType.NewMark_MyBag));
            onNewMarkEvent?.Invoke(NewMarkType.NewMark_Stamp, IsNewMark(NewMarkType.NewMark_Stamp));
        }
        else if (newMarkType == NewMarkType.NewMark_NoticeItem || newMarkType == NewMarkType.NewMark_TeacherItem)
        {
            onNewMarkEvent?.Invoke(NewMarkType.NewMark_Notice, IsNewMark(NewMarkType.NewMark_Notice));
        }
    }

    public void CheckToAddNewMark(NewMarkType newMarkType, int index = 0, NotiTeacherType notiTeacherType = NotiTeacherType.None)
    {
        CheckToAddNewMark(newMarkType, index.ToString(), notiTeacherType);
    }

    public void CheckToAddNewMark(NewMarkType newMarkType, string index = default(string), NotiTeacherType notiTeacherType = NotiTeacherType.None)
    {
        string idx = index;
        if(newMarkType == NewMarkType.NewMark_NoticeItem || newMarkType == NewMarkType.NewMark_StampItem)
        {

        }
        else if(newMarkType == NewMarkType.NewMark_TeacherItem)
        {
            index = $"{index}-{notiTeacherType}";
        }
        else
        {
            Debug.Log("NewMarkType Error.");
            return;
        }

        if (false == checkedNewMarkList[newMarkType].Contains(index))
        {
            AddNewMark(newMarkType, index);
        }
    }

    public bool IsNewMark(NewMarkType newMarkType, int index, NotiTeacherType notiTeacherType)
    {
        return IsNewMark(newMarkType, $"{index}-{notiTeacherType}");
    }

    public bool IsNewMark(NewMarkType newMarkType, string index = default(string))
    {
        if (newMarkType == NewMarkType.NewMark_MyBag)
        {
            return newMarkList[NewMarkType.NewMark_StampItem].Count > 0 || IsNewMark(NewMarkType.NewMark_Ranking);
        }
        else if(newMarkType == NewMarkType.NewMark_Notice)
        {
            return newMarkList[NewMarkType.NewMark_NoticeItem].Count > 0 || newMarkList[NewMarkType.NewMark_TeacherItem].Count > 0;
        }
        else if(newMarkType == NewMarkType.NewMark_Stamp)
        {
            return newMarkList[NewMarkType.NewMark_StampItem].Count > 0;
        }
        else if (newMarkType == NewMarkType.NewMark_Ranking)
        {
            if (0 == PlayerPrefsManager.GetInt(newMarkType.ToString()))
                return false;
            else
                return true;
        }
        else if(newMarkType == NewMarkType.NewMark_StampItem)
        {
            return newMarkList[NewMarkType.NewMark_StampItem].Contains(index);
        }
        else if (newMarkType == NewMarkType.NewMark_NoticeItem)
        {
            return newMarkList[NewMarkType.NewMark_NoticeItem].Contains(index);
        }
        else if (newMarkType == NewMarkType.NewMark_TeacherItem)
        {
            return newMarkList[NewMarkType.NewMark_TeacherItem].Contains(index);
        }
        else if (newMarkType == NewMarkType.NewMark_ScanItItem)
        {
            return newMarkList[NewMarkType.NewMark_ScanItItem].Contains(index);
        }

        return newMarkList[newMarkType].Contains(index);
    }

    public void CheckNewStamp(int stampNo, Action close = null)
    {
        if (IsHasStamp(stampNo) == false)
        {
            PopupManager.Instance.OpenPopup<NewStampPopupController>(popup =>
            {
                popup.Bind(stampNo);
            },
            onCloseAction: close
            );
        }
        else
        {
            if (close != null)
                close();
        }
    }

    public void CheckNewStamp(Study.BigCategory bigCategory, Action close = null)
    {
        if (bigCategory.Finish == false)
            return;

        CheckNewStamp(bigCategory.rewardStampNo, close);
    }

    public void CheckAllNewStamp()
    {
        newStampList.Clear();
        foreach (var item in GameData.Study.Worlds)
        {
            if (item.Finish && IsHasStamp(item.rewardStampNo) == false)
                newStampList.Add(GameData.StampDatas[item.rewardStampNo]);
        }
        // Theme reward stamp 
        foreach (var item in GameData.Study.Themes)
        {
            if (item.Finish && IsHasStamp(item.rewardStampNo) == false)
                newStampList.Add(GameData.StampDatas[item.rewardStampNo]);
        }
        // Speaking reward stamp 
        foreach (var item in GameData.Study.Speakings)
        {
            if (item.Finish && IsHasStamp(item.rewardStampNo) == false)
                newStampList.Add(GameData.StampDatas[item.rewardStampNo]);
        }
        // LetsTalk reward stamp 
        foreach (var item in GameData.Study.LetsTalks)
        {
            if (item.Finish && IsHasStamp(item.rewardStampNo) == false)
                newStampList.Add(GameData.StampDatas[item.rewardStampNo]);
        }
        // ScanIt reward stamp 
        foreach (var item in GameData.Study.ScanIts)
        {
            if (item.Finish && IsHasStamp(item.rewardStampNo) == false)
                newStampList.Add(GameData.StampDatas[item.rewardStampNo]);
        }

        ShowNewStamp();
    }

    async void ShowNewStamp()
    {
        for(int i = 0;i< newStampList.Count;i++)
        {
            var taskCompletion = new TaskCompletionSource<bool>();
            PopupManager.Instance.OpenPopup<NewStampPopupController>(popup =>
            {
                popup.Bind(newStampList[i].no);
            },
            onCloseAction: async () =>
            {
                await Task.Delay(100);
                taskCompletion.SetResult(true);
            });

            await taskCompletion.Task;
        }
    }

    public void AddHasStamp(int idx, bool addNewMark = true)
    {
        PlayerPrefsManager.SetInt($"{hasStampKey}{idx}", 1);
        if (addNewMark)
            AddNewMark(NewMarkType.NewMark_StampItem, idx);
        else
            RemoveNewMark(NewMarkType.NewMark_StampItem, idx);

        GameData.StampDatas[idx].isHasStamp = true;
        onNewStampEvent?.Invoke(idx);
    }

    public bool IsHasStamp(int idx)
    {
        return PlayerPrefsManager.GetInt($"{hasStampKey}{idx}", 0) == 1;
    }

    private void OnApplicationQuit()
    {
        SaveAll();
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pause)
    {
        SaveAll();
        PlayerPrefs.Save();
    }

    private void SaveAll()
    {
        SaveNewMark(NewMarkType.NewMark_NoticeItem);
        SaveNewMark(NewMarkType.NewMark_TeacherItem);
        SaveNewMark(NewMarkType.NewMark_StampItem);
        SaveNewMark(NewMarkType.NewMark_ScanItItem);

        SaveChekedNewMark(NewMarkType.NewMark_NoticeItem);
        SaveChekedNewMark(NewMarkType.NewMark_TeacherItem);
        SaveChekedNewMark(NewMarkType.NewMark_StampItem);
        SaveChekedNewMark(NewMarkType.NewMark_ScanItItem);
    }

    private void SaveNewMark(NewMarkType newMarkType)
    {
        PlayerPrefsManager.SetStringArray(newMarkType.ToString(), newMarkList[newMarkType].ToArray());
    }

    private void SaveChekedNewMark(NewMarkType newMarkType)
    {
        PlayerPrefsManager.SetStringArray($"Checked_{newMarkType}", checkedNewMarkList[newMarkType].ToArray());
    }
}
