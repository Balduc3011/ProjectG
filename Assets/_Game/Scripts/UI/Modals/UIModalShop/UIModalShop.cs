using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{
    public class UIModalShop : Modal
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public static string ModalName { get; private set; } = "UIModalShop";
        [field: SerializeField] public UIModalShopModel Model { get; private set; }
        [field: SerializeField] public UIModalShopView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseModal);
            InitShopPack();
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        private void OnClickCloseModal()
        {
            CloseModal().Forget();
        }

        private async UniTask CloseModal()
        {
            ClearAction();
            await UIManager.CloseModal(ModalName, null);
        }

        private void ClearAction()
        {
            View.CloseBtn?.onClick.RemoveAllListeners();
        }

        private void InitShopPack()
        {
            for (var i = 0; i < View.UiShopPacks.Count; i++)
            {
                View.UiShopPacks[i].Init();
            }
        }
        
    }

    [System.Serializable]
    public class UIModalShopModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIModalShopView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
        [field: SerializeField] public List<UIShopPack> UiShopPacks { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}
