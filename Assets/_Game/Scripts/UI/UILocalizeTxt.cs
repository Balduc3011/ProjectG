using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class UILocalizeTxt : MonoBehaviour
{
    public TextMeshProUGUI txt;
    public string key;
    [FormerlySerializedAs("additionalText")]
    public string subString;
    [FormerlySerializedAs("additionalTextType")]
    public SubStringType subStringType;

    private LocalizationManager localizationManager;

#if UNITY_EDITOR
    // Gọi khi Add Component: lấy text EN hiện tại của TMP để tìm key
    private void Reset()
    {
        txt = GetComponent<TextMeshProUGUI>();
        FindKeyBySample();
    }

    [Sirenix.OdinInspector.Button]
    private void FindKeyBySample()
    {
        if (txt == null) return;
        // Edit mode chưa có GameManager nên tra thẳng config
        string foundKey = LocalizeGlobalConfig.Instance.GetKeyBySample(txt.text);
        if (string.IsNullOrEmpty(foundKey))
        {
            Debug.LogWarning($"[UILocalizeTxt] Không tìm thấy key cho text '{txt.text}'", this);
            return;
        }
        key = foundKey;
        if (txt == null) txt = GetComponent<TextMeshProUGUI>();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Start()
    {
        if (txt == null) txt = GetComponent<TextMeshProUGUI>();
        if (!GameManager.TryGet(out localizationManager)) return;
        localizationManager.OnLanguageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (localizationManager == null) return;
        localizationManager.OnLanguageChanged -= OnLanguageChanged;
        localizationManager = null;
    }

    private void OnLanguageChanged(LanguageType language)
    {
        Refresh();
    }

    public void SetKey(string newKey)
    {
        key = newKey;
        Refresh();
    }

    public void SetSubString(string newSubString)
    {
        subString = newSubString;
        Refresh();
    }

    public void Refresh()
    {
        if (txt == null || localizationManager == null) return;
        string text = localizationManager.GetTextByKey(key);
        if (!string.IsNullOrEmpty(subString))
            text = subStringType == SubStringType.Before ? subString + text : text + subString;
        txt.text = text;
    }
}

public enum SubStringType
{
    After = 0,
    Before = 1,
}
