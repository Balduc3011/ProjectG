using Balduu;
using Baseduu.ReactiveFields;
using R3;
using TMPro;
using UnityEngine;

public class SettingSwitch : MonoBehaviour
{
    public SettingType settingType;
    public ReactiveValue<bool> value;
    public TextMeshProUGUI titleTxt;
    public UISwitch uiSwitch;
    
    public void Init(SettingType type, ReactiveValue<bool> val)
    {
        settingType = type;
        titleTxt.text = GameManager.LocalizationManager.GetTextBySample(settingType.ToString());
        value = val;
        uiSwitch.Init(value.Value);
        uiSwitch.onValueChange = onValueChange;
    }

    private void onValueChange(bool newValue)
    {
        value.Value = newValue;
    }
}
