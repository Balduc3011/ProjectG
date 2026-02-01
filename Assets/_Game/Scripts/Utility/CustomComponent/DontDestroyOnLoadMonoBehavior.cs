using UnityEngine;

namespace Balduu.Utility.CustomComponent
{
    public class DontDestroyOnLoadMonoBehavior : MonoBehaviour
    {
        private void Awake()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

}