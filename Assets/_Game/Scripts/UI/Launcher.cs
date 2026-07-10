using System;
using Baseduu.UINavigator.Core.Screens;
using Baseduu.UINavigator.Core;
using Baseduu.UINavigator.Core.Views;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Baseduu.UINavigator.Core.Windows;

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