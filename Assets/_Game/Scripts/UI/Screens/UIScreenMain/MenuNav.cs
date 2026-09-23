using System;
using System.Collections.Generic;
using Balduu;
using Sirenix.OdinInspector;
using UnityEngine;

public class MenuNav : MonoBehaviour
{
    public static Action<int> SetSelectedTabEvent {get ; set; }
    public RectTransform layout;
    public int selectedTabIndex = 2;
    public float selectedTabWidthMul = 1.25f;
    public List<NavTabBtn> tabBtns = new List<NavTabBtn>();

    private void Start()
    {
        SetSelectedTabEvent += SetSelectedTab;
    }

    private void OnDestroy()
    {
        SetSelectedTabEvent -= SetSelectedTab;
    }
    
    [Button]
    public void PreSetup()
    {
        var width = layout.rect.width;
        var tabWidth = width / (tabBtns.Count + selectedTabWidthMul - 1);
        for (var i = 0; i < tabBtns.Count; i++)
        {
            int index = i;
            tabBtns[i].tabIndex = index;
            tabBtns[i].SetCachedWidth(tabWidth, selectedTabWidthMul);
            tabBtns[i].SetFirstSelected(i == selectedTabIndex);
        }
    }

    private void SetSelectedTab(int tabIndex)
    {
        selectedTabIndex = tabIndex;
        for (var i = 0; i < tabBtns.Count; i++)
        {
            tabBtns[i].SetSelected(i == selectedTabIndex);
        }
    }
    
    public void LeftSwipe()
    {
        selectedTabIndex += selectedTabIndex < tabBtns.Count - 1 ? 1 : 0;
        SetSelectedTab(selectedTabIndex);
        UIScreenMainContext.Events.MoveSwipeAreaEvent?.Invoke(selectedTabIndex, false);
    }
    public void RightSwipe()
    {
        selectedTabIndex -= selectedTabIndex > 0 ? 1 : 0;
        SetSelectedTab(selectedTabIndex);
        UIScreenMainContext.Events.MoveSwipeAreaEvent?.Invoke(selectedTabIndex, false);
    }
}
