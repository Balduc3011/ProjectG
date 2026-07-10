using System;
using Cysharp.Threading.Tasks;

namespace Baseduu.UINavigator.MVPPattern
{
    public interface IAPresenter
    {
        UniTask Initialize(Memory<object> args);
    }
}
