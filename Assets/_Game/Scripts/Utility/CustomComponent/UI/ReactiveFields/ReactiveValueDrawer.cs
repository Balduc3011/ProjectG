#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using Baseduu.ReactiveFields;

[CustomPropertyDrawer(typeof(ReactiveValue<>), true)]
public class ReactiveValueDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("value");

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(position, valueProp, label, true);

        if (EditorGUI.EndChangeCheck())
        {
            property.serializedObject.ApplyModifiedProperties();

            object reactiveInstance = GetTargetObject(property);
            if (reactiveInstance != null)
            {
                var method = reactiveInstance.GetType()
                    .GetMethod("InternalSetFromInspector");

                object newValue = GetSerializedValue(valueProp);

                method?.Invoke(reactiveInstance, new object[] { newValue });
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("value");
        return EditorGUI.GetPropertyHeight(valueProp, label, true);
    }

    private object GetTargetObject(SerializedProperty property)
    {
        object obj = property.serializedObject.targetObject;
        var path = property.propertyPath.Replace(".Array.data[", "[");
        var elements = path.Split('.');

        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = Convert.ToInt32(
                    element.Substring(element.IndexOf("[") + 1).Replace("]", ""));

                obj = GetField(obj, elementName);

                if (obj is System.Collections.IList list)
                    obj = list[index];
            }
            else
            {
                obj = GetField(obj, element);
            }

            if (obj == null)
                return null;
        }

        return obj;
    }

    private object GetField(object source, string name)
    {
        if (source == null) return null;

        var type = source.GetType();
        var field = type.GetField(name,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        return field?.GetValue(source);
    }

    private object GetSerializedValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue;
            case SerializedPropertyType.Float: return prop.floatValue;
            case SerializedPropertyType.Boolean: return prop.boolValue;
            case SerializedPropertyType.String: return prop.stringValue;
            case SerializedPropertyType.Enum: return prop.enumValueIndex;
            case SerializedPropertyType.Vector2: return prop.vector2Value;
            case SerializedPropertyType.Vector3: return prop.vector3Value;
            case SerializedPropertyType.Color: return prop.colorValue;
            case SerializedPropertyType.ObjectReference: return prop.objectReferenceValue;
            default: return null;
        }
    }
}
#endif