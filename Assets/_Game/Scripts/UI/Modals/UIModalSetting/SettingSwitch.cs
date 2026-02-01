using Balduu;
using R3;
using TMPro;
using UnityEngine;

public class SettingSwitch : MonoBehaviour
{
    public SettingType settingType;
    public ReactiveProperty<bool> value;
    public TextMeshProUGUI titleTxt;
    public UISwitch uiSwitch;
    
    public void Init(SettingType type, ReactiveProperty<bool> val)
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
