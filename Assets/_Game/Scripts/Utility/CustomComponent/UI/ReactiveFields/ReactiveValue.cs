using System;
using UnityEngine;
using R3;

namespace Baseduu.ReactiveFields
{
    [Serializable]
    public class ReactiveValue<T> : ISerializationCallbackReceiver
    {
        [SerializeField] private T _serializedValue;
        [NonSerialized] private ReactiveProperty<T> _property;
        
        public ReactiveValue(T value)
        {
            _serializedValue = value;
        }

        public ReactiveProperty<T> Property
        {
            get
            {
                if (_property == null)
                    _property = new ReactiveProperty<T>(_serializedValue);
                return _property;
            }
        }

        public T Value
        {
            get => _property != null ? _property.Value : _serializedValue;
            set
            {
                if (_property != null)
                    _property.Value = value;
                _serializedValue = value;
            }
        }

        public void OnBeforeSerialize()
        {
            if (_property != null)
                _serializedValue = _property.Value;
        }

        public void OnAfterDeserialize()
        {
            _property = new ReactiveProperty<T>(_serializedValue);
        }

        public override string ToString()
        {
            return (object)Value != null ? Value.ToString() : "(null)";
        }
    }
}
