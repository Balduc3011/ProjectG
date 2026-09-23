#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIItemShow))]
[CanEditMultipleObjects]
public class UIItemShowEditor : Editor
{
    private const string BASE_NAME = "UIItemShow";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var resourceTypeProp = serializedObject.FindProperty("resourceType");
        var defineValueProp = serializedObject.FindProperty("defineValue");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(resourceTypeProp);
        DrawDefineValue(resourceTypeProp, defineValueProp);
        bool resourceChanged = EditorGUI.EndChangeCheck();

        DrawPropertiesExcluding(serializedObject, "m_Script", "resourceType", "defineValue");

        serializedObject.ApplyModifiedProperties();

        if (resourceChanged)
        {
            RenameTargets();
        }
    }

    // DefineValue theo ResourceType
    private static void DrawDefineValue(SerializedProperty resourceTypeProp, SerializedProperty defineValueProp)
    {
        if (resourceTypeProp.hasMultipleDifferentValues)
        {
            return;
        }

        ResourceType type = (ResourceType)resourceTypeProp.enumValueIndex;
        if (type == ResourceType.None)
        {
            return;
        }

        EditorGUI.showMixedValue = defineValueProp.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int newDefineValue = DrawDefineValuePopup(type, defineValueProp.intValue);
        if (EditorGUI.EndChangeCheck())
        {
            defineValueProp.intValue = newDefineValue;
        }
        EditorGUI.showMixedValue = false;
    }

    private static int DrawDefineValuePopup(ResourceType type, int defineValue)
    {
        switch (type)
        {
            case ResourceType.Currency:
                return (int)(CurrencyType)EditorGUILayout.EnumPopup("Currency", (CurrencyType)defineValue);

            case ResourceType.Booster:
                return (int)(BoosterType)EditorGUILayout.EnumPopup("Booster", (BoosterType)defineValue);

            case ResourceType.Special:
                return (int)(SpecialType)EditorGUILayout.EnumPopup("Special", (SpecialType)defineValue);

            default:
                return defineValue;
        }
    }

    /// <summary>
    /// Đổi tên object theo resource đang chọn cho dễ theo dõi, ví dụ "UIItemShow Money".
    /// Chỉ chạy trong editor lúc sửa inspector.
    /// </summary>
    private void RenameTargets()
    {
        for (var i = 0; i < targets.Length; i++)
        {
            var itemShow = (UIItemShow)targets[i];
            string objectName = GetObjectName(itemShow.resourceType, itemShow.defineValue);
            if (itemShow.gameObject.name == objectName)
            {
                continue;
            }
            Undo.RecordObject(itemShow.gameObject, "Rename UIItemShow");
            itemShow.gameObject.name = objectName;
            PrefabUtility.RecordPrefabInstancePropertyModifications(itemShow.gameObject);
        }
    }

    private static string GetObjectName(ResourceType type, int defineValue)
    {
        switch (type)
        {
            case ResourceType.Currency:
                return $"{BASE_NAME} {(CurrencyType)defineValue}";

            case ResourceType.Booster:
                return $"{BASE_NAME} {(BoosterType)defineValue}";

            case ResourceType.Special:
                return $"{BASE_NAME} {(SpecialType)defineValue}";

            default:
                return BASE_NAME;
        }
    }
}
#endif
