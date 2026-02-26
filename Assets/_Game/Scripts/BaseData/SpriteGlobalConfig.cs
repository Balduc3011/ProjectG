using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteGlobalConfig", menuName = "GlobalConfigs/SpriteGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class SpriteGlobalConfig : GlobalConfig<SpriteGlobalConfig>
{
    public List<CurrencySprites> currencySprites = new List<CurrencySprites>();
    public List<BoosterSprites> boosterSprites = new List<BoosterSprites>();
    public List<SpecialSprites> specialSprites = new List<SpecialSprites>();
    
    public Sprite GetResourceSprite(ResourceType type, int defineValue)
    {
        return type switch
        {
            ResourceType.Currency => GetCurrencySprite((CurrencyType)defineValue),
            ResourceType.Booster => GetBoosterSprite((BoosterType)defineValue),
            ResourceType.Special => GetSpecialSprite((SpecialType)defineValue),
            _ => null
        };
    }
    
    public Sprite GetCurrencySprite(CurrencyType type)
    {
        for (var i = 0; i < currencySprites.Count; i++)
        {
            if (currencySprites[i].type == type)
                return currencySprites[i].sprite;
        }
        return null;
    }
    public Sprite GetBoosterSprite(BoosterType type)
    {
        for (var i = 0; i < boosterSprites.Count; i++)
        {
            if (boosterSprites[i].type == type)
                return boosterSprites[i].sprite;
        }
        return null;
    }

    public Sprite GetSpecialSprite(SpecialType type)
    {
        for (var i = 0; i < specialSprites.Count; i++)
        {
            if (specialSprites[i].type == type)
                return specialSprites[i].sprite;
        }
        return null;
    }
}

[System.Serializable]
public class CurrencySprites
{
    public CurrencyType type;
    [PreviewField] public Sprite sprite;
}

[System.Serializable]
public class BoosterSprites
{
    public BoosterType type;
    [PreviewField] public Sprite sprite;
}

[System.Serializable]
public class SpecialSprites
{
    public SpecialType type;
    [PreviewField] public Sprite sprite;
}
