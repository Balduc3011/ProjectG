using UnityEngine;
using Baseduu.UINavigator.Core.Views;

namespace Baseduu.UINavigator.Core.Windows
{
    public interface IWindowContainer : IViewContainer
    {
        string LayerName { get; }

        WindowContainerType LayerType { get; }

        IWindowContainerManager ContainerManager { get; }

        Canvas Canvas { get; }
    }
}