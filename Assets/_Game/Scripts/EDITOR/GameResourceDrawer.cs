#if UNITY_EDITOR
using DataSave;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameResource))]
public class GameResourceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var resourceTypeProp = property.FindPropertyRelative("<Type>k__BackingField");
        var defineValueProp  = property.FindPropertyRelative("<DefineValue>k__BackingField");
        var valueProp        = property.FindPropertyRelative("<Value>k__BackingField");
        var expireTimeProp   = property.FindPropertyRelative("<ExpireTime>k__BackingField");

        float line = EditorGUIUtility.singleLineHeight;
        float space = 4f;

        Rect r1 = new Rect(position.x, position.y, position.width, line);
        Rect r2 = new Rect(position.x, position.y + line + space, position.width, line);
        Rect r3 = new Rect(position.x, position.y + (line + space) * 2, position.width, line);
        Rect r4 = new Rect(position.x, position.y + (line + space) * 3, position.width, line);

        // ResourceType
        EditorGUI.PropertyField(r1, resourceTypeProp);

        // DefineValue theo ResourceType
        ResourceType type = (ResourceType)resourceTypeProp.enumValueIndex;

        switch (type)
        {
            case ResourceType.Currency:
                defineValueProp.intValue =
                    (int)(CurrencyType)EditorGUI.EnumPopup(
                        r2,
                        "Currency",
                        (CurrencyType)defineValueProp.intValue);
                break;

            case ResourceType.Booster:
                defineValueProp.intValue =
                    (int)(BoosterType)EditorGUI.EnumPopup(
                        r2,
                        "Booster",
                        (BoosterType)defineValueProp.intValue);
                break;

            case ResourceType.Special:
                defineValueProp.intValue =
                    (int)(SpecialType)EditorGUI.EnumPopup(
                        r2,
                        "Special",
                        (SpecialType)defineValueProp.intValue);
                break;
        }

        // Value
        EditorGUI.PropertyField(r3, valueProp);

        // ExpireTime: runtime tự set, rỗng nghĩa là không hết hạn
        EditorGUI.PropertyField(r4, expireTimeProp, new GUIContent("Expire Time"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float space = 4f;
        return (line + space) * 4;
    }
}


#endif