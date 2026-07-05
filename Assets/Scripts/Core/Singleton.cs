using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// 泛型单例基类。继承后通过 Instance 访问全局唯一实例。
    /// 适用于管理器类，自动在场景中创建并在场景切换时保持存活（可选）。
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        [Header("单例设置")]
        [Tooltip("是否在切换场景时保持该对象存活")]
        [SerializeField] private bool _dontDestroyOnLoad = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] 实例 {typeof(T)} 已在应用退出时销毁，不再创建。");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                        if (_instance == null)
                        {
                            var go = new GameObject($"[{typeof(T).Name}]");
                            _instance = go.AddComponent<T>();
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (_dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] 检测到重复的 {typeof(T).Name} 实例，已销毁多余对象。");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 子类可重写此方法替代 Awake，避免破坏单例初始化逻辑。
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
