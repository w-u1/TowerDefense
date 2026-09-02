using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Systems;
using TowerDefense.Towers;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    /// <summary>
    /// 游戏启动器。挂在场景中一个空物体上即可运行，无需手动搭建任何场景内容。
    /// 程序化生成：地图、路径点、敌人/塔数据配置、波次配置、所有系统组件、UI。
    /// 
    /// 使用方法：
    /// 1. 创建一个空场景
    /// 2. 创建空GameObject，命名为"GameBootstrapper"
    /// 3. 挂载此脚本
    /// 4. 点击Play即可运行
    /// </summary>
    public class GameBootstrapper : Singleton<GameBootstrapper>
    {
        [Header("地图设置")]
        [Tooltip("地图宽度")]
        [SerializeField] private float _mapWidth = 20f;

        [Tooltip("地图高度")]
        [SerializeField] private float _mapHeight = 12f;

        [Tooltip("路径点（世界坐标），留空则使用默认S形路径")]
        [SerializeField] private Vector3[] _customPathPoints;

        /// <summary>当前关卡（1-3）。</summary>
        public static int CurrentLevel = 1;

        // 运行时引用
        private Transform _pathRoot;
        private Transform _mapRoot;
        private Transform[] _pathPoints;
        private List<EnemyData> _enemyDatas;
        private List<TowerData> _towerDatas;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            // 确保在最前面初始化
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            BootstrapGame();
        }

        /// <summary>
        /// 启动游戏全流程。
        /// </summary>
        public void BootstrapGame()
        {
            Debug.Log("[GameBootstrapper] 初始化基础...");

            // 1. 创建摄像机
            SetupCamera();

            // 2. 初始化UI（包含关卡选择面板）
            UIManager.Instance.InitializeUIBase();

            // 3. 显示关卡选择，选择后才 StartGame()
            UIManager.Instance.LevelSelect.Show();
        }

        /// <summary>
        /// 选择关卡后真正开始游戏。
        /// </summary>
        public void StartGame()
        {
            Debug.Log($"[GameBootstrapper] 开始第{CurrentLevel}关...");

            // 创建地图和路径
            CreateMapAndPath();

            // 创建游戏数据
            CreateGameData();

            // 初始化系统
            InitializeSystems();

            // 初始化游戏UI（HUD等）
            InitializeGameUI();

            // 注册敌人类型
            RegisterEnemyTypes();

            // 设置波次
            SetupWaves();

            // 进入准备状态
            GameManager.Instance.ChangeState(GameState.Preparation);

            // 添加可交互奖励
            CreateInteractiveRewards();

            Debug.Log("[GameBootstrapper] 游戏开始！");
        }

        /// <summary>
        /// 在地图上放置可点击的金币奖励。
        /// </summary>
        private void CreateInteractiveRewards()
        {
            var rewardRoot = new GameObject("InteractiveRewards");
            rewardRoot.transform.SetParent(transform);
            var rng = new System.Random(99);

            for (int i = 0; i < 5; i++)
            {
                float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.8f;
                float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.7f;
                // 避免在路径上
                if (_pathPoints != null)
                {
                    bool nearPath = false;
                    foreach (var p in _pathPoints)
                    {
                        if (Vector3.Distance(new Vector3(x, y, 0), p.position) < 1.5f) { nearPath = true; break; }
                    }
                    if (nearPath) continue;
                }

                var reward = new GameObject($"GoldBag_{i}");
                reward.transform.SetParent(rewardRoot.transform);
                reward.transform.position = new Vector3(x, y, 0);
                var sr = reward.AddComponent<SpriteRenderer>();
                sr.sprite = GenerateCircleSprite(24, new Color(1f, 0.85f, 0.2f, 0.9f));
                sr.sortingOrder = 5;
                reward.transform.localScale = Vector3.one * 0.4f;

                // 添加点击检测
                var clickable = reward.AddComponent<ClickableReward>();
                clickable.Init(50 + i * 10);
            }
        }

        /// <summary>
        /// 设置摄像机。
        /// </summary>
        private void SetupCamera()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = _mapHeight * 0.6f;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
        }

        /// <summary>
        /// 创建地图背景和路径。
        /// </summary>
        private void CreateMapAndPath()
        {
            // 所有地图内容放到一个根节点下，方便重置时整体销毁
            var mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(transform);
            _mapRoot = mapRoot.transform;

            // 天空渐变背景（比地图大，在最底层）
            var skyGo = new GameObject("SkyBackground");
            skyGo.transform.SetParent(_mapRoot);
            var skyRenderer = skyGo.AddComponent<SpriteRenderer>();
            skyRenderer.sprite = GenerateSkySprite();
            skyRenderer.sortingOrder = -10;
            skyGo.transform.position = Vector3.zero;
            skyGo.transform.localScale = new Vector3(_mapWidth * 1.8f, _mapHeight * 1.8f, 1f);

            // 地图背景
            var mapGo = new GameObject("MapBackground");
            mapGo.transform.SetParent(_mapRoot);
            var mapRenderer = mapGo.AddComponent<SpriteRenderer>();
            mapRenderer.sprite = GenerateMapSprite();
            mapRenderer.sortingOrder = 0;
            mapGo.transform.position = Vector3.zero;

            // 建造网格线（帮助玩家定位）
            DrawBuildGrid();
            DrawMapBorder();

            // 路径
            _pathRoot = new GameObject("Path").transform;
            _pathRoot.SetParent(_mapRoot);

            Vector3[] pathPositions;
            if (_customPathPoints != null && _customPathPoints.Length > 0)
            {
                pathPositions = _customPathPoints;
            }
            else
            {
                pathPositions = GetLevelPath(CurrentLevel);
            }

            _pathPoints = new Transform[pathPositions.Length];
            for (int i = 0; i < pathPositions.Length; i++)
            {
                var pointGo = new GameObject($"PathPoint_{i}");
                pointGo.transform.SetParent(_pathRoot);
                pointGo.transform.position = pathPositions[i];
                _pathPoints[i] = pointGo.transform;
            }

            // 绘制路径线（可视化）
            DrawPathVisual(pathPositions);

            // 起点和终点标记
            CreateEndpointMarker(pathPositions[0], "起点", new Color(0.3f, 1f, 0.3f));
            CreateEndpointMarker(pathPositions[pathPositions.Length - 1], "终点", new Color(1f, 0.3f, 0.3f));

            // 添加地图装饰
            CreateMapDecorations(pathPositions);
        }

        /// <summary>
        /// 根据关卡编号返回路径点。
        /// </summary>
        private Vector3[] GetLevelPath(int level)
        {
            float w = _mapWidth / 2f + 1;
            switch (level)
            {
                case 1:
                    // 第一关：S形（经典）
                    return new Vector3[]
                    {
                        new Vector3(-w, 3f, 0),
                        new Vector3(-6f, 3f, 0),
                        new Vector3(-6f, -2f, 0),
                        new Vector3(0f, -2f, 0),
                        new Vector3(0f, 3f, 0),
                        new Vector3(6f, 3f, 0),
                        new Vector3(6f, -2f, 0),
                        new Vector3(w, -2f, 0)
                    };
                case 2:
                    // 第二关：之字形（更长更曲折）
                    return new Vector3[]
                    {
                        new Vector3(-w, 4f, 0),
                        new Vector3(-7f, 4f, 0),
                        new Vector3(-7f, -4f, 0),
                        new Vector3(-3f, -4f, 0),
                        new Vector3(-3f, 1f, 0),
                        new Vector3(2f, 1f, 0),
                        new Vector3(2f, -4f, 0),
                        new Vector3(7f, -4f, 0),
                        new Vector3(7f, 4f, 0),
                        new Vector3(w, 4f, 0)
                    };
                case 3:
                    // 第三关：环形包围（最短但最难防守）
                    return new Vector3[]
                    {
                        new Vector3(-w, 0f, 0),
                        new Vector3(-8f, 0f, 0),
                        new Vector3(-8f, 4f, 0),
                        new Vector3(0f, 4f, 0),
                        new Vector3(0f, -4f, 0),
                        new Vector3(-4f, -4f, 0),
                        new Vector3(-4f, 0f, 0),
                        new Vector3(4f, 0f, 0),
                        new Vector3(4f, 4f, 0),
                        new Vector3(8f, 4f, 0),
                        new Vector3(8f, -4f, 0),
                        new Vector3(w, -4f, 0)
                    };
                default:
                    return GetLevelPath(1);
            }
        }

        /// <summary>
        /// 在地图上随机放置树木、岩石、云朵等装饰。
        /// </summary>
        private void CreateMapDecorations(Vector3[] pathPositions)
        {
            var decoRoot = new GameObject("Decorations");
            decoRoot.transform.SetParent(_mapRoot);
            var rng = new System.Random(123);

            // 根据关卡选择装饰类型
            if (CurrentLevel == 1)
            {
                // 草原：绿树
                for (int i = 0; i < 15; i++)
                {
                    float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                    float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                    if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.8f)) continue;
                    CreateTree(decoRoot.transform, x, y, ref rng);
                }
            }
            else if (CurrentLevel == 2)
            {
                // 沙漠：仙人掌
                for (int i = 0; i < 12; i++)
                {
                    float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                    float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                    if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.8f)) continue;
                    CreateCactus(decoRoot.transform, x, y, ref rng);
                }
                // 枯骨
                for (int i = 0; i < 5; i++)
                {
                    float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                    float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                    if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.7f)) continue;
                    CreateBones(decoRoot.transform, x, y, ref rng);
                }
            }
            else
            {
                // 冰雪：冰晶
                for (int i = 0; i < 12; i++)
                {
                    float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                    float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                    if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.8f)) continue;
                    CreateIceCrystal(decoRoot.transform, x, y, ref rng);
                }
                // 雪人
                for (int i = 0; i < 5; i++)
                {
                    float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                    float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                    if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.7f)) continue;
                    CreateSnowman(decoRoot.transform, x, y, ref rng);
                }
            }

            // 岩石（所有关卡都有）
            for (int i = 0; i < 6; i++)
            {
                float x = (float)(rng.NextDouble() - 0.5) * _mapWidth * 0.9f;
                float y = (float)(rng.NextDouble() - 0.5) * _mapHeight * 0.9f;
                if (IsNearPath(new Vector3(x, y, 0), pathPositions, 0.7f)) continue;
                var rock = new GameObject($"Rock_{i}");
                rock.transform.SetParent(decoRoot.transform);
                rock.transform.position = new Vector3(x, y, 0);
                var rockR = rock.AddComponent<SpriteRenderer>();
                float gray = 0.4f + (float)rng.NextDouble() * 0.2f;
                rockR.sprite = GenerateCircleSprite(24, new Color(gray, gray, gray * 0.9f, 1f));
                rockR.sortingOrder = 1;
                rock.transform.localScale = new Vector3(0.3f + (float)rng.NextDouble() * 0.2f, 0.25f + (float)rng.NextDouble() * 0.15f, 1f);
            }

            // 天空中漂浮的云朵（在地图上方，不影响游戏）
            for (int i = 0; i < 5; i++)
            {
                float x = (float)(rng.NextDouble() - 0.5f) * _mapWidth * 1.2f;
                float y = _mapHeight / 2 + 1f + (float)rng.NextDouble() * 2f;
                var cloud = new GameObject($"Cloud_{i}");
                cloud.transform.SetParent(decoRoot.transform);
                cloud.transform.position = new Vector3(x, y, 0);
                var cloudR = cloud.AddComponent<SpriteRenderer>();
                cloudR.sprite = GenerateCircleSprite(32, new Color(1f, 1f, 1f, 0.7f));
                cloudR.sortingOrder = -5;
                cloud.transform.localScale = new Vector3(1.5f + (float)rng.NextDouble(), 0.6f, 1f);
            }
        }

        /// <summary>
        /// 检查点是否在路径附近。
        /// </summary>
        private bool IsNearPath(Vector3 pos, Vector3[] path, float threshold)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                // 点到线段距离
                Vector3 dir = path[i + 1] - path[i];
                float lenSq = dir.sqrMagnitude;
                if (lenSq < 0.001f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(pos - path[i], dir) / lenSq);
                Vector3 closest = path[i] + dir * t;
                if (Vector3.Distance(pos, closest) < threshold) return true;
            }
            return false;
        }

        private void CreateTree(Transform parent, float x, float y, ref System.Random rng)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = new Vector3(x, y, 0);
            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, -0.2f, 0);
            var trunkR = trunk.AddComponent<SpriteRenderer>();
            trunkR.sprite = GenerateCircleSprite(16, new Color(0.45f, 0.3f, 0.15f, 1f));
            trunkR.sortingOrder = 1;
            trunk.transform.localScale = new Vector3(0.15f, 0.3f, 1f);
            var leaves = new GameObject("Leaves");
            leaves.transform.SetParent(tree.transform);
            leaves.transform.localPosition = new Vector3(0, 0.15f, 0);
            var leavesR = leaves.AddComponent<SpriteRenderer>();
            float green = 0.35f + (float)rng.NextDouble() * 0.15f;
            leavesR.sprite = GenerateCircleSprite(32, new Color(green, green + 0.2f, green * 0.7f, 1f));
            leavesR.sortingOrder = 2;
            leaves.transform.localScale = Vector3.one * (0.7f + (float)rng.NextDouble() * 0.4f);
        }

        private void CreateCactus(Transform parent, float x, float y, ref System.Random rng)
        {
            var cactus = new GameObject("Cactus");
            cactus.transform.SetParent(parent);
            cactus.transform.position = new Vector3(x, y, 0);
            // 主体（绿色椭圆）
            var body = new GameObject("Body");
            body.transform.SetParent(cactus.transform);
            var bodyR = body.AddComponent<SpriteRenderer>();
            bodyR.sprite = GenerateCircleSprite(32, new Color(0.3f, 0.55f, 0.25f, 1f));
            bodyR.sortingOrder = 2;
            body.transform.localScale = new Vector3(0.25f, 0.6f, 1f);
            // 手臂
            var arm = new GameObject("Arm");
            arm.transform.SetParent(cactus.transform);
            var armR = arm.AddComponent<SpriteRenderer>();
            armR.sprite = GenerateCircleSprite(16, new Color(0.35f, 0.6f, 0.3f, 1f));
            armR.sortingOrder = 2;
            arm.transform.localPosition = new Vector3(0.2f, 0.1f, 0);
            arm.transform.localScale = new Vector3(0.15f, 0.3f, 1f);
        }

        private void CreateBones(Transform parent, float x, float y, ref System.Random rng)
        {
            var bones = new GameObject("Bones");
            bones.transform.SetParent(parent);
            bones.transform.position = new Vector3(x, y, 0);
            var skull = new GameObject("Skull");
            skull.transform.SetParent(bones.transform);
            var skullR = skull.AddComponent<SpriteRenderer>();
            skullR.sprite = GenerateCircleSprite(20, new Color(0.95f, 0.92f, 0.8f, 1f));
            skullR.sortingOrder = 2;
            skull.transform.localScale = Vector3.one * 0.4f;
        }

        private void CreateIceCrystal(Transform parent, float x, float y, ref System.Random rng)
        {
            var crystal = new GameObject("IceCrystal");
            crystal.transform.SetParent(parent);
            crystal.transform.position = new Vector3(x, y, 0);
            var sr = crystal.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateDiamondSprite(32);
            sr.sortingOrder = 2;
            float s = 0.5f + (float)rng.NextDouble() * 0.3f;
            crystal.transform.localScale = new Vector3(s * 0.6f, s, 1f);
        }

        private void CreateSnowman(Transform parent, float x, float y, ref System.Random rng)
        {
            var snowman = new GameObject("Snowman");
            snowman.transform.SetParent(parent);
            snowman.transform.position = new Vector3(x, y, 0);
            // 身体
            var body = new GameObject("Body");
            body.transform.SetParent(snowman.transform);
            body.transform.localPosition = new Vector3(0, -0.15f, 0);
            var bodyR = body.AddComponent<SpriteRenderer>();
            bodyR.sprite = GenerateCircleSprite(32, new Color(0.95f, 0.97f, 1f, 1f));
            bodyR.sortingOrder = 2;
            body.transform.localScale = Vector3.one * 0.5f;
            // 头
            var head = new GameObject("Head");
            head.transform.SetParent(snowman.transform);
            head.transform.localPosition = new Vector3(0, 0.25f, 0);
            var headR = head.AddComponent<SpriteRenderer>();
            headR.sprite = GenerateCircleSprite(24, new Color(0.95f, 0.97f, 1f, 1f));
            headR.sortingOrder = 2;
            head.transform.localScale = Vector3.one * 0.35f;
        }

        private Sprite GenerateDiamondSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Abs(x - center.x) / center.x + Mathf.Abs(y - center.y) / center.y;
                    if (dist <= 1f)
                    {
                        float alpha = 1f - Mathf.Max(0, dist - 0.7f) / 0.3f;
                        tex.SetPixel(x, y, new Color(0.7f, 0.9f, 1f, alpha * 0.9f));
                    }
                    else tex.SetPixel(x, y, Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 绘制建造网格线（淡色，帮助玩家定位建塔位置）。
        /// </summary>
        private void DrawBuildGrid()
        {
            var gridRoot = new GameObject("BuildGrid");
            gridRoot.transform.SetParent(transform);

            float gridSize = 1f; // 每格1单位
            Color gridColor = new Color(1f, 1f, 1f, 0.06f);

            // 垂直线
            for (float x = -_mapWidth / 2; x <= _mapWidth / 2; x += gridSize)
            {
                var line = new GameObject($"VLine_{x:F1}");
                line.transform.SetParent(gridRoot.transform);
                line.transform.position = new Vector3(x, 0, 0);
                var renderer = line.AddComponent<SpriteRenderer>();
                renderer.sprite = GeneratePathSegmentSprite();
                renderer.color = gridColor;
                renderer.sortingOrder = 0;
                line.transform.localScale = new Vector3(0.03f, _mapHeight, 1);
                line.transform.rotation = Quaternion.Euler(0, 0, 90);
            }

            // 水平线
            for (float y = -_mapHeight / 2; y <= _mapHeight / 2; y += gridSize)
            {
                var line = new GameObject($"HLine_{y:F1}");
                line.transform.SetParent(gridRoot.transform);
                line.transform.position = new Vector3(0, y, 0);
                var renderer = line.AddComponent<SpriteRenderer>();
                renderer.sprite = GeneratePathSegmentSprite();
                renderer.color = gridColor;
                renderer.sortingOrder = 0;
                line.transform.localScale = new Vector3(_mapWidth, 0.03f, 1);
            }
        }

        /// <summary>
        /// 绘制地图边界围栏（木栅栏风格）。
        /// </summary>
        private void DrawMapBorder()
        {
            var borderRoot = new GameObject("MapBorder");
            borderRoot.transform.SetParent(transform);

            Color fenceColor = new Color(0.5f, 0.35f, 0.18f, 0.9f);
            float margin = 0.15f;

            // 四条边
            CreateBorderLine(borderRoot.transform, new Vector3(0, _mapHeight / 2 + margin, 0),
                new Vector3(_mapWidth + 0.5f, 0.25f, 1), fenceColor);
            CreateBorderLine(borderRoot.transform, new Vector3(0, -_mapHeight / 2 - margin, 0),
                new Vector3(_mapWidth + 0.5f, 0.25f, 1), fenceColor);
            CreateBorderLine(borderRoot.transform, new Vector3(-_mapWidth / 2 - margin, 0, 0),
                new Vector3(0.25f, _mapHeight + 0.5f, 1), fenceColor);
            CreateBorderLine(borderRoot.transform, new Vector3(_mapWidth / 2 + margin, 0, 0),
                new Vector3(0.25f, _mapHeight + 0.5f, 1), fenceColor);

            // 四角装饰柱
            Vector3[] corners = {
                new Vector3(-_mapWidth / 2 - margin, _mapHeight / 2 + margin, 0),
                new Vector3(_mapWidth / 2 + margin, _mapHeight / 2 + margin, 0),
                new Vector3(-_mapWidth / 2 - margin, -_mapHeight / 2 - margin, 0),
                new Vector3(_mapWidth / 2 + margin, -_mapHeight / 2 - margin, 0)
            };
            foreach (var corner in corners)
            {
                var post = new GameObject("CornerPost");
                post.transform.SetParent(borderRoot.transform);
                post.transform.position = corner;
                var renderer = post.AddComponent<SpriteRenderer>();
                renderer.sprite = GenerateCircleSprite(32, new Color(0.6f, 0.42f, 0.22f, 1f));
                renderer.sortingOrder = 1;
                post.transform.localScale = Vector3.one * 0.4f;
            }
        }

        private void CreateBorderLine(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var line = new GameObject("BorderLine");
            line.transform.SetParent(parent);
            line.transform.position = pos;
            line.transform.localScale = scale;
            var renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratePathSegmentSprite();
            renderer.color = color;
            renderer.sortingOrder = 1;
        }

        /// <summary>
        /// 绘制路径可视化（底层宽路径 + 顶层窄路径，形成边框效果）。
        /// </summary>
        private void DrawPathVisual(Vector3[] points)
        {
            var pathVisual = new GameObject("PathVisual");
            pathVisual.transform.SetParent(_pathRoot);

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 mid = (start + end) / 2f;
                float length = Vector3.Distance(start, end);
                float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

                // 底层：路径边框（深色，更宽）
                var border = new GameObject($"Border_{i}");
                border.transform.SetParent(pathVisual.transform);
                border.transform.position = mid;
                border.transform.rotation = Quaternion.Euler(0, 0, angle);
                var borderRenderer = border.AddComponent<SpriteRenderer>();
                borderRenderer.sprite = GeneratePathSegmentSprite();
                borderRenderer.color = new Color(0.45f, 0.32f, 0.18f, 0.95f);
                borderRenderer.sortingOrder = 1;
                border.transform.localScale = new Vector3(length, 1.15f, 1);

                // 顶层：路径主体（浅色，稍窄）
                var segment = new GameObject($"Segment_{i}");
                segment.transform.SetParent(pathVisual.transform);
                segment.transform.position = mid;
                segment.transform.rotation = Quaternion.Euler(0, 0, angle);
                var renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = GeneratePathSegmentSprite();
                renderer.color = new Color(0.72f, 0.55f, 0.35f, 0.92f);
                renderer.sortingOrder = 2;
                segment.transform.localScale = new Vector3(length, 0.85f, 1);
            }

            // 路径转弯处添加圆形补丁，消除缝隙
            for (int i = 1; i < points.Length - 1; i++)
            {
                var patch = new GameObject($"CornerPatch_{i}");
                patch.transform.SetParent(pathVisual.transform);
                patch.transform.position = points[i];
                var patchRenderer = patch.AddComponent<SpriteRenderer>();
                patchRenderer.sprite = GenerateCircleSprite(32, new Color(0.72f, 0.55f, 0.35f, 0.92f));
                patchRenderer.sortingOrder = 2;
                patch.transform.localScale = Vector3.one * 0.9f;
            }

            // 路径上的小石子装饰（随机散布）
            System.Random rng = new System.Random(42);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                float length = Vector3.Distance(start, end);
                int pebbleCount = Mathf.FloorToInt(length * 1.5f);
                for (int j = 0; j < pebbleCount; j++)
                {
                    float t = (float)rng.NextDouble();
                    Vector3 pos = Vector3.Lerp(start, end, t);
                    // 横向偏移（在路径宽度内）
                    Vector3 dir = (end - start).normalized;
                    Vector3 perp = new Vector3(-dir.y, dir.x, 0);
                    pos += perp * (float)(rng.NextDouble() - 0.5) * 0.5f;

                    var pebble = new GameObject($"Pebble_{i}_{j}");
                    pebble.transform.SetParent(pathVisual.transform);
                    pebble.transform.position = pos;
                    var pebbleRenderer = pebble.AddComponent<SpriteRenderer>();
                    float gray = 0.4f + (float)rng.NextDouble() * 0.2f;
                    pebbleRenderer.sprite = GenerateCircleSprite(16, new Color(gray, gray * 0.9f, gray * 0.75f, 0.6f));
                    pebbleRenderer.sortingOrder = 3;
                    pebble.transform.localScale = Vector3.one * (0.08f + (float)rng.NextDouble() * 0.06f);
                }
            }
        }

        private void CreateEndpointMarker(Vector3 pos, string label, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(_pathRoot);
            go.transform.position = pos;

            bool isStart = label == "起点";

            // 多层光圈（脉冲效果通过不同大小的半透明圆实现）
            for (int i = 0; i < 3; i++)
            {
                var ring = new GameObject($"Ring_{i}");
                ring.transform.SetParent(go.transform);
                ring.transform.localPosition = Vector3.zero;
                var ringRenderer = ring.AddComponent<SpriteRenderer>();
                ringRenderer.sprite = GenerateCircleSprite(64, new Color(color.r, color.g, color.b, 0.15f - i * 0.04f));
                ringRenderer.sortingOrder = 2;
                ring.transform.localScale = Vector3.one * (1.2f + i * 0.4f);
                // 添加旋转动画
                var rotator = ring.AddComponent<EndpointRotator>();
                rotator.speed = (i + 1) * 20f * (i % 2 == 0 ? 1 : -1);
            }

            // 核心圆
            var core = new GameObject("Core");
            core.transform.SetParent(go.transform);
            core.transform.localPosition = Vector3.zero;
            var coreRenderer = core.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = GenerateCircleSprite(64, color);
            coreRenderer.sortingOrder = 3;
            core.transform.localScale = Vector3.one * 0.7f;

            // 核心高光
            var highlight = new GameObject("Highlight");
            highlight.transform.SetParent(go.transform);
            highlight.transform.localPosition = new Vector3(-0.1f, 0.1f, 0);
            var hlRenderer = highlight.AddComponent<SpriteRenderer>();
            hlRenderer.sprite = GenerateCircleSprite(32, new Color(1f, 1f, 1f, 0.4f));
            hlRenderer.sortingOrder = 4;
            highlight.transform.localScale = Vector3.one * 0.35f;

            // 起点：箭头标识；终点：基地标识
            if (isStart)
            {
                // 箭头（指向路径方向）
                var arrow = new GameObject("Arrow");
                arrow.transform.SetParent(go.transform);
                arrow.transform.localPosition = Vector3.zero;
                var arrowRenderer = arrow.AddComponent<SpriteRenderer>();
                arrowRenderer.sprite = GenerateArrowSprite();
                arrowRenderer.color = Color.white;
                arrowRenderer.sortingOrder = 5;
                arrow.transform.localScale = Vector3.one * 0.5f;
                arrow.transform.localRotation = Quaternion.Euler(0, 0, -90);
            }
            else
            {
                // 旗帜
                var pole = new GameObject("Pole");
                pole.transform.SetParent(go.transform);
                pole.transform.localPosition = new Vector3(0.2f, 0.3f, 0);
                var poleRenderer = pole.AddComponent<SpriteRenderer>();
                poleRenderer.sprite = GenerateRectSprite();
                poleRenderer.color = new Color(0.5f, 0.35f, 0.2f, 1f);
                poleRenderer.sortingOrder = 5;
                pole.transform.localScale = new Vector3(0.08f, 0.6f, 1f);

                var flag = new GameObject("Flag");
                flag.transform.SetParent(go.transform);
                flag.transform.localPosition = new Vector3(0.35f, 0.45f, 0);
                var flagRenderer = flag.AddComponent<SpriteRenderer>();
                flagRenderer.sprite = GenerateFlagSprite();
                flagRenderer.color = color;
                flagRenderer.sortingOrder = 5;
                flag.transform.localScale = Vector3.one * 0.4f;
            }

            // 文字标签
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0, -0.9f, 0);
            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 56;
            textMesh.characterSize = 0.07f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;
            var meshRenderer = labelGo.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 6;
        }

        /// <summary>
        /// 生成箭头Sprite。
        /// </summary>
        private Sprite GenerateArrowSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    // 简单箭头形状
                    bool inShaft = x > size * 0.35f && x < size * 0.5f && y > size * 0.15f && y < size * 0.85f;
                    bool inHead = x > size * 0.35f && x < size * 0.85f &&
                                   Mathf.Abs(y - size / 2f) < (x - size * 0.35f) * 0.7f;
                    pixels[idx] = (inShaft || inHead) ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 生成矩形Sprite。
        /// </summary>
        private Sprite GenerateRectSprite()
        {
            int size = 8;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 生成旗帜Sprite。
        /// </summary>
        private Sprite GenerateFlagSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    // 三角形旗帜
                    bool inFlag = x < size * 0.85f && y > size * 0.2f && y < size * 0.8f &&
                                  (x < size * 0.3f || y > size * 0.5f - (x - size * 0.3f) * 0.5f);
                    pixels[idx] = inFlag ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), size);
        }

        /// <summary>
        /// 创建游戏数据（敌人和塔的ScriptableObject配置）。
        /// </summary>
        private void CreateGameData()
        {
            _enemyDatas = new List<EnemyData>();
            _towerDatas = new List<TowerData>();

            // ===== 敌人数据 =====
            // 普通敌人
            var normalEnemy = ScriptableObject.CreateInstance<EnemyData>();
            normalEnemy.name = "EnemyData_Normal";
            normalEnemy.Type = EnemyType.Normal;
            normalEnemy.DisplayName = "普通敌人";
            normalEnemy.MaxHealth = 80;
            normalEnemy.MoveSpeed = 1.8f;
            normalEnemy.Damage = 1;
            normalEnemy.RewardGold = 10;
            normalEnemy.BodyColor = new Color(0.9f, 0.35f, 0.35f);
            normalEnemy.Size = 0.7f;
            normalEnemy.Armor = 0f;
            _enemyDatas.Add(normalEnemy);

            // 快速敌人
            var fastEnemy = ScriptableObject.CreateInstance<EnemyData>();
            fastEnemy.name = "EnemyData_Fast";
            fastEnemy.Type = EnemyType.Fast;
            fastEnemy.DisplayName = "快速敌人";
            fastEnemy.MaxHealth = 40;
            fastEnemy.MoveSpeed = 3.5f;
            fastEnemy.Damage = 1;
            fastEnemy.RewardGold = 15;
            fastEnemy.BodyColor = new Color(1f, 0.78f, 0.25f);
            fastEnemy.Size = 0.55f;
            fastEnemy.Armor = 0f;
            _enemyDatas.Add(fastEnemy);

            // 坦克敌人
            var tankEnemy = ScriptableObject.CreateInstance<EnemyData>();
            tankEnemy.name = "EnemyData_Tank";
            tankEnemy.Type = EnemyType.Tank;
            tankEnemy.DisplayName = "坦克敌人";
            tankEnemy.MaxHealth = 300;
            tankEnemy.MoveSpeed = 1.0f;
            tankEnemy.Damage = 3;
            tankEnemy.RewardGold = 30;
            tankEnemy.BodyColor = new Color(0.5f, 0.55f, 0.65f);
            tankEnemy.Size = 1.0f;
            tankEnemy.Armor = 0.3f;
            _enemyDatas.Add(tankEnemy);

            // Boss
            var bossEnemy = ScriptableObject.CreateInstance<EnemyData>();
            bossEnemy.name = "EnemyData_Boss";
            bossEnemy.Type = EnemyType.Boss;
            bossEnemy.DisplayName = "Boss";
            bossEnemy.MaxHealth = 1500;
            bossEnemy.MoveSpeed = 0.8f;
            bossEnemy.Damage = 10;
            bossEnemy.RewardGold = 200;
            bossEnemy.BodyColor = new Color(0.7f, 0.2f, 0.8f);
            bossEnemy.Size = 1.4f;
            bossEnemy.Armor = 0.2f;
            _enemyDatas.Add(bossEnemy);

            // 精英敌人：高血量高奖励，紫色中型
            var eliteEnemy = ScriptableObject.CreateInstance<EnemyData>();
            eliteEnemy.name = "EnemyData_Elite";
            eliteEnemy.Type = EnemyType.Elite;
            eliteEnemy.DisplayName = "精英";
            eliteEnemy.MaxHealth = 500;
            eliteEnemy.MoveSpeed = 1.2f;
            eliteEnemy.Damage = 5;
            eliteEnemy.RewardGold = 80;
            eliteEnemy.BodyColor = new Color(0.6f, 0.2f, 0.9f);
            eliteEnemy.Size = 1.1f;
            eliteEnemy.Armor = 0.15f;
            _enemyDatas.Add(eliteEnemy);

            // ===== 塔数据 =====
            // 箭塔：快速单体，射速快，擅长清理小怪
            var archerTower = ScriptableObject.CreateInstance<TowerData>();
            archerTower.name = "TowerData_Archer";
            archerTower.Type = TowerType.Archer;
            archerTower.DisplayName = "箭塔";
            archerTower.BuildCost = 50;
            archerTower.Range = 3.5f;
            archerTower.Damage = 10f;
            archerTower.AttackInterval = 0.4f;
            archerTower.ProjectileSpeed = 14f;
            archerTower.Targeting = TargetingStrategy.First;
            archerTower.BodyColor = new Color(0.3f, 0.55f, 0.9f);
            archerTower.TopColor = new Color(0.4f, 0.75f, 1f);
            archerTower.ProjectileColor = new Color(1f, 0.95f, 0.5f);
            archerTower.Size = 1f;
            archerTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 80, DamageMultiplier = 1.5f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 150, DamageMultiplier = 1.6f, RangeBonus = 0.5f, AttackSpeedBonus = 0.15f }
            };
            _towerDatas.Add(archerTower);

            // 炮塔：高伤溅射，范围大，擅长群怪
            var cannonTower = ScriptableObject.CreateInstance<TowerData>();
            cannonTower.name = "TowerData_Cannon";
            cannonTower.Type = TowerType.Cannon;
            cannonTower.DisplayName = "炮塔";
            cannonTower.BuildCost = 100;
            cannonTower.Range = 3.2f;
            cannonTower.Damage = 45f;
            cannonTower.AttackInterval = 1.6f;
            cannonTower.ProjectileSpeed = 8f;
            cannonTower.Targeting = TargetingStrategy.First;
            cannonTower.HasSplashDamage = true;
            cannonTower.SplashRadius = 2.0f;
            cannonTower.BodyColor = new Color(0.65f, 0.4f, 0.2f);
            cannonTower.TopColor = new Color(0.85f, 0.55f, 0.25f);
            cannonTower.ProjectileColor = new Color(1f, 0.45f, 0.15f);
            cannonTower.Size = 1.15f;
            cannonTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 120, DamageMultiplier = 1.5f, RangeBonus = 0.3f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 200, DamageMultiplier = 1.7f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f }
            };
            _towerDatas.Add(cannonTower);

            // 冰塔：减速辅助，低伤但减速强
            var frostTower = ScriptableObject.CreateInstance<TowerData>();
            frostTower.name = "TowerData_Frost";
            frostTower.Type = TowerType.Frost;
            frostTower.DisplayName = "冰塔";
            frostTower.BuildCost = 75;
            frostTower.Range = 3.5f;
            frostTower.Damage = 8f;
            frostTower.AttackInterval = 0.9f;
            frostTower.ProjectileSpeed = 11f;
            frostTower.Targeting = TargetingStrategy.First;
            frostTower.HasSlowEffect = true;
            frostTower.SlowAmount = 0.5f;
            frostTower.SlowDuration = 2.5f;
            frostTower.BodyColor = new Color(0.2f, 0.6f, 0.85f);
            frostTower.TopColor = new Color(0.5f, 0.9f, 1f);
            frostTower.ProjectileColor = new Color(0.6f, 0.95f, 1f);
            frostTower.Size = 0.95f;
            frostTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 90, DamageMultiplier = 1.4f, RangeBonus = 0.4f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 160, DamageMultiplier = 1.5f, RangeBonus = 0.5f, AttackSpeedBonus = 0.15f }
            };
            _towerDatas.Add(frostTower);

            // 激光塔：持续光束，长射程，高DPS，优先打最强敌人
            var laserTower = ScriptableObject.CreateInstance<TowerData>();
            laserTower.name = "TowerData_Laser";
            laserTower.Type = TowerType.Laser;
            laserTower.DisplayName = "激光塔";
            laserTower.BuildCost = 150;
            laserTower.Range = 4.5f;
            laserTower.Damage = 35f;
            laserTower.AttackInterval = 0.15f;
            laserTower.ProjectileSpeed = 25f;
            laserTower.Targeting = TargetingStrategy.Strongest;
            laserTower.IsContinuousDamage = true;
            laserTower.BodyColor = new Color(0.75f, 0.2f, 0.5f);
            laserTower.TopColor = new Color(1f, 0.4f, 0.75f);
            laserTower.ProjectileColor = new Color(1f, 0.3f, 0.9f);
            laserTower.Size = 1f;
            laserTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 180, DamageMultiplier = 1.5f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 300, DamageMultiplier = 1.8f, RangeBonus = 0.5f, AttackSpeedBonus = 0.2f }
            };
            _towerDatas.Add(laserTower);
        }

        /// <summary>
        /// 初始化各游戏系统。
        /// </summary>
        private void InitializeSystems()
        {
            // GameManager（单例自动创建）
            var gameManager = GameManager.Instance;

            // ObjectPool
            var poolGo = new GameObject("ObjectPool");
            poolGo.transform.SetParent(transform);
            var objectPool = poolGo.AddComponent<ObjectPool>();

            // EconomySystem
            var economyGo = new GameObject("EconomySystem");
            economyGo.transform.SetParent(transform);
            var economy = economyGo.AddComponent<EconomySystem>();

            // WaveSystem
            var waveGo = new GameObject("WaveSystem");
            waveGo.transform.SetParent(transform);
            var waveSystem = waveGo.AddComponent<WaveSystem>();
            waveSystem.SetPath(_pathPoints);

            // EnemySpawner（单例）
            var enemySpawner = EnemySpawner.Instance;
            enemySpawner.SetPath(_pathPoints);

            // TowerPlacer（单例）
            var towerPlacer = TowerPlacer.Instance;
            // 传递路径点坐标，禁止在路径上放置塔
            Vector3[] pathPositions = new Vector3[_pathPoints.Length];
            for (int i = 0; i < _pathPoints.Length; i++)
            {
                pathPositions[i] = _pathPoints[i].position;
            }
            towerPlacer.SetPath(pathPositions);

            // 注册系统到GameManager
            gameManager.RegisterSystems(objectPool, economy, waveSystem);

            // 浮动文字管理器（伤害飘字、金币飘字）
            var floatingTextGo = new GameObject("FloatingTextManager");
            floatingTextGo.transform.SetParent(transform);
            floatingTextGo.AddComponent<TowerDefense.UI.FloatingTextManager>();
        }

        /// <summary>
        /// 初始化UI。
        /// </summary>
        private void InitializeGameUI()
        {
            var uiManager = UIManager.Instance;
            uiManager.InitializeGameUI();

            // 设置商店可用塔
            uiManager.TowerShop.SetAvailableTowers(_towerDatas);

            // 订阅游戏结束事件
            EventBus.Subscribe<GameVictoryEvent>(_ => UIManager.Instance.ShowGameOver(true));
            EventBus.Subscribe<GameDefeatEvent>(_ => UIManager.Instance.ShowGameOver(false));
        }

        /// <summary>
        /// 注册敌人类型到生成器。
        /// </summary>
        private void RegisterEnemyTypes()
        {
            foreach (var data in _enemyDatas)
            {
                EnemySpawner.Instance.RegisterEnemyType(data);
            }
        }

        /// <summary>
        /// 设置波次配置（10波，难度递增）。
        /// </summary>
        private void SetupWaves()
        {
            var normalData = _enemyDatas.Find(d => d.Type == EnemyType.Normal);
            var fastData = _enemyDatas.Find(d => d.Type == EnemyType.Fast);
            var tankData = _enemyDatas.Find(d => d.Type == EnemyType.Tank);
            var bossData = _enemyDatas.Find(d => d.Type == EnemyType.Boss);
            var eliteData = _enemyDatas.Find(d => d.Type == EnemyType.Elite);

            var waves = new List<WaveConfig>();

            // 第1波：纯普通敌人
            waves.Add(CreateWave(1, new[] { (normalData, 8, 1.2f, 0f) }));
            // 第2波：普通+快速
            waves.Add(CreateWave(2, new[] { (normalData, 10, 1.0f, 0f), (fastData, 5, 0.8f, 3f) }));
            // 第3波：快速为主
            waves.Add(CreateWave(3, new[] { (fastData, 12, 0.7f, 0f), (normalData, 5, 1.0f, 2f) }));
            // 第4波：坦克登场
            waves.Add(CreateWave(4, new[] { (normalData, 8, 1.0f, 0f), (tankData, 3, 2.5f, 2f) }));
            // 第5波：Boss
            waves.Add(CreateWave(5, new[] { (normalData, 10, 0.8f, 0f), (bossData, 1, 0f, 5f) }));
            // 第6波：混合+精英登场
            waves.Add(CreateWave(6, new[] { (fastData, 15, 0.6f, 0f), (eliteData, 2, 3f, 3f) }));
            // 第7波：坦克潮
            waves.Add(CreateWave(7, new[] { (tankData, 8, 1.8f, 0f), (normalData, 10, 0.8f, 2f) }));
            // 第8波：快速+精英
            waves.Add(CreateWave(8, new[] { (fastData, 20, 0.5f, 0f), (eliteData, 3, 2.5f, 4f) }));
            // 第9波：全类型混合
            waves.Add(CreateWave(9, new[] { (normalData, 15, 0.7f, 0f), (fastData, 10, 0.5f, 2f), (tankData, 6, 1.5f, 4f) }));
            // 第10波：双Boss
            waves.Add(CreateWave(10, new[] { (eliteData, 5, 2f, 0f), (bossData, 2, 6f, 5f) }));
            // 第11波：精英海
            waves.Add(CreateWave(11, new[] { (eliteData, 8, 1.5f, 0f), (fastData, 15, 0.4f, 2f) }));
            // 第12波：坦克+精英
            waves.Add(CreateWave(12, new[] { (tankData, 10, 1.5f, 0f), (eliteData, 5, 2f, 3f) }));
            // 第13波：快速风暴
            waves.Add(CreateWave(13, new[] { (fastData, 40, 0.35f, 0f), (eliteData, 4, 2f, 5f) }));
            // 第14波：全面进攻
            waves.Add(CreateWave(14, new[] { (normalData, 25, 0.5f, 0f), (fastData, 20, 0.35f, 2f), (tankData, 8, 1.2f, 4f), (eliteData, 6, 1.5f, 6f) }));
            // 第15波：最终决战
            waves.Add(CreateWave(15, new[] { (eliteData, 10, 1.2f, 0f), (tankData, 10, 1.0f, 3f), (bossData, 3, 8f, 8f) }));
            // 第16-20波（第3关专属）
            waves.Add(CreateWave(16, new[] { (fastData, 50, 0.3f, 0f), (tankData, 8, 1.5f, 3f) }));
            waves.Add(CreateWave(17, new[] { (eliteData, 10, 1.5f, 0f), (fastData, 30, 0.35f, 2f), (tankData, 8, 1.2f, 4f) }));
            waves.Add(CreateWave(18, new[] { (bossData, 2, 5f, 0f), (eliteData, 10, 1.5f, 3f), (tankData, 10, 1.0f, 5f) }));
            waves.Add(CreateWave(19, new[] { (normalData, 30, 0.4f, 0f), (fastData, 40, 0.3f, 2f), (eliteData, 10, 1.5f, 4f), (tankData, 10, 1.0f, 6f) }));
            waves.Add(CreateWave(20, new[] { (bossData, 5, 6f, 0f), (eliteData, 15, 1.2f, 3f), (tankData, 15, 0.8f, 5f), (fastData, 30, 0.3f, 8f) }));

            // 根据关卡截取波数
            int maxWaves = CurrentLevel == 1 ? 10 : CurrentLevel == 2 ? 15 : 20;
            var selectedWaves = waves.GetRange(0, Mathf.Min(maxWaves, waves.Count));

            GameManager.Instance.WaveSystem.SetWaves(selectedWaves);
            // 更新 GameManager 的总波数
            GameManager.Instance.SetTotalWaves(selectedWaves.Count);
        }

        /// <summary>
        /// 辅助：创建一波配置。
        /// </summary>
        private WaveConfig CreateWave(int waveNumber, (EnemyData data, int count, float interval, float delay)[] groups)
        {
            var wave = new WaveConfig { WaveNumber = waveNumber };
            foreach (var g in groups)
            {
                wave.SpawnGroups.Add(new EnemySpawnGroup
                {
                    EnemyData = g.data,
                    Count = g.count,
                    SpawnInterval = g.interval,
                    StartDelay = g.delay
                });
            }
            return wave;
        }

        /// <summary>
        /// 重置游戏（重新开始时调用）。
        /// </summary>
        public void ResetGame()
        {
            // 清理敌人
            EnemySpawner.Instance.ClearAllEnemies();

            // 清理塔和投射物
            TowerPlacer.Instance.ClearAll();

            // 清理旧地图（包括天空、地面、路径、装饰）
            if (_mapRoot != null) Destroy(_mapRoot.gameObject);
            CreateMapAndPath();

            // 重置波次
            GameManager.Instance.WaveSystem.ResetWaves();

            // 重新设置波次
            SetupWaves();

            // 重新传递路径给各系统
            GameManager.Instance.WaveSystem.SetPath(_pathPoints);
            EnemySpawner.Instance.SetPath(_pathPoints);
            Vector3[] pathPositions = new Vector3[_pathPoints.Length];
            for (int i = 0; i < _pathPoints.Length; i++) pathPositions[i] = _pathPoints[i].position;
            TowerPlacer.Instance.SetPath(pathPositions);

            // 重置GameManager状态
            GameManager.Instance.ChangeState(GameState.Preparation);

            // 重新推送初始UI数据
            EventBus.Publish(new GoldChangedEvent { CurrentGold = GameManager.Instance.CurrentGold, Delta = 0 });
            EventBus.Publish(new LivesChangedEvent { CurrentLives = GameManager.Instance.CurrentLives, Delta = 0 });
            // 刷新HUD波次显示
            if (UIManager.Instance != null && UIManager.Instance.HUD != null)
            {
                UIManager.Instance.HUD.RefreshWaveDisplay(0, GameManager.Instance.TotalWaves);
            }
        }

        #region Sprite生成工具

        /// <summary>
        /// 生成天空渐变Sprite（上深下浅的蓝色渐变）。
        /// </summary>
        private Sprite GenerateSkySprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color topColor = new Color(0.35f, 0.6f, 0.9f, 1f);
            Color bottomColor = new Color(0.7f, 0.85f, 0.95f, 1f);

            for (int y = 0; y < size; y++)
            {
                float t = (float)y / size;
                Color c = Color.Lerp(topColor, bottomColor, t);
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite GenerateMapSprite()
        {
            int width = Mathf.RoundToInt(_mapWidth * 32);
            int height = Mathf.RoundToInt(_mapHeight * 32);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = new Color[width * height];
            // 根据关卡选择配色方案
            Color grassA, grassB, grassC, grassDark, grassLight, flowerYellow, flowerPink, flowerWhite, flowerPurple, bush;
            if (CurrentLevel == 1)
            {
                // 草原
                grassA = new Color(0.42f, 0.72f, 0.3f);
                grassB = new Color(0.36f, 0.64f, 0.26f);
                grassC = new Color(0.46f, 0.76f, 0.34f);
                grassDark = new Color(0.3f, 0.56f, 0.22f);
                grassLight = new Color(0.55f, 0.82f, 0.4f);
                flowerYellow = new Color(1f, 0.92f, 0.35f);
                flowerPink = new Color(1f, 0.55f, 0.75f);
                flowerWhite = new Color(0.96f, 0.96f, 0.92f);
                flowerPurple = new Color(0.7f, 0.5f, 0.9f);
                bush = new Color(0.25f, 0.5f, 0.18f);
            }
            else if (CurrentLevel == 2)
            {
                // 沙漠
                grassA = new Color(0.85f, 0.72f, 0.42f);
                grassB = new Color(0.78f, 0.65f, 0.38f);
                grassC = new Color(0.88f, 0.76f, 0.48f);
                grassDark = new Color(0.65f, 0.52f, 0.3f);
                grassLight = new Color(0.92f, 0.82f, 0.55f);
                flowerYellow = new Color(1f, 0.85f, 0.2f);
                flowerPink = new Color(0.95f, 0.5f, 0.3f);
                flowerWhite = new Color(1f, 0.95f, 0.8f);
                flowerPurple = new Color(0.8f, 0.6f, 0.3f);
                bush = new Color(0.5f, 0.4f, 0.2f);
            }
            else
            {
                // 冰雪
                grassA = new Color(0.65f, 0.8f, 0.95f);
                grassB = new Color(0.58f, 0.75f, 0.9f);
                grassC = new Color(0.72f, 0.85f, 0.98f);
                grassDark = new Color(0.45f, 0.65f, 0.85f);
                grassLight = new Color(0.85f, 0.92f, 1f);
                flowerYellow = new Color(1f, 0.95f, 0.6f);
                flowerPink = new Color(0.9f, 0.8f, 1f);
                flowerWhite = Color.white;
                flowerPurple = new Color(0.7f, 0.85f, 1f);
                bush = new Color(0.4f, 0.6f, 0.75f);
            }

            var rng = new System.Random(42);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    // 大格子草地（三色棋盘）
                    int cell = (x / 40) + (y / 40);
                    Color baseColor;
                    if (cell % 3 == 0) baseColor = grassA;
                    else if (cell % 3 == 1) baseColor = grassB;
                    else baseColor = grassC;

                    // 随机噪点
                    float noise = (float)(rng.NextDouble() - 0.5) * 0.06f;
                    baseColor.r += noise;
                    baseColor.g += noise;
                    baseColor.b += noise;

                    // 亮斑（更频繁）
                    if (rng.NextDouble() < 0.04)
                    {
                        baseColor = Color.Lerp(baseColor, grassLight, 0.35f);
                    }
                    // 暗斑
                    if (rng.NextDouble() < 0.025)
                    {
                        baseColor = Color.Lerp(baseColor, grassDark, 0.3f);
                    }

                    // 草丛小点
                    if (rng.NextDouble() < 0.015)
                    {
                        baseColor = Color.Lerp(baseColor, bush, 0.5f);
                    }

                    // 花朵装饰（更多种类和数量）
                    double flowerRoll = rng.NextDouble();
                    if (flowerRoll < 0.004) baseColor = flowerYellow;
                    else if (flowerRoll < 0.007) baseColor = flowerPink;
                    else if (flowerRoll < 0.009) baseColor = flowerWhite;
                    else if (flowerRoll < 0.0105) baseColor = flowerPurple;

                    pixels[idx] = baseColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private Sprite GeneratePathSegmentSprite()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite GenerateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? color : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        #endregion
    }

    /// <summary>
    /// 终点标记光圈旋转动画组件。
    /// </summary>
    public class EndpointRotator : MonoBehaviour
    {
        public float speed = 30f;

        private void Update()
        {
            transform.Rotate(0, 0, speed * Time.deltaTime);
        }
    }
}
