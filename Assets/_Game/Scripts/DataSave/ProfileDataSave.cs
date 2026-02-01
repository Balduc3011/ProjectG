using MemoryPack;
using UnityEngine;


namespace DataSave
{
    [System.Serializable]
    [MemoryPackable]
    public partial class ProfileDataSave
    {
        public static ProfileDataSave Instance => InGameDataManager.Instance.InGameData.ProfileDataSave;
        [MemoryPackOrder(0)]
        public string playerId;
        [MemoryPackOrder(1)]
        public string playerName;
        [MemoryPackOrder(2)]
        public int playerLevel;
        
        [MemoryPackConstructor]
        public ProfileDataSave()
        {
            playerId = "";
            playerName = "";
            playerLevel = 1;
        }
        
        public static class Function
        {
            public static string GetPlayerId()
            {
                return Instance.playerId;
            }
            public static void SetPlayerId(string id)
            {
                Instance.playerId = id;
            }
        }
    }
}

