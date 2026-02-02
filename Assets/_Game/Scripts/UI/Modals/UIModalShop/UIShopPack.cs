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
        
    }
    
    private void PurchasePackage()
    {
        
    }
}
