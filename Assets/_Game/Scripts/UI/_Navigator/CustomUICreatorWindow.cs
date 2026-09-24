#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class CustomUICreatorWindow : EditorWindow
{
    private enum UIKind
    {
        Activity,
        Modal,
        Screen
    }

    private const string SampleFolder = "Assets/_Game/Addressables/UISample";
    private const string PrefabFolder = "Assets/_Game/Addressables/UI";
    private const string ScriptRootFolder = "Assets/_Game/Scripts/UI";
    private const string ScriptNamespace = "Balduu";
    private const string MainViewPropertyPath = "<UIPresenter>k__BackingField.<View>k__BackingField.<MainView>k__BackingField";

    // Survive the domain reload triggered by the new script, so the component can be attached afterwards.
    private const string PendingClassKey = "CustomUICreator.PendingClass";
    private const string PendingPrefabKey = "CustomUICreator.PendingPrefab";

    private UIKind kind = UIKind.Modal;
    private string uiName = "";
    private bool customName;
    private bool createSubFolder;

    [MenuItem("Assets/Create/CustomUI", priority = -1)]
    private static void Open()
    {
        var window = GetWindow<CustomUICreatorWindow>(true, "Create Custom UI");
        window.minSize = new Vector2(360, 170);
        window.ShowUtility();
    }

    private string ClassName => customName ? uiName.Trim() : $"UI{kind}{uiName.Trim()}";

    private string ScriptFolder
    {
        get
        {
            string folder = $"{ScriptRootFolder}/{GetKindFolder(kind)}";
            return createSubFolder ? $"{folder}/{ClassName}" : folder;
        }
    }

    private string ScriptPath => $"{ScriptFolder}/{ClassName}.cs";
    private string PrefabPath => $"{PrefabFolder}/{ClassName}.prefab";

    private void OnGUI()
    {
        kind = (UIKind)EditorGUILayout.EnumPopup("Type", kind);
        uiName = EditorGUILayout.TextField("UI Name", uiName);
        customName = EditorGUILayout.Toggle("Custom Name", customName);
        createSubFolder = EditorGUILayout.Toggle("Create Sub Folder", createSubFolder);

        EditorGUILayout.Space();

        string error = Validate();
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Script: {ScriptPath}\nPrefab: {PrefabPath}", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Create UI", GUILayout.Height(28)))
            {
                Create();
            }
        }
    }

    private string Validate()
    {
        string className = ClassName;
        if (string.IsNullOrWhiteSpace(uiName)) return "Enter a UI name.";
        if (!IsValidIdentifier(className)) return $"'{className}' is not a valid class name.";
        if (File.Exists(ScriptPath)) return $"Script already exists: {ScriptPath}";
        if (File.Exists(PrefabPath)) return $"Prefab already exists: {PrefabPath}";
        return null;
    }

    private void Create()
    {
        string className = ClassName;
        string samplePath = $"{SampleFolder}/UI{kind}Sample.prefab";

        if (!AssetDatabase.CopyAsset(samplePath, PrefabPath))
        {
            Debug.LogError($"[CustomUICreator] Failed to copy sample prefab: {samplePath}");
            return;
        }

        Directory.CreateDirectory(ScriptFolder);
        File.WriteAllText(ScriptPath, GetTemplate(kind, className));

        SessionState.SetString(PendingClassKey, $"{ScriptNamespace}.{className}");
        SessionState.SetString(PendingPrefabKey, PrefabPath);

        AssetDatabase.Refresh();
        Close();
    }

    [DidReloadScripts]
    private static void AttachPendingScript()
    {
        string typeName = SessionState.GetString(PendingClassKey, "");
        string prefabPath = SessionState.GetString(PendingPrefabKey, "");
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(prefabPath)) return;

        SessionState.EraseString(PendingClassKey);
        SessionState.EraseString(PendingPrefabKey);

        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName))
            .FirstOrDefault(t => t != null);
        if (type == null)
        {
            Debug.LogError($"[CustomUICreator] Type not found after compile: {typeName}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Component component = root.AddComponent(type);

            var serializedObject = new SerializedObject(component);
            SerializedProperty mainView = serializedObject.FindProperty(MainViewPropertyPath);
            if (mainView != null)
            {
                mainView.objectReferenceValue = root.GetComponent<CanvasGroup>();
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[CustomUICreator] MainView property not found on {typeName}");
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[CustomUICreator] Created {prefabPath} with {typeName}");
    }

    private static string GetKindFolder(UIKind kind)
    {
        switch (kind)
        {
            case UIKind.Activity: return "Activities";
            case UIKind.Modal: return "Modals";
            default: return "Screens";
        }
    }

    private static string GetTemplate(UIKind kind, string className)
    {
        switch (kind)
        {
            case UIKind.Activity: return ActivityTemplateCreator.GetActivityTemplate(className);
            case UIKind.Modal: return ModalTemplateCreator.GetModalTemplate(className);
            default: return ScreenTemplateCreator.GetScreenTemplate(className);
        }
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}

#endif
