using System;
using System.Collections.Generic;
using Balduu;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizeGlobalConfig", menuName = "GlobalConfigs/LocalizeGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class LocalizeGlobalConfig : GlobalConfig<LocalizeGlobalConfig>
{
    private const string TabName = "Localize";
    private const string KeyColumn = "Key";

    [Searchable]
    public List<LocalizeEntry> entries = new List<LocalizeEntry>();

    [Button]
    private async void FetchData()
    {
        try
        {
            List<Dictionary<string, string>> rows = await GoogleSheetHelper.FetchTableAsync(DefaultGlobalConfig.linkSheetID, TabName);
            List<LocalizeEntry> newEntries = new List<LocalizeEntry>();

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                if (!row.TryGetValue(KeyColumn, out string key)) continue;
                key = key.Trim();
                if (string.IsNullOrEmpty(key)) continue;

                LocalizeEntry entry = new LocalizeEntry { key = key, texts = new List<LocalizeText>() };
                // Mỗi cột (trừ Key) có tên trùng LanguageType (không phân biệt hoa thường: Ms, Fil...)
                foreach (KeyValuePair<string, string> cell in row)
                {
                    if (cell.Key == KeyColumn) continue;
                    if (!Enum.TryParse(cell.Key, true, out LanguageType language))
                    {
                        Debug.LogWarning($"[LocalizeGlobalConfig] Unknown language column '{cell.Key}'");
                        continue;
                    }
                    entry.texts.Add(new LocalizeText { language = language, text = cell.Value.Trim() });
                }
                newEntries.Add(entry);
            }

            entries = newEntries;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[LocalizeGlobalConfig] Fetched {entries.Count} keys");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalizeGlobalConfig] FetchData failed: {e}");
        }
    }

    /// <summary>Tìm key có text EN trùng với sample (bỏ khoảng trắng 2 đầu, không phân biệt hoa thường). Không thấy trả về "".</summary>
    public string GetKeyBySample(string sample)
    {
        if (string.IsNullOrWhiteSpace(sample)) return "";
        sample = sample.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            string text = entries[i].GetText(LanguageType.EN);
            if (text != null && string.Equals(text.Trim(), sample, StringComparison.OrdinalIgnoreCase))
                return entries[i].key;
        }
        return "";
    }
}

[Serializable]
public class LocalizeEntry
{
    public string key;
    [TableList] public List<LocalizeText> texts = new List<LocalizeText>();

    public string GetText(LanguageType language)
    {
        for (int i = 0; i < texts.Count; i++)
        {
            if (texts[i].language == language)
                return texts[i].text;
        }
        return null;
    }
}

[Serializable]
public class LocalizeText
{
    public LanguageType language;
    public string text;
}

/// <summary>Tên enum phải trùng tên cột trên sheet Localize (không phân biệt hoa thường).</summary>
public enum LanguageType
{
    EN = 0,
    VI = 1,
    FR = 2,
    DE = 3,
    ES = 4,
    IT = 5,
    PT = 6,
    JA = 7,
    KO = 8,
    ID = 9,
    MS = 10,
    FIL = 11,
    TH = 12,
    ZH = 13,
}
