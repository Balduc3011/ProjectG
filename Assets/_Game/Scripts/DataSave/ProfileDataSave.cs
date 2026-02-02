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
            private static ProfileDataSave ProfileDataSave => InGameDataManager.Instance.InGameData.ProfileDataSave;
            public static string GetPlayerId()
            {
                return ProfileDataSave.playerId;
            }
            public static void SetPlayerId(string id)
            {
                ProfileDataSave.playerId = id;
                InGameDataManager.Instance.SaveData();
            }
            public static string GetPlayerName()
            {
                return ProfileDataSave.playerName;
            }

            public static void SetPlayerName(string name)
            {
                ProfileDataSave.playerName = name;
                InGameDataManager.Instance.SaveData();
            }
            
            public static int GetPlayerLevel()
            {
                return ProfileDataSave.playerLevel;
            }
            public static void SetPlayerLevel(int level)
            {
                ProfileDataSave.playerLevel = level;
                InGameDataManager.Instance.SaveData();
            }
        }
    }
}

