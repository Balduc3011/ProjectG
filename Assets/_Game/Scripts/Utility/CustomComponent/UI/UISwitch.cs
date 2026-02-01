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
        public float trasitionTime = 0.15f;
        private bool isTrasiting;
        
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
            if (!isTrasiting)
            {
                activeStatus = !activeStatus;
                onValueChange?.Invoke(activeStatus);
                UpdateSwitchVisuals().Forget();
            }
        }
    
        private async UniTask UpdateSwitchVisuals()
        {
            var targetPos = !activeStatus ? handleDeactivePos : handleActivePos;
            isTrasiting = true;
            if (handleActiveCG)
            {
                var targetAlpha = !activeStatus ? 0f : 1f;
                LMotion.Create(handleActiveCG.alpha, targetAlpha, trasitionTime)
                    .BindToAlpha(handleActiveCG)
                    .AddTo(handleActiveCG);
            }
            await LMotion.Create(handle.localPosition, targetPos.localPosition, trasitionTime)
                .BindToLocalPosition(handle)
                .AddTo(handle);
            isTrasiting = false;
        }
    }
}

