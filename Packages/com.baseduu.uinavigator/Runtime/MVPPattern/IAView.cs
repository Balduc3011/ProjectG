using System;
using Cysharp.Threading.Tasks;

namespace Baseduu.UINavigator.MVPPattern
{
    public interface IAView
    {
        UniTask Initialize(Memory<object> args);
    }
}
