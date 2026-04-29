#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace QuickAccess.Editor
{
    /// <summary>
    /// Loads or creates the database asset under Assets/QuickAccess/.
    /// </summary>
    internal static class QuickAccessDatabaseUtility
    {
        internal static QuickAccessDatabase GetOrCreate()
        {
            var db = AssetDatabase.LoadAssetAtPath<QuickAccessDatabase>(QuickAccessDatabase.AssetPath);
            if (db != null)
            {
                db.InitializeDefaultSectionsIfEmpty();
                return db;
            }

            if (!AssetDatabase.IsValidFolder(QuickAccessDatabase.ResourceFolder))
            {
                AssetDatabase.CreateFolder("Assets", "QuickAccess");
            }

            db = ScriptableObject.CreateInstance<QuickAccessDatabase>();
            db.InitializeDefaultSectionsIfEmpty();
            AssetDatabase.CreateAsset(db, QuickAccessDatabase.AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return db;
        }
    }
}
#endif
