using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMarkController : MonoBehaviour
{
    [SerializeField] private NewMarkManager.NewMarkType NewMarkType;
    [SerializeField] private GameObject NewMark;

    private string index = "";
    private bool isAddEvent = false;

    public void SetNewMark(NewMarkManager.NewMarkType newMarkType)
    {
        NewMarkType = newMarkType;
        UpdateNewMark();
    }

    public void SetNewMark(NewMarkManager.NewMarkType newMarkType, int idx, NotiTeacherType notiTeacherType = NotiTeacherType.None)
    {
        SetNewMark(newMarkType, idx.ToString(), notiTeacherType);
    }

    public void SetNewMark(NewMarkManager.NewMarkType newMarkType, string idx, NotiTeacherType notiTeacherType = NotiTeacherType.None)
    {
        if (newMarkType == NewMarkManager.NewMarkType.NewMark_TeacherItem)
            this.index = $"{idx}-{notiTeacherType}";
        else
            this.index = idx;

        SetNewMark(newMarkType);
    }

    public void Clear()
    {
        index = "";
        NewMarkType = NewMarkManager.NewMarkType.None;
        NewMark.SetActive(false);
    }

    public string GetIndex()
    {
        return index;
    }

    public void RemoveNewMark()
    {
        NewMarkManager.Instance.RemoveNewMark(NewMarkType, index);
    }

    private void OnNewMark(NewMarkManager.NewMarkType newMarkType, bool isActive, string idx = default(string), NotiTeacherType notiTeacherType = NotiTeacherType.None)
    {
        if (newMarkType != NewMarkType)
            return;

        if(NewMarkType == NewMarkManager.NewMarkType.NewMark_NoticeItem || NewMarkType == NewMarkManager.NewMarkType.NewMark_TeacherItem
            || NewMarkType == NewMarkManager.NewMarkType.NewMark_StampItem || NewMarkType == NewMarkManager.NewMarkType.NewMark_ScanItItem)
        {
            if (index != idx)
                return;
        }

        NewMark.SetActive(isActive);
    }

    private void OnEnable()
    {
        if(isAddEvent == false)
        {
            isAddEvent = true;
            NewMarkManager.Instance.onNewMarkEvent += OnNewMark;
        }
    }

    private void UpdateNewMark()
    {
        if (NewMarkType == NewMarkManager.NewMarkType.NewMark_NoticeItem || NewMarkType == NewMarkManager.NewMarkType.NewMark_TeacherItem
            || NewMarkType == NewMarkManager.NewMarkType.NewMark_StampItem || NewMarkType == NewMarkManager.NewMarkType.NewMark_ScanItItem)
            NewMark.SetActive(NewMarkManager.Instance.IsNewMark(NewMarkType, index));
        else
            NewMark.SetActive(NewMarkManager.Instance.IsNewMark(NewMarkType));
    }

    private void OnDisable()
    {
        //if (isAddEvent)
        //{
        //    isAddEvent = false;
        //    NewMarkManager.Instance.onNewMarkEvent -= OnNewMark;
        //}
    }

    private void OnDestroy()
    {
        if (isAddEvent)
        {
            isAddEvent = false;
            NewMarkManager.Instance.onNewMarkEvent -= OnNewMark;
        }
    }
}
