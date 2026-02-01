using System;
using System.Collections;
using System.Collections.Generic;
using Balduu.Utility;
using Balduu.Utility.DesignPattern;
using CodeStage.AntiCheat.Storage;
using DataSave;
using MemoryPack;
using Sirenix.OdinInspector;
using UnityEngine;

public class InGameDataManager : DDOLSingleton<InGameDataManager>
{
    [field: SerializeField] public InGameData InGameData { get; private set; } = new();
    [field: SerializeField] public int InstallVersion { get; set; }
    [field: SerializeField] public int PlayingVersion { get; set; }
    private bool pendingSave = false;
    private bool saveScheduled = false;
    
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }
    
    [Button]
    public void SaveData()
    {
        pendingSave = true;
        if (saveScheduled) return;
        saveScheduled = true;
        StartCoroutine(SaveDataEndOfFrame());
    }
    
    private IEnumerator SaveDataEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        if (pendingSave)
        {
            UserDataSaveManager.SaveAllData(InGameData);
            DebugAds.Log("Save data successfully");
            pendingSave = false;
        }
        saveScheduled = false;
    }
    
    [Button]
    public void LoadData()
    {
        LoadOrMigrateData();
        DebugAds.Log("Load data successfully");
    }
    
    private void LoadOrMigrateData()
    {
        try
        {
            InGameData = UserDataSaveManager.LoadAllData();
        }
        catch (Exception e)
        {
            DebugAds.Log($"[InGameData] Error loading data: {e.GetType().Name} - {e.Message}");
            Debug.LogException(e);
        }
    }
    
    [Button]
    public void ResetData()
    {
        InGameData = new InGameData();
        SaveData();
    }
}
