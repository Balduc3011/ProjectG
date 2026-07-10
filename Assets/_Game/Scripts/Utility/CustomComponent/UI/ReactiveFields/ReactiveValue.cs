using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Baseduu.ReactiveFields
{
    [MemoryPackable]
    [Serializable]
    public partial class ReactiveValue<T>
    {
#if UNITY_EDITOR
        public class ReactiveStructValueDrawer : OdinValueDrawer<ReactiveValue<T>>
        {
            protected override void DrawPropertyLayout(GUIContent label)
            {
                ValueEntry.Property.Children.First().Draw(label);
            }
        }
#endif
        [OnValueChanged(nameof(OnValueChange))]
        [Delayed]
        [SerializeField] private T value;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                _property.Value = value;
            }
        }

        [MemoryPackIgnore] public ReactiveProperty<T> _property;
        [MemoryPackIgnore] public ReactiveProperty<T> Property => InitializeOrReSync();

        public ReactiveValue()
        {
            _property = new ReactiveProperty<T>(value);
        }

        [MemoryPackConstructor]
        public ReactiveValue(T value)
        {
            this.value = value;
            _property = new ReactiveProperty<T>(value);
            Value = value;
        }

        public static implicit operator T(ReactiveValue<T> reactiveValue)
        {
            return reactiveValue.Value;
        }

        private void OnValueChange()
        {
#if UNITY_EDITOR
            _property.Value = value;
#endif
        }

        private ReactiveProperty<T> InitializeOrReSync()
        {
            if (_property != null && EqualityComparer<T>.Default.Equals(_property.Value, Value)) return _property;
            _property = new ReactiveProperty<T>(value);
            return _property;
        }
    }
}
