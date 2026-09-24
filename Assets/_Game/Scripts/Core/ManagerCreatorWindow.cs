#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ManagerCreatorWindow : EditorWindow
{
    private const string GameManagerFolder = "Assets/_Game/Scripts/Manager";
    private const string WorldManagerFolder = "Assets/_Game/Scripts/Manager/World";
    private const string Suffix = "Manager";

    private static readonly string[] Tabs = { "GameManager", "WorldManager" };

    private int tab;
    private string managerName = "";

    [MenuItem("Assets/Create/Manager", priority = -1)]
    private static void Open()
    {
        var window = GetWindow<ManagerCreatorWindow>(true, "Create Manager");
        window.minSize = new Vector2(360, 140);
        window.ShowUtility();
    }

    private bool IsWorld => tab == 1;

    // "Quest" or "QuestManager" -> "QuestManager"
    private string ClassName
    {
        get
        {
            string name = managerName.Trim();
            return name.EndsWith(Suffix) ? name : name + Suffix;
        }
    }

    private string ScriptFolder => IsWorld ? WorldManagerFolder : GameManagerFolder;
    private string ScriptPath => $"{ScriptFolder}/{ClassName}.cs";
    private string BaseClass => IsWorld ? nameof(BaseWorldManager) : nameof(BaseManager);
    private string Scope => IsWorld ? nameof(WorldSubsystem) : nameof(GameManager);

    private void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(24));
        EditorGUILayout.Space();

        managerName = EditorGUILayout.TextField("Manager Name", managerName);

        EditorGUILayout.Space();

        string error = Validate();
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Script: {ScriptPath}\nBase: {BaseClass}\nThêm làm con của {Scope} sau khi tạo.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Create Manager", GUILayout.Height(28)))
            {
                Create();
            }
        }
    }

    private string Validate()
    {
        string className = ClassName;
        if (string.IsNullOrWhiteSpace(managerName)) return "Enter a manager name.";
        if (className == Suffix) return $"Enter a name before '{Suffix}'.";
        if (!IsValidIdentifier(className)) return $"'{className}' is not a valid class name.";
        if (File.Exists(ScriptPath)) return $"Script already exists: {ScriptPath}";
        // Cả hai scope đều ở global namespace nên tên không được trùng với bất kỳ type nào đã có.
        if (TypeExists(className)) return $"Type '{className}' already exists in project.";
        return null;
    }

    private void Create()
    {
        Directory.CreateDirectory(ScriptFolder);
        File.WriteAllText(ScriptPath, GetTemplate(ClassName, BaseClass, Scope, IsWorld));
        AssetDatabase.Refresh();

        var script = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScriptPath);
        Selection.activeObject = script;
        EditorGUIUtility.PingObject(script);
        Debug.Log($"[ManagerCreator] Created {ScriptPath}. Thêm {ClassName} làm con của {Scope}.");
        Close();
    }

    private static string GetTemplate(string className, string baseClass, string scope, bool isWorld)
    {
        string getter = isWorld ? $"WorldSubsystem.Get<{className}>()" : $"GameManager.Get<{className}>()";
        return $@"using Cysharp.Threading.Tasks;

/// <summary>
/// Đặt làm con của {scope}. Lấy ra bằng <c>{getter.Replace("<", "&lt;").Replace(">", "&gt;")}</c>.
/// </summary>
public class {className} : {baseClass}
{{
    protected internal override void Initialize(ManagerCollection<{baseClass}> managers)
    {{
        // Chỉ lấy reference / subscribe event. Không load data, không gọi logic của manager khác.
    }}

    public override UniTask LoadData()
    {{
        return UniTask.CompletedTask;
    }}

    protected internal override UniTask PostLoad()
    {{
        return UniTask.CompletedTask;
    }}

    protected internal override void Deinitialize()
    {{
        // Huỷ subscribe event, dọn state của chính mình.
    }}
}}
";
    }

    private static bool TypeExists(string className)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(className) != null);
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}

#endif
