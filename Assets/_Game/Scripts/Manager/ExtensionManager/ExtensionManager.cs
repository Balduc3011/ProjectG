using Balduu.Utility.DesignPattern;
using UnityEngine;

public class ExtensionManager : Singleton<ExtensionManager>
{
    public AdsController adsController;
    public static AdsController AdsController => Instance.adsController;
    public AnalyticController analyticController;
    public static AnalyticController AnalyticController => Instance.analyticController;
    public APIController apiController;
    public static APIController APIController => Instance.apiController;
}
