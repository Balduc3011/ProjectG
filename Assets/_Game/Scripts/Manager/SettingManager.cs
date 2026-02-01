using DataSave;
using R3;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [field: SerializeField] public ReactiveProperty<bool> MusicStatus { get; set; }
    [field: SerializeField] public ReactiveProperty<bool> SoundStatus { get; set; }
    [field: SerializeField] public ReactiveProperty<bool> HapticStatus { get; set; }

    public void LoadData()
    {
        MusicStatus = new ReactiveProperty<bool>(SettingDataSave.Function.GetSettingData(SettingType.Music).isOn);
        SoundStatus = new ReactiveProperty<bool>(SettingDataSave.Function.GetSettingData(SettingType.Sound).isOn);
        HapticStatus = new ReactiveProperty<bool>(SettingDataSave.Function.GetSettingData(SettingType.Haptic).isOn);
    }
}


public enum SettingType
{
    None = 0,
    Music = 1,
    Sound = 2,
    Haptic = 3,
    Effect = 4,
}