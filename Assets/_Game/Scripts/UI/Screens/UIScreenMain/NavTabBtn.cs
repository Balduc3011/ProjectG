using System;
using Balduu;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NavTabBtn : MonoBehaviour
{
    public int tabIndex;
    public RectTransform tabRect;
    public Button tabBtn;
    public TextMeshProUGUI tabTitle;
    public CanvasGroup tabTitleCg;
    public Transform tabIconTrs;
    public Transform tabIconShow;
    public Transform tabIconHide;
    private float cachedWidth;
    private float cachedHeight;
    private float cachedWidthMul;
    private float iconMul = 1.5f;
    private float transtionTime = 0.1f;
    private MotionHandle tabRectHandle;
    private MotionHandle tabIconScaleHandle;
    private MotionHandle tabIconPosHandle;
    private MotionHandle tabTileCgHandle;

    private void Start()
    {
        tabBtn.AddListenerUI(ChangeSelectedTab, false);
    }

    private void ChangeSelectedTab()
    {
        MenuNav.SetSelectedTabEvent?.Invoke(tabIndex);
        UIScreenMain.Events.MoveSwipeAreaEvent?.Invoke(tabIndex);
    }

    public void SetCachedWidth(float width, float mul = 1f)
    {
        cachedWidth = width;
        cachedHeight = tabRect.rect.height;
        cachedWidthMul = mul;
    }

    public void SetFirstSelected(bool selected)
    {
        tabRect.sizeDelta = selected ? new Vector2(cachedWidth * cachedWidthMul, cachedHeight) : new Vector2(cachedWidth, cachedHeight);
        tabIconTrs.localScale = selected ? Vector3.one * iconMul : Vector3.one;
        tabIconTrs.localPosition = selected ? tabIconShow.localPosition : tabIconHide.localPosition;
        tabTitleCg.alpha = selected ? 1f : 0f;
    }
    
    public void SetSelected(bool selected)
    {
        CancelHandle();
        var rectWidth = selected ? cachedWidth * cachedWidthMul : cachedWidth;
        tabRectHandle = LMotion.Create(tabRect.rect.width, rectWidth, transtionTime).Bind(ChangeTabRectWidth);
        var iconScale = selected ? iconMul : 1f;
        tabIconScaleHandle = LMotion.Create(tabIconTrs.localScale.x, iconScale, transtionTime).Bind(ChangeTabIconScale);
        var iconPos = selected ? tabIconShow.localPosition : tabIconHide.localPosition;
        tabIconPosHandle = LMotion.Create(tabIconTrs.localPosition, iconPos, transtionTime).Bind(ChangeTabIconPosition);
        var titleAlpha = selected ? 1f : 0f;
        tabTileCgHandle = LMotion.Create(tabTitleCg.alpha, titleAlpha, transtionTime).Bind(ChangeTabTitleAlpha);
    }
    
    private void CancelHandle() {
    {
        if(tabRectHandle.IsPlaying())
            tabRectHandle.Cancel();
        if(tabIconScaleHandle.IsPlaying())
            tabIconScaleHandle.Cancel();
        if(tabIconPosHandle.IsPlaying())
            tabIconPosHandle.Cancel();
        if(tabTileCgHandle.IsPlaying())
            tabTileCgHandle.Cancel();
    }}
    
    void ChangeTabRectWidth(float width)
    {
        tabRect.sizeDelta = new Vector2(width, cachedHeight);
    }
    void ChangeTabIconScale(float val)
    {
        tabIconTrs.localScale = Vector3.one * val;
    }
    void ChangeTabIconPosition(Vector3 pos)
    {
        tabIconTrs.localPosition = pos;
    }
    void ChangeTabTitleAlpha(float alpha)
    {
        tabTitleCg.alpha = alpha;
    }
}
