#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public static class ActivityTemplateCreator
{
    [MenuItem("Assets/Create/CustomUI/Activity", priority = -1)]
    private static void CreateActivityTemplate()
    {
        string defaultName = "NewActivity";
        string selectedPath = GetSelectedPathOrFallback();
        string path = EditorUtility.SaveFilePanelInProject("Create Activity", defaultName, "cs", "Enter a name for the new activity script", selectedPath);

        if (string.IsNullOrEmpty(path))
        {
            // Return if canceled.
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        string template = GetActivityTemplate(fileName);

        File.WriteAllText(path, template);
        AssetDatabase.Refresh();
    }

    private static string GetActivityTemplate(string className)
    {
        return $@"using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;
using UnityEngine.UI;

namespace Balduu
{{
    public class {className} : Activity
    {{
        public static class Events
        {{
            
        }}
        [field: SerializeField] public bool ShowOnTop {{ get; private set; }}
        [field: SerializeField] public static string ActivityName {{ get; private set; }} = ""{className}"";
        [field: SerializeField] public {className}Model Model {{ get; private set; }}
        [field: SerializeField] public {className}View View {{ get; private set; }}

        public override async UniTask Initialize(Memory<object> args)
        {{
            if (ShowOnTop)
            {{
                transform.SetAsLastSibling();
            }}
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseActivity);
        }}

        public override UniTask Cleanup(Memory<object> args)
        {{
            ClearAction();
            return UniTask.CompletedTask;
        }}

        private void OnClickCloseActivity()
        {{
            CloseActivity().Forget();
        }}

        private async UniTask CloseActivity()
        {{
            await UIManager.CloseActivity(ActivityName, null);
        }}

        private void ClearAction()
        {{
            // Clear any listeners or actions here
        }}
    }}

    [System.Serializable]
    public class {className}Model
    {{
        public async UniTask Initialize(Memory<object> args)
        {{
            await UniTask.CompletedTask;
        }}
    }}

    [System.Serializable]
    public class {className}View
    {{
        [field: SerializeField] public CanvasGroup MainView {{ get; private set; }}
        [field: SerializeField] public Button CloseBtn {{ get; private set; }}

        public async UniTask Initialize(Memory<object> args)
        {{
            await UniTask.CompletedTask;
        }}
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