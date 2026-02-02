using System.Collections.Generic;
using DataSave;

[System.Serializable]
public class IAPPackage
{
    public string packageID;
    public ConsumeType consumeType;
    public string title;
    public string description;
    public GameResource price;
    public List<GameResource> gameResources;
}

public enum ConsumeType
{
    Consumable,
    NonConsumable,
}
