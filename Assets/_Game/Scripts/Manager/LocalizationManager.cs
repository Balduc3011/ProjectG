using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public string GetTextByKey(string key)
    {
        return key;
    }
    public string GetTextBySample(string sample)
    {
        return sample;
    }
    
    public string GetKeyBySample(string sample)
    {
        return "";
    }
}
