using System;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Views;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Windows;

namespace Balduu
{
    public class Launcher : UnityScreenNavigatorLauncher
    {
        public static WindowContainerManager ContainerManager { get; private set; }
        
        

        protected override void OnAwake()
        {
            ContainerManager = this;
        }

        protected override void OnPostCreateContainers()
        {
            UnityScreenNavigatorSettings.Initialize();
            ShowTopPage().Forget();
        }

        private async UniTaskVoid ShowTopPage()
        {
            await UniTask.CompletedTask;
        }
    }
}