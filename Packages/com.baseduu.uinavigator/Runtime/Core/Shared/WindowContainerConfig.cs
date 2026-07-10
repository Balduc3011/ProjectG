using System;
using UnityEngine.Serialization;
using Baseduu.UINavigator.Foundation;

namespace Baseduu.UINavigator.Core
{
    [Serializable]
    public class WindowContainerConfig
    {
        public string name;

        [FormerlySerializedAs("layerType")]
        public WindowContainerType containerType;

        public bool overrideSorting;

        [ShowIf(nameof(overrideSorting))]
        public SortingLayerId sortingLayer;

        [ShowIf(nameof(overrideSorting))]
        public int orderInLayer = 0;
    }
}
