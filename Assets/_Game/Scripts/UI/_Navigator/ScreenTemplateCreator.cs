#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScreenTemplateCreator
{
    [MenuItem("Assets/Create/CustomUI/Screen", priority = -1)]
    private static void CreateScreenTemplate()
    {
        string defaultName = "NewScreen";
        string selectedPath = GetSelectedPathOrFallback();
        string path = EditorUtility.SaveFilePanelInProject("Create Screen", defaultName, "cs", "Enter a name for the new screen script", selectedPath);

        if (string.IsNullOrEmpty(path))
        {
            // Return if canceled.
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        string template = GetScreenTemplate(fileName);

        File.WriteAllText(path, template);
        AssetDatabase.Refresh();
    }

    private static string GetScreenTemplate(string className)
    {
        return $@"using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Screens;
using Baseduu.UINavigator.MVPPattern;
using Screen = Baseduu.UINavigator.Core.Screens.Screen;

namespace Balduu
{{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class {className} : Screen
    {{
        [field: SerializeField] public static string ScreenName {{ get; private set; }} = ""{className}"";
        [field: SerializeField] public {className}Context.UIPresenter UIPresenter {{ get; private set; }}

        // Container-driven UIs are initialized by the navigator (it calls Initialize/Cleanup on
        // this component); embedded UIs (e.g. a tab hosted inside a screen) are initialized by a
        // direct Initialize call. Both paths reach these overrides, so the presenter is driven
        // here instead of via AddLifecycleEvent.
        public override UniTask Initialize(Memory<object> args)
        {{
            return UIPresenter.Initialize(args);
        }}

        public override UniTask Cleanup(Memory<object> args)
        {{
            return UIPresenter.Cleanup(args);
        }}
    }}

    [Serializable]
    public class {className}Context
    {{
        // Static hub for cross-UI communication.
        public static class Events
        {{
        }}

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {{
            public UniTask Initialize(Memory<object> args)
            {{
                return UniTask.CompletedTask;
            }}
        }}

        [HideLabel]
        [Serializable]
        public class UIView : IAView
        {{
            [field: Title(nameof(UIView))]
            [field: SerializeField] public CanvasGroup MainView {{ get; private set; }}
            [field: SerializeField] public Button CloseBtn {{ get; private set; }}

            public UniTask Initialize(Memory<object> args)
            {{
                return UniTask.CompletedTask;
            }}
        }}

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IScreenLifecycleEventSimple
        {{
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model {{ get; private set; }} = new();
            [field: SerializeField] public UIView View {{ get; private set; }} = new();

            public async UniTask Initialize(Memory<object> args)
            {{
                await Model.Initialize(args);
                await View.Initialize(args);
                View.CloseBtn?.AddListenerUI(OnClickCloseScreen);
            }}

            public UniTask Cleanup(Memory<object> args)
            {{
                View.CloseBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }}

            private void OnClickCloseScreen()
            {{
                UIManager.CloseScreen({className}.ScreenName, null).Forget();
            }}
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