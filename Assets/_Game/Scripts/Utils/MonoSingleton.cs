using UnityEngine;

namespace Game.Utils
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;
        
        public virtual void Awake()
        {
            if (Instance != null && Instance != this as T)
            {
                Destroy(this.gameObject);
                return;
            }
            
            Instance = this as T;
        }
    }
}
