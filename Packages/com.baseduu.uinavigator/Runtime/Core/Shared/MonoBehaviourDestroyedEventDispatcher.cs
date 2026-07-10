using System;
using UnityEngine;

namespace Baseduu.UINavigator.Core
{
    public class MonoBehaviourDestroyedEventDispatcher : MonoBehaviour
    {
        public void OnDestroy()
        {
            OnDispatch?.Invoke();
        }

        public event Action OnDispatch;
    }
}