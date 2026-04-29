using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuickAccess
{
    /// <summary>
    /// Named group of bookmarks. Built-in sections cannot be deleted from the window.
    /// </summary>
    [Serializable]
    public class QuickAccessSection
    {
        [SerializeField] string _sectionName;
        [SerializeField] bool _isBuiltIn;
        [SerializeField] bool _itemsExpanded = true;
        [SerializeField] List<QuickAccessItem> _items = new List<QuickAccessItem>();

        public string SectionName
        {
            get => _sectionName;
            set => _sectionName = value;
        }

        public bool IsBuiltIn => _isBuiltIn;

        /// <summary>When false, section body (drop zone + item rows) is hidden in Quick Access window.</summary>
        public bool ItemsExpanded
        {
            get => _itemsExpanded;
            set => _itemsExpanded = value;
        }

        public List<QuickAccessItem> Items => _items;

        /// <summary>
        /// Keeps serialized lists safe after manual YAML edits or version upgrades.
        /// </summary>
        internal void EnsureListsNotNull()
        {
            if (_items == null)
            {
                _items = new List<QuickAccessItem>();
            }
        }

        public QuickAccessSection()
        {
        }

        public QuickAccessSection(string sectionName, bool isBuiltIn)
        {
            _sectionName = sectionName;
            _isBuiltIn = isBuiltIn;
        }
    }
}
