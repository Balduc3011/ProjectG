#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace QuickAccess.Editor
{
    /// <summary>
    /// Classifies project assets for optional UX (labels / future smart routing).
    /// </summary>
    internal static class QuickAccessTypeDetector
    {
        internal enum AssetKind
        {
            Unknown = 0,
            Scene = 1,
            Prefab = 2,
            ScriptableObject = 3
        }

        internal static AssetKind Classify(Object obj)
        {
            if (obj == null)
            {
                return AssetKind.Unknown;
            }

            if (obj is SceneAsset)
            {
                return AssetKind.Scene;
            }

            if (obj is GameObject go)
            {
                if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    return AssetKind.Prefab;
                }

                return AssetKind.Unknown;
            }

            if (obj is ScriptableObject)
            {
                return AssetKind.ScriptableObject;
            }

            return AssetKind.Unknown;
        }

        internal static string KindLabel(AssetKind kind)
        {
            switch (kind)
            {
                case AssetKind.Scene:
                    return "Scene";
                case AssetKind.Prefab:
                    return "Prefab";
                case AssetKind.ScriptableObject:
                    return "SO";
                default:
                    return string.Empty;
            }
        }
    }
}
#endif
