using System;
using Cysharp.Threading.Tasks;

namespace Baseduu.UINavigator.MVPPattern
{
    public interface IAModel
    {
        UniTask Initialize(Memory<object> args);
    }
}
