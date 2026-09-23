using UnityEngine;
using Baseduu.UINavigator.Core;

namespace Balduu
{
    // Transition animation for the menu tab. UIScreenMain parks every tab modal at a fixed
    // anchoredPosition and slides them as a group through SwipeArea, so this animation must not
    // write RectTransform.anchoredPosition - animate scale, alpha or a child rect instead.
    public class UIModalMenuTransitionAnimation : TransitionAnimationBehaviour
    {
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private EaseType _easeType = EaseType.QuarticEaseOut;

        public override float Duration => _duration;

        // Called once before the animation plays, with RectTransform already assigned.
        public override void Setup()
        {
        }

        public override void SetTime(float time)
        {
            var progress = _duration <= 0.0f ? 1.0f : Mathf.Clamp01(time / _duration);
            progress = Easings.Interpolate(progress, _easeType);
            Apply(progress);
        }

        // TODO: chuyen dong chua chot. progress chay 0 -> 1 trong suot Duration.
        private void Apply(float progress)
        {
        }
    }
}
