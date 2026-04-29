using System;
using UnityEngine;

namespace QuickAccess
{
    /// <summary>
    /// Single bookmark entry: display name + project object reference.
    /// </summary>
    [Serializable]
    public class QuickAccessItem
    {
        [SerializeField] string _name;
        [SerializeField] UnityEngine.Object _target;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public UnityEngine.Object Target
        {
            get => _target;
            set => _target = value;
        }

        public QuickAccessItem()
        {
        }

        public QuickAccessItem(string displayName, UnityEngine.Object asset)
        {
            _name = displayName;
            _target = asset;
        }
    }
}
