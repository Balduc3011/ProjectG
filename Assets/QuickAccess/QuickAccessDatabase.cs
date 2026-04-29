using System.Collections.Generic;
using UnityEngine;

namespace QuickAccess
{
    /// <summary>
    /// Persistent store for Quick Access sections and items (ScriptableObject asset).
    /// </summary>
    public class QuickAccessDatabase : ScriptableObject
    {
        public const string DefaultSceneSection = "Scene";
        public const string DefaultPrefabSection = "Prefab";
        public const string DefaultSoSection = "SO";
        public const string ResourceFolder = "Assets/QuickAccess";
        public const string AssetFileName = "QuickAccessDatabase.asset";
        public static readonly string AssetPath = $"{ResourceFolder}/{AssetFileName}";

        [SerializeField] List<QuickAccessSection> _sections = new List<QuickAccessSection>();

        public IReadOnlyList<QuickAccessSection> Sections => _sections;

        public List<QuickAccessSection> SectionsMutable => _sections;

        /// <summary>
        /// Called for brand-new assets or empty lists: creates Scene / Prefab / SO sections.
        /// </summary>
        public void InitializeDefaultSectionsIfEmpty()
        {
            if (_sections == null)
            {
                _sections = new List<QuickAccessSection>();
            }

            if (_sections.Count != 0)
            {
                return;
            }

            _sections.Add(new QuickAccessSection(DefaultSceneSection, true));
            _sections.Add(new QuickAccessSection(DefaultPrefabSection, true));
            _sections.Add(new QuickAccessSection(DefaultSoSection, true));
        }

        void OnValidate()
        {
            if (_sections == null)
            {
                _sections = new List<QuickAccessSection>();
                return;
            }

            for (var i = 0; i < _sections.Count; i++)
            {
                _sections[i]?.EnsureListsNotNull();
            }
        }
    }
}
