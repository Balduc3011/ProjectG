using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Scripts;
using LitMotion;
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
            public static Action OnEndDragSwipeEvent;
            public static Action<int> MoveSwipeAreaEvent;
        }
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenMain";
        [field: SerializeField] public UIScreenMainModel Model { get; private set; }
        [field: SerializeField] public UIScreenMainView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            Events.OnEndDragSwipeEvent += View.OnEndDragSwipe;
            Events.MoveSwipeAreaEvent += View.MoveSwipeArea;
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            Events.OnEndDragSwipeEvent -= View.OnEndDragSwipe;
            Events.MoveSwipeAreaEvent -= View.MoveSwipeArea;
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
        [field: SerializeField] public RectTransform MainRects {get; private set;}
        [field: SerializeField] public List<RectTransform> ModalRects {get; private set;}
        [field: SerializeField] public List<float> ModalRectsPosX {get; private set;}
        [field: SerializeField] public SwipeControls SwipeControls {get; private set;}
        [field: SerializeField] public RectTransform SwipeArea {get; private set;}
        
        public async UniTask Initialize(Memory<object> args)
        {
            InitializeModal(args).Forget();
            await UniTask.DelayFrame(10);
            MenuNav.PreSetup();
            await UniTask.CompletedTask;
        }
        
        public async UniTask InitializeModal(Memory<object> args)
        {
            InitModal(args);
            await UniTask.CompletedTask;
        }

        private void InitModal(Memory<object> args)
        {
            for (var i = 0; i < MenuModals.Count; i++)
            {
                MenuModals[i].Initialize(args).Forget();
            }
            var rectWidth = MainRects.rect.width;
            var delta = (ModalRects.Count - 1) / 2;
            for (var i = 0; i < ModalRects.Count; i++)
            {
                var posX = (i - delta) * rectWidth;
                ModalRects[i].anchoredPosition = new Vector2(posX, 0);
                ModalRectsPosX.Add(posX);
            }
        }

        public void OnEndDragSwipe()
        {
            var nearestDistance = float.MaxValue;
            var nearestIndex = 0;
            for (var i = 0; i < ModalRectsPosX.Count; i++)
            {
                var distance = Mathf.Abs(SwipeArea.anchoredPosition.x - ModalRectsPosX[i]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
            SwipeControls.AutoMoveSwipeArea(SwipeArea.anchoredPosition.x, ModalRectsPosX[nearestIndex]).Forget();
            MenuNav.SetSelectedTabEvent?.Invoke(ModalRectsPosX.Count - 1 - nearestIndex);
        }

        public void MoveSwipeArea(int tabIndex)
        {
            SwipeControls.AutoMoveSwipeArea(SwipeArea.anchoredPosition.x, ModalRectsPosX[ModalRects.Count - 1 -tabIndex]).Forget();
        }
    }
}
