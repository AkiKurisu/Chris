using UnityEngine;
namespace Chris
{
    /// <summary>
    /// Generic helper for singleton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<T>();
                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(gameObject);
            else
                _instance = (T)this;
        }
        
        public static bool IsInitialized => _instance != null;

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}