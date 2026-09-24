#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class DataSaveCreatorWindow : EditorWindow
{
    private const string ScriptFolder = "Assets/_Game/Scripts/DataSave";
    private const string InGameDataPath = ScriptFolder + "/InGameData.cs";
    private const string SaveManagerPath = ScriptFolder + "/Manager/UserDataSaveManager.cs";
    private const string Suffix = "DataSave";
    // Generated lines are inserted right above these marker comments.
    private const string MarkerPrefix = "// @DataSaveCreator:";

    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private static readonly string[] Tabs = { "CREATE", "REMOVE" };

    private int tab;
    private string saveName = "";
    private List<string> existingSaves = new List<string>();
    private int removeIndex;

    [MenuItem("Assets/Create/DataSave", priority = -1)]
    private static void Open()
    {
        var window = GetWindow<DataSaveCreatorWindow>(true, "DataSave");
        window.minSize = new Vector2(360, 140);
        window.ShowUtility();
    }

    private void OnEnable() => RefreshExistingSaves();
    private void OnFocus() => RefreshExistingSaves();

    private void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(24));
        EditorGUILayout.Space();

        if (tab == 0) DrawCreateTab();
        else DrawRemoveTab();
    }

    // "Quest" or "QuestDataSave" -> "Quest"
    private string BaseName
    {
        get
        {
            string name = saveName.Trim();
            return name.EndsWith(Suffix) ? name.Substring(0, name.Length - Suffix.Length) : name;
        }
    }

    private string ClassName => BaseName + Suffix;
    private string KeyName => BaseName.Length == 0 ? "" : char.ToLowerInvariant(BaseName[0]) + BaseName.Substring(1);
    private string KeyValue => BaseName + "Data";
    private string ScriptPath => $"{ScriptFolder}/{ClassName}.cs";

    private void DrawCreateTab()
    {
        saveName = EditorGUILayout.TextField("Save Name", saveName);

        EditorGUILayout.Space();

        string error = Validate();
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Script: {ScriptPath}\nKey: \"{KeyValue}\"\nRegister: InGameData, UserDataSaveManager", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("CreateDataSave", GUILayout.Height(28)))
            {
                Create();
            }
        }
    }

    private void DrawRemoveTab()
    {
        if (existingSaves.Count == 0)
        {
            EditorGUILayout.HelpBox("No DataSave found in InGameData.", MessageType.Info);
            return;
        }

        removeIndex = Mathf.Clamp(removeIndex, 0, existingSaves.Count - 1);
        removeIndex = EditorGUILayout.Popup("DataSave", removeIndex, existingSaves.ToArray());
        string className = existingSaves[removeIndex];

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Script: {FindScriptPath(className) ?? "(not found)"}\n" +
                                "Remove setup in: InGameData, UserDataSaveManager\n" +
                                "Other usages must be removed manually.", MessageType.Info);

        if (GUILayout.Button("Remove", GUILayout.Height(28)))
        {
            Remove(className);
        }
    }

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(saveName)) return "Enter a save name.";
        if (BaseName.Length == 0) return $"Enter a name before '{Suffix}'.";
        if (!IsValidIdentifier(ClassName)) return $"'{ClassName}' is not a valid class name.";
        if (CSharpKeywords.Contains(KeyName)) return $"Key name '{KeyName}' is a C# keyword, choose another name.";
        if (File.Exists(ScriptPath)) return $"Script already exists: {ScriptPath}";
        if (!File.Exists(InGameDataPath)) return $"Missing file: {InGameDataPath}";
        if (!File.Exists(SaveManagerPath)) return $"Missing file: {SaveManagerPath}";

        string inGameData = File.ReadAllText(InGameDataPath);
        string saveManager = File.ReadAllText(SaveManagerPath);
        if (inGameData.Contains($" {ClassName} {ClassName} ")) return $"InGameData already has {ClassName}.";
        if (saveManager.Contains($"const string {KeyName} ")) return $"Key '{KeyName}' already exists in UserDataSaveManager.";
        if (saveManager.Contains($"\"{KeyValue}\"")) return $"Key value \"{KeyValue}\" already exists in UserDataSaveManager.";

        return FindMissingMarker(inGameData, GetInGameDataInserts(), "InGameData")
               ?? FindMissingMarker(saveManager, GetSaveManagerInserts(), "UserDataSaveManager");
    }

    private static string FindMissingMarker(string text, Dictionary<string, string> inserts, string fileName)
    {
        var markers = text.Split('\n').Select(GetMarkerName).Where(name => name != null).ToList();
        string missing = inserts.Keys.FirstOrDefault(marker => !markers.Contains(marker));
        return missing == null ? null : $"Marker '{MarkerPrefix}{missing}' not found in {fileName}.";
    }

    private void Create()
    {
        string inGameData = InsertAtMarkers(File.ReadAllText(InGameDataPath), GetInGameDataInserts());
        string saveManager = InsertAtMarkers(File.ReadAllText(SaveManagerPath), GetSaveManagerInserts());

        File.WriteAllText(ScriptPath, GetTemplate(ClassName));
        File.WriteAllText(InGameDataPath, inGameData);
        File.WriteAllText(SaveManagerPath, saveManager);
        AssetDatabase.Refresh();

        var script = AssetDatabase.LoadAssetAtPath<Object>(ScriptPath);
        Selection.activeObject = script;
        EditorGUIUtility.PingObject(script);
        Debug.Log($"[DataSaveCreator] Created {ScriptPath} (key \"{KeyValue}\")");
        Close();
    }

    private void Remove(string className)
    {
        string scriptPath = FindScriptPath(className);
        string inGameData = File.ReadAllText(InGameDataPath);
        string saveManager = File.ReadAllText(SaveManagerPath);

        // Every setup line names the class, except the key const and DeleteKey which only use Keys.<key>.
        var inGameDataPatterns = new List<string> { $@"\b{className}\b" };
        var saveManagerPatterns = new List<string> { $@"\b{className}\b" };
        Match keyMatch = Regex.Match(saveManager, $@"\b{className}\b[^\n]*\bKeys\.(\w+)");
        if (keyMatch.Success)
        {
            string key = keyMatch.Groups[1].Value;
            saveManagerPatterns.Add($@"\bKeys\.{key}\b");
            saveManagerPatterns.Add($@"\bconst\s+string\s+{key}\b");
        }

        string newInGameData = RemoveLines(inGameData, inGameDataPatterns, out int inGameDataCount);
        string newSaveManager = RemoveLines(saveManager, saveManagerPatterns, out int saveManagerCount);

        string message = $"Remove {className}?\n\n" +
                         $"- Delete script: {scriptPath ?? "(not found)"}\n" +
                         $"- InGameData: {inGameDataCount} line(s)\n" +
                         $"- UserDataSaveManager: {saveManagerCount} line(s)\n\n" +
                         "Other usages must be removed manually.";
        if (!EditorUtility.DisplayDialog("Remove DataSave", message, "Remove", "Cancel")) return;

        File.WriteAllText(InGameDataPath, newInGameData);
        File.WriteAllText(SaveManagerPath, newSaveManager);
        if (scriptPath != null) AssetDatabase.DeleteAsset(scriptPath);
        AssetDatabase.Refresh();

        Debug.Log($"[DataSaveCreator] Removed {className}");
        RefreshExistingSaves();
    }

    private static string RemoveLines(string text, List<string> patterns, out int count)
    {
        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        count = lines.RemoveAll(line => GetMarkerName(line) == null && patterns.Any(pattern => Regex.IsMatch(line, pattern)));
        return string.Join(newLine, lines);
    }

    private void RefreshExistingSaves()
    {
        existingSaves = File.Exists(InGameDataPath)
            ? Regex.Matches(File.ReadAllText(InGameDataPath), $@"public\s+(\w+{Suffix})\s+\1\s*\{{")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .ToList()
            : new List<string>();
    }

    private static string FindScriptPath(string className)
    {
        return AssetDatabase.FindAssets($"{className} t:MonoScript", new[] { ScriptFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == className);
    }

    private Dictionary<string, string> GetInGameDataInserts()
    {
        return new Dictionary<string, string>
        {
            { "Property", $"[field: SerializeField] public {ClassName} {ClassName} {{ get; set; }}= new();" },
            { "Init", $"{ClassName} ??= new {ClassName}();" },
        };
    }

    private Dictionary<string, string> GetSaveManagerInserts()
    {
        return new Dictionary<string, string>
        {
            { "Key", $"public const string {KeyName} = \"{KeyValue}\";" },
            { "Save", $"public static void Save{BaseName}Data({ClassName} data) {{ DataSave.DataFactory.SaveData(Keys.{KeyName}, data); }}" },
            { "Load", $"public static {ClassName} Load{BaseName}Data() {{ return DataSave.DataFactory.LoadData(Keys.{KeyName}, new {ClassName}()); }}" },
            { "LoadAll", $"{ClassName} = Load{BaseName}Data()," },
            { "SaveAll", $"Save{BaseName}Data(data.{ClassName});" },
            { "DeleteAll", $"DataFactory.DeleteKey(Keys.{KeyName});" },
        };
    }

    // Inserts each line above every occurrence of its marker, keeping the marker's indentation.
    private static string InsertAtMarkers(string text, Dictionary<string, string> inserts)
    {
        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            string marker = GetMarkerName(lines[i]);
            if (marker == null || !inserts.TryGetValue(marker, out string code)) continue;

            string indent = lines[i].Substring(0, lines[i].Length - lines[i].TrimStart().Length);
            lines.Insert(i, indent + code);
        }

        return string.Join(newLine, lines);
    }

    // "// @DataSaveCreator:Load - DO NOT DELETE THIS LINE" -> "Load"
    private static string GetMarkerName(string line)
    {
        string trimmed = line.Trim();
        if (!trimmed.StartsWith(MarkerPrefix)) return null;
        return trimmed.Substring(MarkerPrefix.Length).Split(' ')[0];
    }

    private static string GetTemplate(string className)
    {
        return $@"using System.Collections.Generic;
using MemoryPack;

namespace DataSave
{{
    [System.Serializable]
    [MemoryPackable]
    public partial class {className}
    {{
        public static {className} Instance => InGameDataManager.Instance.InGameData.{className};

        public static class Function
        {{
            private static {className} {className} => InGameDataManager.Instance.InGameData.{className};
        }}
    }}
}}
";
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}

#endif
