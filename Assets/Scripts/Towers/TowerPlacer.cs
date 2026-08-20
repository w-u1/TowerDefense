using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.UI;
using TowerDefense.Systems;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 防御塔建造管理器。处理塔的放置预览、建造确认、投射物对象池。
    /// </summary>
    public class TowerPlacer : Singleton<TowerPlacer>
    {
        [Header("建造设置")]

        [Tooltip("塔预制体容器")]
        [SerializeField] private Transform _towerContainer;

        [Tooltip("投射物容器")]
        [SerializeField] private Transform _projectileContainer;

        // 投射物对象池
        private readonly Dictionary<TowerType, Queue<Projectile>> _projectilePools = new Dictionary<TowerType, Queue<Projectile>>();
        private readonly Dictionary<TowerType, GameObject> _projectileTemplates = new Dictionary<TowerType, GameObject>();

        // 建造预览
        private GameObject _previewTower;
        private TowerData _selectedTowerData;
        private bool _isPlacing = false;

        // 已建造的塔
        private readonly List<Tower> _builtTowers = new List<Tower>();

        // 路径点（用于禁止在路径上放置）
        private Vector3[] _pathPoints;
        private const float PathClearance = 0.3f; // 路径周围不可放置的半径

        // 摄像机引用
        private Camera _mainCamera;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            _mainCamera = Camera.main;

            if (_towerContainer == null)
            {
                var go = new GameObject("Towers");
                _towerContainer = go.transform;
                _towerContainer.SetParent(transform);
            }
            if (_projectileContainer == null)
            {
                var go = new GameObject("Projectiles");
                _projectileContainer = go.transform;
                _projectileContainer.SetParent(transform);
            }
        }

        /// <summary>
        /// 设置路径点（由启动器调用，用于禁止在路径上放置塔）。
        /// </summary>
        public void SetPath(Vector3[] pathPoints)
        {
            _pathPoints = pathPoints;
        }

        /// <summary>
        /// 开始放置指定类型的塔。
        /// </summary>
        public void StartPlacing(TowerData data)
        {
            if (_isPlacing) CancelPlacing();

            _selectedTowerData = data;
            _isPlacing = true;

            // 创建预览塔
            _previewTower = CreateTowerGameObject(data, isPreview: true);
            _previewTower.name = $"Preview_{data.Type}";
        }

        /// <summary>
        /// 取消放置。
        /// </summary>
        public void CancelPlacing()
        {
            if (_previewTower != null)
            {
                Destroy(_previewTower);
                _previewTower = null;
            }
            _selectedTowerData = null;
            _isPlacing = false;
            // 通知商店清除选中高亮
            if (UIManager.Instance != null && UIManager.Instance.TowerShop != null)
            {
                UIManager.Instance.TowerShop.ClearSelection();
            }
        }

        /// <summary>
        /// 确认建造塔。
        /// </summary>
        public bool ConfirmPlacement(Vector3 position)
        {
            if (!_isPlacing || _selectedTowerData == null) return false;

            // 检查金币
            if (!GameManager.Instance.TrySpendGold(_selectedTowerData.BuildCost))
            {
                return false;
            }

            // 检查位置是否可放置（简化：不与其他塔重叠）
            if (!IsValidPlacement(position))
            {
                GameManager.Instance.AddGold(_selectedTowerData.BuildCost); // 退还
                return false;
            }

            // 创建实际塔
            var towerGo = CreateTowerGameObject(_selectedTowerData, isPreview: false);
            towerGo.transform.position = position;
            towerGo.name = $"Tower_{_selectedTowerData.Type}_{_builtTowers.Count}";

            var tower = towerGo.GetComponent<Tower>();
            tower.Initialize(_selectedTowerData);
            _builtTowers.Add(tower);

            EventBus.Publish(new TowerBuiltEvent
            {
                TowerType = _selectedTowerData.Type.ToString(),
                Position = position
            });

            // 继续放置同类型塔（Shift连续建造）或结束放置
            CancelPlacing();
            return true;
        }

        /// <summary>
        /// 检查位置是否可放置。
        /// </summary>
        private bool IsValidPlacement(Vector3 position)
        {
            // 清理已销毁的塔（出售后留下的null引用）
            _builtTowers.RemoveAll(t => t == null);

            // 不与已有塔重叠
            foreach (var tower in _builtTowers)
            {
                if (tower == null) continue;
                if (Vector3.Distance(tower.transform.position, position) < 0.5f)
                {
                    return false;
                }
            }

            // 不在路径上：检查位置到路径每段线段的距离
            if (_pathPoints != null && _pathPoints.Length >= 2)
            {
                for (int i = 0; i < _pathPoints.Length - 1; i++)
                {
                    float dist = DistanceToSegment(position, _pathPoints[i], _pathPoints[i + 1]);
                    if (dist < PathClearance)
                    {
                        return false;
                    }
                }
            }


            // 不在障碍物上：检查所有障碍物
            var allObstacles = Object.FindObjectsOfType<Obstacle>();
            foreach (var obs in allObstacles)
            {
                if (obs == null) continue;
                if (Vector3.Distance(obs.transform.position, position) < obs.Radius * 0.6f + 0.1f)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 计算点到线段的最短距离。
        /// </summary>
        private static float DistanceToSegment(Vector3 point, Vector3 segStart, Vector3 segEnd)
        {
            Vector3 dir = segEnd - segStart;
            float lenSq = dir.sqrMagnitude;
            if (lenSq < 0.001f) return Vector3.Distance(point, segStart);
            float t = Mathf.Clamp01(Vector3.Dot(point - segStart, dir) / lenSq);
            Vector3 closest = segStart + dir * t;
            return Vector3.Distance(point, closest);
        }

        private void Update()
        {
            // 右键取消障碍物选中
            if (Input.GetMouseButtonDown(1) && Obstacle.Selected != null)
            {
                Obstacle.Selected.Deselect();
            }
            if (_isPlacing && _previewTower != null)
            {
                UpdatePreviewPosition();
                HandlePlacementInput();
            }
            else if (!_isPlacing && Input.GetMouseButtonDown(0))
            {
                // 非放置模式下，点击已建造的塔显示信息面板
                // 排除点击在UI上的情况（否则会关闭面板导致按钮无法点击）
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    TrySelectTower();
                }
            }
        }

        /// <summary>
        /// 尝试点击选中已建造的塔。
        /// </summary>
        private void TrySelectTower()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Tower closest = null;
            float closestDist = 0.7f; // 点击判定半径

            _builtTowers.RemoveAll(t => t == null);
            foreach (var tower in _builtTowers)
            {
                if (tower == null) continue;
                float dist = Vector3.Distance(tower.transform.position, mousePos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = tower;
                }
            }

            if (closest != null)
            {
                if (UIManager.Instance != null && UIManager.Instance.TowerInfo != null)
                {
                    UIManager.Instance.TowerInfo.Show(closest);
                }
            }
            else
            {
                // 点击空白处关闭信息面板
                if (UIManager.Instance != null && UIManager.Instance.TowerInfo != null)
                {
                    UIManager.Instance.TowerInfo.Hide();
                }
            }
        }

        /// <summary>
        /// 更新预览塔位置跟随鼠标（对齐到网格）。
        /// </summary>
        private void UpdatePreviewPosition()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            // 对齐到1单位网格
            mousePos.x = Mathf.Round(mousePos.x);
            mousePos.y = Mathf.Round(mousePos.y);
            _previewTower.transform.position = mousePos;

            // 显示预览范围圈
            var previewRange = _previewTower.transform.Find("RangeIndicator");
            if (previewRange != null)
            {
                previewRange.gameObject.SetActive(true);
                float scale = _selectedTowerData.Range / _selectedTowerData.Size;
                previewRange.localScale = new Vector3(scale, scale, 1f);
            }

            // 颜色指示是否可放置（不影响范围圈颜色）
            bool valid = IsValidPlacement(mousePos) &&
                         GameManager.Instance.CurrentGold >= _selectedTowerData.BuildCost;
            var renderers = _previewTower.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                if (r.GetComponent<Transform>().name == "RangeIndicator") continue;
                r.color = valid ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.6f);
            }
        }

        /// <summary>
        /// 处理放置输入。
        /// </summary>
        private void HandlePlacementInput()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                // 对齐到1单位网格
                mousePos.x = Mathf.Round(mousePos.x);
                mousePos.y = Mathf.Round(mousePos.y);
                ConfirmPlacement(mousePos);
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacing();
            }
        }

        /// <summary>
        /// 创建塔GameObject（使用素材精灵）。
        /// </summary>
        private GameObject CreateTowerGameObject(TowerData data, bool isPreview)
        {
            var go = new GameObject();
            go.transform.SetParent(_towerContainer);

            // 基座阴影
            var platformShadow = new GameObject("PlatformShadow");
            platformShadow.transform.SetParent(go.transform);
            var shadowRenderer = platformShadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GenerateCircleSprite(64, new Color(0f, 0f, 0f, 0.25f));
            shadowRenderer.sortingOrder = 1;
            shadowRenderer.transform.localScale = new Vector3(1.3f, 0.9f, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0.05f, -0.15f, 0);

            // 塔的完整视觉（程序化生成，包含基座+炮塔）
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform);
            var visualRenderer = visual.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = TowerVisualFactory.GetTowerSprite(data.Type);
            visualRenderer.sortingOrder = 3;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 1.5f;

            // 炮口点（在塔的顶部，炮塔朝向目标时旋转）
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(visual.transform);
            muzzle.transform.localPosition = new Vector3(0, 0.45f, 0);

            // 范围指示器
            var rangeIndicator = new GameObject("RangeIndicator");
            rangeIndicator.transform.SetParent(go.transform);
            var rangeRenderer = rangeIndicator.AddComponent<SpriteRenderer>();
            rangeRenderer.sprite = GenerateRangeSprite();
            rangeRenderer.color = Color.white;
            rangeRenderer.sortingOrder = 9;
            rangeIndicator.SetActive(false);

            if (!isPreview)
            {
                go.AddComponent<Tower>();
            }

            go.transform.localScale = Vector3.one * data.Size;
            return go;
        }


        /// <summary>
        /// 生成圆形Sprite（带抗锯齿边缘）。
        /// </summary>
        private Sprite GenerateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) * 2.5f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }


        /// <summary>
        /// 生成范围指示Sprite。
        /// </summary>
        private Sprite GenerateRangeSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.5f; // 精确填满Sprite，确保scale=range*2时半径=range

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;

                    if (dist < radius)
                    {
                        // 内部半透明填充（径向渐变，中心更淡边缘更明显）
                        float fillAlpha = 0.15f + (dist / radius) * 0.25f;
                        pixels[idx] = new Color(0.15f, 0.65f, 1f, fillAlpha);
                    }
                    else if (dist < radius + 6f)
                    {
                        // 边框（更粗更亮，抗锯齿）
                        float edgeAlpha = Mathf.Clamp01(1f - (dist - radius) / 6f);
                        pixels[idx] = new Color(0.2f, 0.8f, 1f, 0.95f * edgeAlpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        #region 投射物对象池

        /// <summary>
        /// 生成投射物。
        /// </summary>
        public void SpawnProjectile(TowerData data, Enemy target, Vector3 startPos, float damage)
        {
            if (!_projectileTemplates.ContainsKey(data.Type))
            {
                CreateProjectileTemplate(data);
            }

            var pool = _projectilePools[data.Type];
            Projectile proj;
            if (pool.Count > 0)
            {
                proj = pool.Dequeue();
            }
            else
            {
                proj = CreateProjectileInstance(_projectileTemplates[data.Type]);
            }

            proj.Initialize(data, target, startPos, damage);
        }

        /// <summary>
        /// 归还投射物。
        /// </summary>
        public void ReturnProjectile(Projectile proj)
        {
            if (proj == null) return;
            var type = proj.GetComponent<ProjectileTypeTag>()?.Type ?? TowerType.Archer;
            if (_projectilePools.ContainsKey(type))
            {
                proj.ForceDeactivate();
                _projectilePools[type].Enqueue(proj);
            }
            else
            {
                Destroy(proj.gameObject);
            }
        }

        private void CreateProjectileTemplate(TowerData data)
        {
            var go = new GameObject($"Projectile_{data.Type}");
            go.transform.SetParent(_projectileContainer);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GenerateProjectileSprite(data);
            renderer.color = data.ProjectileColor;
            renderer.sortingOrder = 6;
            var proj = go.AddComponent<Projectile>();
            var tag = go.AddComponent<ProjectileTypeTag>();
            tag.Type = data.Type;
            go.SetActive(false);

            _projectileTemplates[data.Type] = go;
            _projectilePools[data.Type] = new Queue<Projectile>();

            // 预生成几个
            for (int i = 0; i < 10; i++)
            {
                var instance = CreateProjectileInstance(go);
                _projectilePools[data.Type].Enqueue(instance);
            }
        }

        private Projectile CreateProjectileInstance(GameObject template)
        {
            var go = Instantiate(template, _projectileContainer);
            go.name = template.name;
            var proj = go.GetComponent<Projectile>();
            go.SetActive(false);
            return proj;
        }

        private Sprite GenerateProjectileSprite(TowerData data)
        {
            return ProjectileVisualFactory.GetProjectileSprite(data.Type);
        }

        #endregion

        /// <summary>
        /// 获取所有已建造的塔。
        /// </summary>
        public IReadOnlyList<Tower> GetBuiltTowers() => _builtTowers;

        /// <summary>
        /// 清空所有塔和投射物（重置用）。
        /// </summary>
        public void ClearAll()
        {
            foreach (var tower in _builtTowers)
            {
                if (tower != null) Destroy(tower.gameObject);
            }
            _builtTowers.Clear();

            foreach (Transform child in _projectileContainer)
            {
                Destroy(child.gameObject);
            }
            _projectilePools.Clear();
            _projectileTemplates.Clear();

            CancelPlacing();
        }
    }

    /// <summary>
    /// 投射物类型标签（用于对象池归还时识别类型）。
    /// </summary>
    public class ProjectileTypeTag : MonoBehaviour
    {
        public TowerType Type;
    }
}
