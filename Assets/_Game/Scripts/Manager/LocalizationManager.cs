using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataSave;
using UnityEngine;

public class LocalizationManager : BaseManager
{
    [field: SerializeField] public LanguageType CurrentLanguage { get; private set; }

    /// <summary>Bắn ra sau khi đổi ngôn ngữ và đã build lại bảng text.</summary>
    public event Action<LanguageType> OnLanguageChanged;

    // key -> text của ngôn ngữ hiện tại (fallback EN khi ô trống)
    private readonly Dictionary<string, string> texts = new Dictionary<string, string>();

    public override async UniTask LoadData()
    {
        CurrentLanguage = SettingDataSave.Function.GetLanguage();
        BuildTexts();
        await UniTask.CompletedTask;
    }

    public void ChangeLanguage(LanguageType language)
    {
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        SettingDataSave.Function.ChangeLanguage(language);
        BuildTexts();
        OnLanguageChanged?.Invoke(language);
    }

    private void BuildTexts()
    {
        texts.Clear();
        List<LocalizeEntry> entries = LocalizeGlobalConfig.Instance.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            LocalizeEntry entry = entries[i];
            string text = entry.GetText(CurrentLanguage);
            if (string.IsNullOrEmpty(text)) text = entry.GetText(LanguageType.EN);
            texts[entry.key] = text ?? entry.key;
        }
    }

    /// <summary>Không có key trả về chính key.</summary>
    public string GetTextByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        return texts.TryGetValue(key, out string text) ? text : key;
    }

    /// <summary>Lấy text của key theo 1 ngôn ngữ cụ thể (không phải ngôn ngữ hiện tại). Fallback EN, không có key trả về chính key.</summary>
    public string GetTextByKey(string key, LanguageType language)
    {
        List<LocalizeEntry> entries = LocalizeGlobalConfig.Instance.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].key != key) continue;
            string text = entries[i].GetText(language);
            if (string.IsNullOrEmpty(text)) text = entries[i].GetText(LanguageType.EN);
            return string.IsNullOrEmpty(text) ? key : text;
        }
        return key;
    }

    /// <summary>sample là text EN. Không tìm được key trả về chính sample.</summary>
    public string GetTextBySample(string sample)
    {
        string key = GetKeyBySample(sample);
        return string.IsNullOrEmpty(key) ? sample : GetTextByKey(key);
    }

    public string GetKeyBySample(string sample)
    {
        return LocalizeGlobalConfig.Instance.GetKeyBySample(sample);
    }
}
