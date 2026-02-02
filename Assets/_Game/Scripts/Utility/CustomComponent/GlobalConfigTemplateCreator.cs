#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GlobalConfigTemplateCreator
{
    [MenuItem("Assets/Create/Custom/GlobalConfig", priority = -1)]
    private static void CreateGlobalConfigTemplate()
    {
        string defaultName = "NewGlobalConfig";
        string selectedPath = GetSelectedPathOrFallback();
        string path = EditorUtility.SaveFilePanelInProject("Create GlobalConfig", defaultName, "cs", "Enter a name for the new GlobalConfig script", selectedPath);

        if (string.IsNullOrEmpty(path))
        {
            // Return if canceled.
            return;
        }
        
        string fileName = Path.GetFileNameWithoutExtension(path);
        string template = GetGlobalConfigTemplate(fileName);

        File.WriteAllText(path, template);
        AssetDatabase.Refresh();
    }
    
    private static string GetGlobalConfigTemplate(string className)
    {
        return $@"using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = ""{className}"", menuName = ""GlobalConfigs/{className}"")]
[GlobalConfig(""Assets/Resources/GlobalConfig/"")]
public class {className} : GlobalConfig<{className}>
{{
    private async void FetchData()
    {{
    }}
}}
";
    }
    
    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
            if (!string.IsNullOrEmpty(path))
            {
                break;
            }
        }
        return path;
    }
}
#endif
