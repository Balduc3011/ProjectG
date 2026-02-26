using System.Collections.Generic;
using Balduu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopPack : MonoBehaviour
{
    public string packageId;
    public IAPPackage package;
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
        package = ShopGlobalConfig.Instance.GetIAPPackage(packageId);
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
            if(i < package.gameResources.Count)
            {
                var sprite = SpriteGlobalConfig.Instance.GetResourceSprite(package.gameResources[i].Type, package.gameResources[i].DefineValue);
                var value = ShopGlobalConfig.Instance.GetResourceTextValue(package.gameResources[i]);
                itemShows[i].Init(sprite, value);
            }
            else
            {
                itemShows[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void PurchasePackage()
    {
        GameManager.RewardManager.AddReward(package.gameResources);
    }
}
