using UnityEngine;
using Baseduu.UINavigator.Core;
using System.Collections.Generic;

namespace Balduu
{
    // Transition animation for the shop tab. UIScreenMain parks every tab modal at a fixed
    // anchoredPosition and slides them as a group through SwipeArea, so this animation must not
    // write RectTransform.anchoredPosition - animate scale, alpha or a child rect instead.
    public class UIModalShopTransitionAnimation : TransitionAnimationBehaviour
    {
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private EaseType _easeType = EaseType.QuarticEaseOut;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform header;
        [SerializeField] private float headerAppearPos;
        [SerializeField] private float headerIdlePos;

        [SerializeField] private RectTransform shopComponent;
        [SerializeField] private float shopComponentAppearPos;
        [SerializeField] private float shopComponentIdlePos;

        public override float Duration => _duration;

        // Called once before the animation plays, with RectTransform already assigned.
        public override void Setup()
        {
        }

        public override void SetTime(float time)
        {
            var progress = _duration <= 0.0f ? 1.0f : Mathf.Clamp01(time / _duration);
            progress = Easings.Interpolate(progress, _easeType);

            canvasGroup.alpha = progress;

            var headerPos = header.anchoredPosition;
            headerPos.y = Mathf.Lerp(headerAppearPos, headerIdlePos, progress);
            header.anchoredPosition = headerPos;

            var componentPos = shopComponent.anchoredPosition;
            componentPos.x = Mathf.Lerp(shopComponentAppearPos, shopComponentIdlePos, progress);
            shopComponent.anchoredPosition = componentPos;
        }
    }
}
