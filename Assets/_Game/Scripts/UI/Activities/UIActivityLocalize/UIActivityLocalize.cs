using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIActivityLocalize : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityLocalize";
        [field: SerializeField] public UIActivityLocalizeContext.UIPresenter UIPresenter { get; private set; }

        // Container-driven UIs are initialized by the navigator (it calls Initialize/Cleanup on
        // this component); embedded UIs (e.g. a tab hosted inside a screen) are initialized by a
        // direct Initialize call. Both paths reach these overrides, so the presenter is driven
        // here instead of via AddLifecycleEvent.
        public override UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            return UIPresenter.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UIPresenter.Cleanup(args);
        }
    }

    [Serializable]
    public class UIActivityLocalizeContext
    {
        // Static hub for cross-UI communication.
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIView : IAView
        {
            [field: Title(nameof(UIView))]
            [field: SerializeField] public CanvasGroup MainView { get; private set; }
            [field: SerializeField] public Button CloseBtn { get; private set; }
            [field: SerializeField] public LanguageBtn languageBtn { get; private set; }
            [field: SerializeField] public Transform Container { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IActivityLifecycleEventSimple
        {
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model { get; private set; } = new();
            [field: SerializeField] public UIView View { get; private set; } = new();

            // Mỗi ngôn ngữ trong key này là tên của chính nó (EN = English, VI = Tiếng Việt...).
            private const string LanguageNameKey = "txt_language_key";
            private readonly List<LanguageBtn> languageBtns = new();
            private LocalizationManager localizationManager;

            public async UniTask Initialize(Memory<object> args)
            {
                await Model.Initialize(args);
                await View.Initialize(args);
                View.CloseBtn?.AddListenerUI(OnClickCloseActivity);

                localizationManager = GameManager.Get<LocalizationManager>();
                if (languageBtns.Count == 0) SpawnLanguageBtns();
                localizationManager.OnLanguageChanged += OnLanguageChanged;
                RefreshSelected(localizationManager.CurrentLanguage);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.CloseBtn?.onClick.RemoveAllListeners();
                if (localizationManager != null)
                {
                    localizationManager.OnLanguageChanged -= OnLanguageChanged;
                    localizationManager = null;
                }
                return UniTask.CompletedTask;
            }

            // Spawn 1 lần duy nhất; tên ngôn ngữ hiển thị bằng chính ngôn ngữ đó nên không cần refresh khi đổi.
            private void SpawnLanguageBtns()
            {
                LanguageType[] languages = (LanguageType[])Enum.GetValues(typeof(LanguageType));
                for (int i = 0; i < languages.Length; i++)
                {
                    LanguageType language = languages[i];
                    LanguageBtn btn = UnityEngine.Object.Instantiate(View.languageBtn, View.Container);
                    btn.gameObject.SetActive(true);
                    btn.Setup(language, localizationManager.GetTextByKey(LanguageNameKey, language));
                    btn.Button.AddListenerUI(() => OnClickLanguage(language));
                    languageBtns.Add(btn);
                }
                View.languageBtn.gameObject.SetActive(false);
            }

            private void OnClickLanguage(LanguageType language)
            {
                if (localizationManager == null) return;
                localizationManager.ChangeLanguage(language);
            }

            private void OnLanguageChanged(LanguageType language)
            {
                RefreshSelected(language);
            }

            private void RefreshSelected(LanguageType current)
            {
                for (int i = 0; i < languageBtns.Count; i++)
                {
                    languageBtns[i].SetSelected(current);
                }
            }

            private void OnClickCloseActivity()
            {
                UIManager.CloseActivity(UIActivityLocalize.ActivityName, null).Forget();
            }
        }
    }
}
