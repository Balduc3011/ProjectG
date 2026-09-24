#if UNITY_EDITOR

public static class ModalTemplateCreator
{
    internal static string GetModalTemplate(string className)
    {
        return $@"using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Modals;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class {className} : Modal
    {{
        [field: SerializeField] public static string ModalName {{ get; private set; }} = ""{className}"";
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
        public class UIPresenter : IAPresenter, IModalLifecycleEventSimple
        {{
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model {{ get; private set; }} = new();
            [field: SerializeField] public UIView View {{ get; private set; }} = new();

            public async UniTask Initialize(Memory<object> args)
            {{
                await Model.Initialize(args);
                await View.Initialize(args);
                View.CloseBtn?.AddListenerUI(OnClickCloseModal);
            }}

            public UniTask Cleanup(Memory<object> args)
            {{
                View.CloseBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }}

            private void OnClickCloseModal()
            {{
                UIManager.CloseModal({className}.ModalName, null).Forget();
            }}
        }}
    }}
}}
";
    }
}

#endif