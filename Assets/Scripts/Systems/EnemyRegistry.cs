using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Core;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 敌人注册表。维护当前场景中所有活跃敌人的列表，
    /// 替代每帧 FindObjectsOfType&lt;Enemy&gt;() 的性能黑洞。
    /// 同时提供空间网格加速查询。
    /// </summary>
    public class EnemyRegistry : Singleton<EnemyRegistry>
    {
        [Header("空间网格设置")]
        [Tooltip("格子大小（世界单位）")]
        [SerializeField] private float _cellSize = 3f;

        [Tooltip("地图宽度")]
        [SerializeField] private float _mapWidth = 20f;

        [Tooltip("地图高度")]
        [SerializeField] private float _mapHeight = 12f;

        // 全部活跃敌人列表
        private readonly List<Enemy> _activeEnemies = new List<Enemy>(256);

        // 空间网格
        private int _gridCols;
        private int _gridRows;
        private List<Enemy>[] _gridBuckets;
        private bool _gridInitialized = false;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            BuildGrid();
        }

        private void BuildGrid()
        {
            _gridCols = Mathf.CeilToInt(_mapWidth / _cellSize);
            _gridRows = Mathf.CeilToInt(_mapHeight / _cellSize);
            int total = _gridCols * _gridRows;
            _gridBuckets = new List<Enemy>[total];
            for (int i = 0; i < total; i++)
            {
                _gridBuckets[i] = new List<Enemy>(16);
            }
            _gridInitialized = true;
        }

        /// <summary>注册一个活跃敌人。</summary>
        public void Register(Enemy enemy)
        {
            if (enemy == null) return;
            if (!_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Add(enemy);
            }
            EnsureGrid();
            AddToGrid(enemy);
        }

        /// <summary>注销一个敌人。</summary>
        public void Unregister(Enemy enemy)
        {
            if (enemy == null) return;
            _activeEnemies.Remove(enemy);
            if (_gridInitialized) RemoveFromGrid(enemy);
        }

        /// <summary>敌人移动后更新其在网格中的位置。</summary>
        public void UpdateGridPosition(Enemy enemy)
        {
            if (enemy == null || !enemy.IsActive || !_gridInitialized) return;
            RemoveFromGrid(enemy);
            AddToGrid(enemy);
        }

        /// <summary>
        /// 获取在指定位置、指定半径范围内的所有敌人。
        /// </summary>
        public void GetEnemiesInRange(Vector3 center, float radius, List<Enemy> results)
        {
            results.Clear();

            // 确保网格已初始化
            EnsureGrid();

            // 如果网格还没初始化好，直接遍历全部活跃敌人
            if (!_gridInitialized)
            {
                float sqrRadius = radius * radius;
                foreach (var e in _activeEnemies)
                {
                    if (e == null || !e.IsActive) continue;
                    if ((e.transform.position - center).sqrMagnitude <= sqrRadius)
                    {
                        results.Add(e);
                    }
                }
                return;
            }

            float halfW = _mapWidth / 2f;
            float halfH = _mapHeight / 2f;

            int minCol = Mathf.Max(0, Mathf.FloorToInt((center.x - radius + halfW) / _cellSize));
            int maxCol = Mathf.Min(_gridCols - 1, Mathf.FloorToInt((center.x + radius + halfW) / _cellSize));
            int minRow = Mathf.Max(0, Mathf.FloorToInt((center.y - radius + halfH) / _cellSize));
            int maxRow = Mathf.Min(_gridRows - 1, Mathf.FloorToInt((center.y + radius + halfH) / _cellSize));

            float sqrRadius2 = radius * radius;

            for (int col = minCol; col <= maxCol; col++)
            {
                for (int row = minRow; row <= maxRow; row++)
                {
                    int idx = row * _gridCols + col;
                    if (idx < 0 || idx >= _gridBuckets.Length) continue;
                    var bucket = _gridBuckets[idx];
                    if (bucket == null) continue;
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        var e = bucket[i];
                        if (e == null || !e.IsActive) continue;
                        float sqrDist = (e.transform.position - center).sqrMagnitude;
                        if (sqrDist <= sqrRadius2)
                        {
                            results.Add(e);
                        }
                    }
                }
            }
        }

        /// <summary>获取所有活跃敌人。</summary>
        public IReadOnlyList<Enemy> GetAllActiveEnemies() => _activeEnemies;

        /// <summary>清空注册表。</summary>
        public void Clear()
        {
            _activeEnemies.Clear();
            if (_gridInitialized && _gridBuckets != null)
            {
                for (int i = 0; i < _gridBuckets.Length; i++)
                {
                    _gridBuckets[i]?.Clear();
                }
            }
        }

        private void AddToGrid(Enemy enemy)
        {
            EnsureGrid();

            float halfW = _mapWidth / 2f;
            float halfH = _mapHeight / 2f;
            int col = Mathf.FloorToInt((enemy.transform.position.x + halfW) / _cellSize);
            int row = Mathf.FloorToInt((enemy.transform.position.y + halfH) / _cellSize);
            col = Mathf.Clamp(col, 0, _gridCols - 1);
            row = Mathf.Clamp(row, 0, _gridRows - 1);
            int idx = row * _gridCols + col;

            if (idx < 0 || idx >= _gridBuckets.Length || _gridBuckets[idx] == null)
                return;

            enemy.GridIndex = idx;
            if (!_gridBuckets[idx].Contains(enemy))
            {
                _gridBuckets[idx].Add(enemy);
            }
        }

        private void RemoveFromGrid(Enemy enemy)
        {
            if (!_gridInitialized || _gridBuckets == null)
            {
                enemy.GridIndex = -1;
                return;
            }

            int gridIndex = enemy.GridIndex;
            if (gridIndex >= 0 && gridIndex < _gridBuckets.Length && _gridBuckets[gridIndex] != null)
            {
                _gridBuckets[gridIndex].Remove(enemy);
            }
            enemy.GridIndex = -1;
        }

        /// <summary>
        /// 确保空间网格已初始化且桶完整。任何网格操作前调用，防止
        /// 单例时序/重复创建导致的 NullReference。
        /// </summary>
        private void EnsureGrid()
        {
            bool needRebuild = !_gridInitialized || _gridBuckets == null;
            if (!needRebuild)
            {
                // 检查是否有缺失的桶
                for (int i = 0; i < _gridBuckets.Length; i++)
                {
                    if (_gridBuckets[i] == null) { needRebuild = true; break; }
                }
            }
            if (needRebuild) BuildGrid();
        }
    }
}
