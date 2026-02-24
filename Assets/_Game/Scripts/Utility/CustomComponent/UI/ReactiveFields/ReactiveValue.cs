using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

namespace Baseduu.ReactiveFields
{
    [Serializable]
    public class ReactiveValue<T>
    {
        [SerializeField]
        private T value;

        [NonSerialized]
        private ReactiveProperty<T> _property;

        public ReactiveValue()
        {
            EnsureProperty();
        }

        public ReactiveValue(T initialValue)
        {
            value = initialValue;
            _property = new ReactiveProperty<T>(initialValue);
        }

        public T Value
        {
            get => value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, this.value))
                {
                    this.value = value;
                    EnsureProperty();
                    _property.Value = value;
                }
            }
        }

        public ReactiveProperty<T> Property
        {
            get
            {
                EnsureProperty();
                return _property;
            }
        }

        private void EnsureProperty()
        {
            if (_property == null)
                _property = new ReactiveProperty<T>(value);
        }

        // 🔥 QUAN TRỌNG
        public void InternalSetFromInspector(T newValue)
        {
            EnsureProperty();

            if (!EqualityComparer<T>.Default.Equals(_property.Value, newValue))
            {
                value = newValue;
                _property.Value = newValue;
            }
        }
    }
}