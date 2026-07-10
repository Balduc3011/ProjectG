using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using Baseduu.UINavigator.Core.Modals;
using Baseduu.UINavigator.Core.Screens;
using Baseduu.UINavigator.MVPPattern;
using Screen = Baseduu.UINavigator.Core.Screens.Screen;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIScreenMain : Screen
    {
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenMain";
        [field: SerializeField] public UIScreenMainContext.UIPresenter UIPresenter { get; private set; }

        // Container-driven UIs are initialized by the navigator (it calls Initialize/Cleanup on
        // this component); embedded UIs (e.g. a tab hosted inside a screen) are initialized by a
        // direct Initialize call. Both paths reach these overrides, so the presenter is driven
        // here instead of via AddLifecycleEvent.
        public override UniTask Initialize(Memory<object> args)
        {
            return UIPresenter.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UIPresenter.Cleanup(args);
        }
    }

    [Serializable]
    public class UIScreenMainContext
    {
        public static class Events
        {
            public static Action OnEndDragSwipeEvent;
            public static Action<int> MoveSwipeAreaEvent;
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
            [field: SerializeField] public MenuNav MenuNav { get; private set; }
            [field: SerializeField] public List<Modal> MenuModals { get; private set; }
            [field: SerializeField] public RectTransform MainRects { get; private set; }
            [field: SerializeField] public List<RectTransform> ModalRects { get; private set; }
            [field: SerializeField] public List<float> ModalRectsPosX { get; private set; }
            [field: SerializeField] public SwipeControls SwipeControls { get; private set; }
            [field: SerializeField] public RectTransform SwipeArea { get; private set; }

            public async UniTask Initialize(Memory<object> args)
            {
                InitializeModal(args).Forget();
                await UniTask.DelayFrame(10);
                MenuNav.PreSetup();
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
                SwipeControls.AutoMoveSwipeArea(SwipeArea.anchoredPosition.x, ModalRectsPosX[ModalRects.Count - 1 - tabIndex]).Forget();
            }
        }

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IScreenLifecycleEventSimple
        {
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model { get; private set; } = new();
            [field: SerializeField] public UIView View { get; private set; } = new();

            public async UniTask Initialize(Memory<object> args)
            {
                await Model.Initialize(args);
                await View.Initialize(args);
                Events.OnEndDragSwipeEvent += View.OnEndDragSwipe;
                Events.MoveSwipeAreaEvent += View.MoveSwipeArea;
            }

            public UniTask Cleanup(Memory<object> args)
            {
                Events.OnEndDragSwipeEvent -= View.OnEndDragSwipe;
                Events.MoveSwipeAreaEvent -= View.MoveSwipeArea;
                return UniTask.CompletedTask;
            }
        }
    }
}
