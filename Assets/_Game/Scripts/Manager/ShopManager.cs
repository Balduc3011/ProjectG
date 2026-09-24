using Balduu.Utility;

public class ShopManager : BaseManager
{
    private RewardManager rewardManager;

    protected internal override void Initialize(ManagerCollection<BaseManager> managers)
    {
        // Chỉ dùng lúc runtime (bấm mua), không cần RewardManager load trước.
        rewardManager = managers.Get<RewardManager>();
    }

    public void PurchasePackage(string packageId)
    {
        ShopPackage package = ShopGlobalConfig.Instance.GetShopPackage(packageId);
        if (package == null)
        {
            DebugAds.LogWarning($"ShopManager: không tìm thấy package {packageId}");
            return;
        }

        // TODO: kiểm tra điều kiện mua + thanh toán (currency / IAP) trước khi trao thưởng.
        rewardManager.AddReward(package.gameResources);
    }
}
