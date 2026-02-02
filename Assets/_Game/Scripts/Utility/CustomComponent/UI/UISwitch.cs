using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Balduu
{
    public class UISwitch : MonoBehaviour
    {
        public Action<bool> onValueChange;
        public bool activeStatus;
        public Button toggleButton;
        public Transform handle;
        public Transform handleActivePos;
        public Transform handleDeactivePos;
        public CanvasGroup handleActiveCG;
        public float transitionTime = 0.15f;
        private bool isTransiting;
        
        private void Start()
        {
            toggleButton.onClick.AddListener(ToggleSwitch);
        }
    
        public void Init(bool status)
        {
            activeStatus = status;
            UpdateSwitchVisuals().Forget();
        }
        
        private void ToggleSwitch()
        {
            if (!isTransiting)
            {
                activeStatus = !activeStatus;
                onValueChange?.Invoke(activeStatus);
                UpdateSwitchVisuals().Forget();
            }
        }
    
        private async UniTask UpdateSwitchVisuals()
        {
            var targetPos = !activeStatus ? handleDeactivePos : handleActivePos;
            isTransiting = true;
            if (handleActiveCG)
            {
                var targetAlpha = !activeStatus ? 0f : 1f;
                LMotion.Create(handleActiveCG.alpha, targetAlpha, transitionTime)
                    .BindToAlpha(handleActiveCG)
                    .AddTo(handleActiveCG);
            }
            await LMotion.Create(handle.localPosition, targetPos.localPosition, transitionTime)
                .BindToLocalPosition(handle)
                .AddTo(handle);
            isTransiting = false;
        }
    }
}

