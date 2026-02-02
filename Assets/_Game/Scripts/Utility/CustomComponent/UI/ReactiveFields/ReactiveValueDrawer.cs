using UnityEditor;
using UnityEngine;
using Baseduu.ReactiveFields;

[CustomPropertyDrawer(typeof(ReactiveValue<>), true)]
public class ReactiveValueDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Lấy field _serializedValue bên trong ReactiveValue<T>
        var valueProp = property.FindPropertyRelative("_serializedValue");

        if (valueProp == null)
        {
            EditorGUI.LabelField(position, label.text, "Unsupported type");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, valueProp, label, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("_serializedValue");
        return EditorGUI.GetPropertyHeight(valueProp, label, true);
    }
}