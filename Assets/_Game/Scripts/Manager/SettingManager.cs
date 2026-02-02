using Baseduu.ReactiveFields;
using Cysharp.Threading.Tasks;
using DataSave;
using R3;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [field: SerializeField] public int SampleValue { get; set; }
    [field: SerializeField] public ReactiveValue<bool> MusicStatus { get; set; }
    [field: SerializeField] public ReactiveValue<bool> SoundStatus { get; set; }
    [field: SerializeField] public ReactiveValue<bool> HapticStatus { get; set; }

    public void LoadData()
    {
        MusicStatus = new ReactiveValue<bool>(SettingDataSave.Function.GetSettingData(SettingType.Music).isOn);
        MusicStatus.Property.Subscribe((v) => { OnChangeSettingState(SettingType.Music, v); }).AddTo(gameObject.GetCancellationTokenOnDestroy());
        SoundStatus = new ReactiveValue<bool>(SettingDataSave.Function.GetSettingData(SettingType.Sound).isOn);
        SoundStatus.Property.Subscribe((v) => { OnChangeSettingState(SettingType.Sound, v); }).AddTo(gameObject.GetCancellationTokenOnDestroy());
        HapticStatus = new ReactiveValue<bool>(SettingDataSave.Function.GetSettingData(SettingType.Haptic).isOn);
        HapticStatus.Property.Subscribe((v) => { OnChangeSettingState(SettingType.Haptic, v); }).AddTo(gameObject.GetCancellationTokenOnDestroy());
    }
    
    private void OnChangeSettingState(SettingType type, bool isOn)
    {
        SettingDataSave.Function.ChangeSettingData(type, isOn);
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