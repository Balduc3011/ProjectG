#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModalTemplateCreator
{
    [MenuItem("Assets/Create/CustomUI/Modal", priority = -1)]
    private static void CreateModalTemplate()
    {
        string defaultName = "NewModal";
        string selectedPath = GetSelectedPathOrFallback();
        string path = EditorUtility.SaveFilePanelInProject("Create Modal", defaultName, "cs", "Enter a name for the new modal script", selectedPath);

        if (string.IsNullOrEmpty(path))
        {
            // Return if canceled.
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        string template = GetModalTemplate(fileName);

        File.WriteAllText(path, template);
        AssetDatabase.Refresh();
    }

    private static string GetModalTemplate(string className)
    {
        return $@"using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{{
    public class {className} : Modal
    {{
        public static class Events
        {{
            
        }}
        [field: SerializeField] public static string ModalName {{ get; private set; }} = ""{className}"";
        [field: SerializeField] public {className}Model Model {{ get; private set; }}
        [field: SerializeField] public {className}View View {{ get; private set; }}

        public override async UniTask Initialize(Memory<object> args)
        {{
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseModal);
        }}

        public override UniTask Cleanup(Memory<object> args)
        {{
            return UniTask.CompletedTask;
        }}

        private void OnClickCloseModal()
        {{
            CloseModal().Forget();
        }}

        private async UniTask CloseModal()
        {{
            ClearAction();
            await UIManager.CloseModal(ModalName, null);
        }}

        private void ClearAction()
        {{
            View.CloseBtn?.onClick.RemoveAllListeners();
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