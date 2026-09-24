using System;
using TMPro;
using UnityEngine;

public class UILocalizeTxt : MonoBehaviour
{
    public TextMeshProUGUI txt;
    public string key;
    public string additionalText;
    public AdditionalTextType additionalTextType;

    private void OnEnable()
    {
        Init();
    }
    
    private void Reset()
    {
        Init();
    }

    private void Init()
    {
        txt ??= GetComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(key))
        {
            key = txt != null ? GameManager.Get<LocalizationManager>().GetKeyBySample(txt.text) : "";
        }
    }
}

public enum AdditionalTextType
{
    Right,
    Left
}
