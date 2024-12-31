using Photon.Pun;
using UnityEngine;

namespace MetaJungle.Utilities
{
    public class SingletonMonoBehaviourPunCallbacks<T> : MonoBehaviourPunCallbacks where T : SingletonMonoBehaviourPunCallbacks<T>
    {
        public static T Instance;

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}