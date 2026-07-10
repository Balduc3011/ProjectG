using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Views;
using Baseduu.UINavigator.Foundation;

namespace Baseduu.UINavigator.Core.Modals
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ModalBackdrop : View
    {
        [SerializeField] private ModalBackdropTransitionAnimationContainer _animationContainer;

        public ModalBackdropTransitionAnimationContainer AnimationContainer => _animationContainer;

        protected override void Awake()
        {
        }

        public void Setup(
              RectTransform parent
            , in float? alpha
            , in bool? closeModalWhenClick
        )
        {
            
        }

        private void SetAlpha(in float? value)
        {
            
        }

        private void SetCloseModalOnClick(in bool? value)
        {
            
        }

        private void CloseModalOnClick()
        {
            var modalContainer = ModalContainer.Of(transform);

            if (modalContainer.IsInTransition)
                return;

            modalContainer.Pop(true);
        }

        internal async UniTask<StubEnter> EnterAsync(bool playAnimation)
        {
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
            return default;
        }

        internal async UniTask<StubEnter> ExitAsync(bool playAnimation)
        {
            gameObject.SetActive(false);
            await UniTask.CompletedTask;
            return default;
        }

        private ITransitionAnimation GetAnimation(bool enter)
        {
            var anim = _animationContainer.GetAnimation(enter);

            if (anim == null)
            {
                return Settings.GetDefaultModalBackdropTransitionAnimation(enter);
            }

            return anim;
        }
    }
}