using System.Collections.Generic;
using MemoryPack;

namespace DataSave
{
    [System.Serializable]
    [MemoryPackable]
    public partial class ShopDataSave
    {
        public static ShopDataSave Instance => InGameDataManager.Instance.InGameData.ShopDataSave;
        [MemoryPackOrder(0)]
        public List<string> purchasedPackIds;
        
        public static class Function
        {
            private static ShopDataSave ShopDataSave => InGameDataManager.Instance.InGameData.ShopDataSave;
            
            public static List<string> GetPurchasedPackIds()
            {
                return ShopDataSave.purchasedPackIds ?? (ShopDataSave.purchasedPackIds = new List<string>());
            }
            
            public static void AddPurchasedPackId(string packId)
            {
                ShopDataSave.purchasedPackIds ??= new List<string>();
                if (!ShopDataSave.purchasedPackIds.Contains(packId))
                {
                    ShopDataSave.purchasedPackIds.Add(packId);
                }
            }
        }
    }
}

