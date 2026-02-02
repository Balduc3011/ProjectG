using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Scripts;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Views;

namespace Balduu
{
    public class UIScreenMain : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenMain";
        [field: SerializeField] public UIScreenMainModel Model { get; private set; }
        [field: SerializeField] public UIScreenMainView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIScreenMainModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIScreenMainView
    {
        [field: SerializeField] public CanvasGroup MainView {get; private set;}
        [field: SerializeField] public MenuNav MenuNav {get; private set;}
        [field: SerializeField] public List<Modal> MenuModals {get; private set;}
        
        public async UniTask Initialize(Memory<object> args)
        {
            InitializeModal(args).Forget();
            await UniTask.DelayFrame(10);
            MenuNav.PreSetup();
            await UniTask.CompletedTask;
        }
        
        public async UniTask InitializeModal(Memory<object> args)
        {
            for (var i = 0; i < MenuModals.Count; i++)
            {
                MenuModals[i].Initialize(args).Forget();
            }
        }
    }
}
