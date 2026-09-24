using System.Collections.Generic;
using Balduu;
using Balduu.Utility;
using DataSave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopPack : MonoBehaviour
{
    public string packageId;
    public ShopPackage package;
    public TextMeshProUGUI titleTxt;
    public TextMeshProUGUI priceTxt;
    public Button purchaseBtn;
    public List<UIItemShow> itemShows;

    private void Start()
    {
        purchaseBtn.AddListenerUI(PurchasePackage);
    }

    public void Init()
    {
        package = ShopGlobalConfig.Instance.GetShopPackage(packageId);
        if (!IsPackageAvailable())
        {
            gameObject.SetActive(false);
        }
        else
        {
            InitUI();
        }
    }
    
    private bool IsPackageAvailable()
    {
        if (package == null)
        {
            return false;
        }
        return true;
    }

    private void InitUI()
    {
        titleTxt?.ChangeText(package.title);
        priceTxt.ChangeText(ShopGlobalConfig.Instance.GetResourceTextValue(package.price));

        for (var i = 0; i < itemShows.Count; i++)
        {
            itemShows[i].gameObject.SetActive(false);
        }

        for (var i = 0; i < package.gameResources.Count; i++)
        {
            var resource = package.gameResources[i];
            var itemShow = GetItemShow(resource);
            if (itemShow == null)
            {
                DebugAds.LogWarning($"UIShopPack {packageId}: không có UIItemShow cho {resource.Type}/{resource.DefineValue}");
                continue;
            }
            itemShow.gameObject.SetActive(true);
            var sprite = SpriteGlobalConfig.Instance.GetResourceSprite(resource.Type, resource.DefineValue);
            var value = ShopGlobalConfig.Instance.GetResourceTextValue(resource);
            itemShow.Init(sprite, value);
        }
    }

    private UIItemShow GetItemShow(GameResource resource)
    {
        for (var i = 0; i < itemShows.Count; i++)
        {
            if (itemShows[i].Matches(resource))
                return itemShows[i];
        }
        return null;
    }
    
    private void PurchasePackage()
    {
        GameManager.Get<ShopManager>().PurchasePackage(packageId);
    }
}
