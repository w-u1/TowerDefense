using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// 敌人生成器。管理敌人预制体的对象池，提供生成/回收接口。
    /// 程序化生成敌人Sprite，无需外部美术资源。
    /// </summary>
    public class EnemySpawner : Singleton<EnemySpawner>
    {
        [Header("生成设置")]
        [Tooltip("每种敌人初始池化数量")]
        [SerializeField] private int _poolSizePerType = 15;

        [Tooltip("敌人父节点")]
        [SerializeField] private Transform _enemyContainer;

        // 预制体缓存（按EnemyType）
        private readonly Dictionary<EnemyType, GameObject> _enemyPrefabs = new Dictionary<EnemyType, GameObject>();
        private readonly Dictionary<EnemyType, Queue<Enemy>> _enemyPools = new Dictionary<EnemyType, Queue<Enemy>>();

        // 路径点
        private Transform[] _pathPoints;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            if (_enemyContainer == null)
            {
                var go = new GameObject("Enemies");
                _enemyContainer = go.transform;
                _enemyContainer.SetParent(transform);
            }
        }

        /// <summary>
        /// 设置敌人行进路径。
        /// </summary>
        public void SetPath(Transform[] pathPoints)
        {
            _pathPoints = pathPoints;
        }

        /// <summary>
        /// 注册一种敌人类型并创建对象池。
        /// </summary>
        public void RegisterEnemyType(EnemyData data)
        {
            if (_enemyPrefabs.ContainsKey(data.Type)) return;

            // 程序化创建敌人预制体
            var prefab = CreateEnemyPrefab(data);
            _enemyPrefabs[data.Type] = prefab;

            // 初始化对象池
            var pool = new Queue<Enemy>();
            for (int i = 0; i < _poolSizePerType; i++)
            {
                var enemy = CreateEnemyInstance(prefab);
                pool.Enqueue(enemy);
            }
            _enemyPools[data.Type] = pool;
        }

        /// <summary>
        /// 生成一个敌人。
        /// </summary>
        public Enemy SpawnEnemy(EnemyData data, Transform[] pathPoints = null)
        {
            if (!_enemyPrefabs.ContainsKey(data.Type))
            {
                RegisterEnemyType(data);
            }

            var pool = _enemyPools[data.Type];
            Enemy enemy;
            if (pool.Count > 0)
            {
                enemy = pool.Dequeue();
            }
            else
            {
                // 动态扩容
                enemy = CreateEnemyInstance(_enemyPrefabs[data.Type]);
            }

            enemy.gameObject.SetActive(true);
            enemy.Initialize(data, pathPoints ?? _pathPoints);
            return enemy;
        }

        /// <summary>
        /// 归还敌人到对象池。
        /// </summary>
        public void ReturnEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null) return;

            var type = enemy.Data.Type;
            if (!_enemyPools.ContainsKey(type))
            {
                Destroy(enemy.gameObject);
                return;
            }

            enemy.Deactivate();
            _enemyPools[type].Enqueue(enemy);
        }

        /// <summary>
        /// 程序化创建敌人预制体（带SpriteRenderer和Enemy组件的GameObject）。
        /// </summary>
        private GameObject CreateEnemyPrefab(EnemyData data)
        {
            var go = new GameObject($"Enemy_{data.Type}");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetEnemySprite(data);
            spriteRenderer.sortingOrder = 5;

            var enemy = go.AddComponent<Enemy>();

            // 因为是运行时创建，不能真正"预制体化"，用模板对象代替
            // 实际使用时通过Instantiate复制
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// 创建敌人实例（从模板复制）。
        /// </summary>
        private Enemy CreateEnemyInstance(GameObject template)
        {
            var go = Instantiate(template, _enemyContainer);
            go.name = template.name;
            var enemy = go.GetComponent<Enemy>();
            go.SetActive(false);
            return enemy;
        }

        /// <summary>
        /// 从素材管理器获取敌人精灵，没有素材则用程序化生成。
        /// </summary>
        private Sprite GetEnemySprite(EnemyData data)
        {
            var sm = SpriteManager.Instance;
            if (sm == null)
                return GenerateEnemySprite(data);

            // 地面敌人按类型选虫子
            switch (data.Type)
            {
                case EnemyType.Fast:
                    return sm.EnemyFast;
                case EnemyType.Normal:
                    return sm.EnemyNormal;
                case EnemyType.Tank:
                    return sm.EnemyTank;
                case EnemyType.Boss:
                    return sm.EnemyTank; // Boss用大虫子
                default:
                    return sm.EnemyNormal;
            }
        }
        /// <summary>
        /// 程序化生成敌人Sprite（不同类型不同形状）。
        /// </summary>
        private Sprite GenerateEnemySprite(EnemyData data)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float radius = size * 0.42f;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            Color bodyColor = data.BodyColor;
            // 高光色（身体上半部分更亮）
            Color highlightColor = Color.Lerp(bodyColor, Color.white, 0.35f);
            // 眼睛颜色
            Color eyeWhite = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color eyePupil = new Color(0.1f, 0.1f, 0.15f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    Color pixel = Color.clear;
                    bool inBody = false;

                    switch (data.Type)
                    {
                        case EnemyType.Normal:
                            inBody = dist <= radius;
                            break;
                        case EnemyType.Fast:
                            float diamondDist = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                            inBody = diamondDist <= radius * 1.1f;
                            break;
                        case EnemyType.Tank:
                            float squareDist = Mathf.Max(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y));
                            inBody = squareDist <= radius * 0.95f;
                            break;
                        case EnemyType.Boss:
                            float angle = Mathf.Atan2(y - center.y, x - center.x);
                            float modulation = 1f + 0.15f * Mathf.Sin(angle * 5f);
                            inBody = dist <= radius * modulation;
                            break;
                    }

                    if (inBody)
                    {
                        // 上半部分高光
                        if (y > center.y + radius * 0.2f)
                            pixel = highlightColor;
                        else
                            pixel = bodyColor;

                        // 抗锯齿边缘
                        float edgeDist = 0f;
                        switch (data.Type)
                        {
                            case EnemyType.Normal: edgeDist = radius - dist; break;
                            case EnemyType.Fast: edgeDist = radius * 1.1f - (Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y)); break;
                            case EnemyType.Tank: edgeDist = radius * 0.95f - Mathf.Max(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y)); break;
                            case EnemyType.Boss:
                                float a = Mathf.Atan2(y - center.y, x - center.x);
                                float m = 1f + 0.15f * Mathf.Sin(a * 5f);
                                edgeDist = radius * m - dist; break;
                        }
                        pixel.a = Mathf.Clamp01(edgeDist * 3f);
                    }

                    // 眼睛（在身体上半部分，两只白色眼睛+黑色瞳孔）
                    float eyeY = center.y + radius * 0.15f;
                    float eyeOffsetX = radius * 0.3f;
                    float eyeRadius = radius * 0.18f;
                    float pupilRadius = radius * 0.1f;

                    // 左眼
                    Vector2 leftEye = new Vector2(center.x - eyeOffsetX, eyeY);
                    if (Vector2.Distance(new Vector2(x, y), leftEye) < eyeRadius)
                        pixel = eyeWhite;
                    if (Vector2.Distance(new Vector2(x, y), leftEye + new Vector2(2, -1)) < pupilRadius)
                        pixel = eyePupil;

                    // 右眼
                    Vector2 rightEye = new Vector2(center.x + eyeOffsetX, eyeY);
                    if (Vector2.Distance(new Vector2(x, y), rightEye) < eyeRadius)
                        pixel = eyeWhite;
                    if (Vector2.Distance(new Vector2(x, y), rightEye + new Vector2(2, -1)) < pupilRadius)
                        pixel = eyePupil;

                    pixels[idx] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>
        /// 清空所有敌人（用于重置）。
        /// </summary>
        public void ClearAllEnemies()
        {
            foreach (Transform child in _enemyContainer)
            {
                var enemy = child.GetComponent<Enemy>();
                if (enemy != null && enemy.IsActive)
                {
                    enemy.Deactivate();
                }
            }
        }
    }
}
