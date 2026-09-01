using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 通用对象池。通过预制体预生成对象，避免频繁 Instantiate/Destroy 造成的性能开销。
    /// 适用于敌人、子弹、特效等高频生成/销毁的对象。
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        private readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, GameObject> _prefabMap = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Transform> _poolContainers = new Dictionary<int, Transform>();

        /// <summary>
        /// 初始化一个对象池。
        /// </summary>
        /// <param name="prefab">要池化的预制体</param>
        /// <param name="initialSize">预生成数量</param>
        /// <param name="parent">池化对象的父节点</param>
        public void InitializePool(GameObject prefab, int initialSize, Transform parent = null)
        {
            int id = prefab.GetInstanceID();
            if (_pools.ContainsKey(id))
            {
                Debug.LogWarning($"[ObjectPool] 预制体 {prefab.name} 的池已存在，跳过初始化。");
                return;
            }

            var container = new GameObject($"Pool_{prefab.name}").transform;
            if (parent != null) container.SetParent(parent);
            _poolContainers[id] = container;
            _prefabMap[id] = prefab;
            _pools[id] = new Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateObject(prefab, container);
                _pools[id].Enqueue(obj);
            }
        }

        /// <summary>
        /// 从池中获取一个对象。若池为空则动态扩容。
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            int id = prefab.GetInstanceID();
            if (!_pools.ContainsKey(id))
            {
                Debug.LogWarning($"[ObjectPool] 预制体 {prefab.name} 未初始化池，自动创建默认池。");
                InitializePool(prefab, 5);
            }

            var queue = _pools[id];
            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                obj = CreateObject(_prefabMap[id], _poolContainers[id]);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 将对象归还池中。
        /// </summary>
        public void Return(GameObject prefab, GameObject obj)
        {
            int id = prefab.GetInstanceID();
            if (!_pools.ContainsKey(id))
            {
                Debug.LogWarning($"[ObjectPool] 尝试归还未注册池的对象 {obj.name}，直接销毁。");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_poolContainers[id]);
            _pools[id].Enqueue(obj);
        }

        private GameObject CreateObject(GameObject prefab, Transform parent)
        {
            var obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            return obj;
        }

        /// <summary>
        /// 清空所有池（用于场景重置）。
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _poolContainers)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _pools.Clear();
            _prefabMap.Clear();
            _poolContainers.Clear();
        }
    }
}
