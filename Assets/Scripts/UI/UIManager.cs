using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    /// <summary>
    /// UI管理器。负责创建和管理所有UI面板，程序化生成UI，无需外部资源。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI设置")]
        [Tooltip("UI缩放参考分辨率")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1920, 1080);

        // UI根节点
        private Canvas _mainCanvas;
        private GameObject _uiRoot;

        // 面板引用
        public HUDController HUD { get; private set; }
        public TowerShopPanel TowerShop { get; private set; }
        public GameOverPanel GameOver { get; private set; }
        public TowerInfoPanel TowerInfo { get; private set; }
        public LevelSelectPanel LevelSelect { get; private set; }

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            CreateUIRoot();
        }

        /// <summary>
        /// 创建UI根节点（Canvas + EventSystem）。
        /// </summary>
        private void CreateUIRoot()
        {
            // Canvas
            var canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(transform);
            _mainCanvas = canvasGo.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(transform);
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            _uiRoot = canvasGo.gameObject;
        }

        /// <summary>
        /// 初始化UI基础（关卡选择面板）。
        /// </summary>
        public void InitializeUIBase()
        {
            // 关卡选择面板
            var lsGo = CreateUIPanelObject("LevelSelect");
            LevelSelect = lsGo.AddComponent<LevelSelectPanel>();
            LevelSelect.Initialize();
        }

        /// <summary>
        /// 初始化游戏内UI（HUD、商店等）。
        /// </summary>
        public void InitializeGameUI()
        {
            // HUD
            var hudGo = CreateUIPanelObject("HUD");
            HUD = hudGo.AddComponent<HUDController>();
            HUD.Initialize();

            // 塔商店
            var shopGo = CreateUIPanelObject("TowerShop");
            TowerShop = shopGo.AddComponent<TowerShopPanel>();
            TowerShop.Initialize();

            // 塔信息面板
            var infoGo = CreateUIPanelObject("TowerInfo");
            TowerInfo = infoGo.AddComponent<TowerInfoPanel>();
            TowerInfo.Initialize();
            TowerInfo.Hide();

            // 游戏结束面板
            var gameOverGo = CreateUIPanelObject("GameOver");
            GameOver = gameOverGo.AddComponent<GameOverPanel>();
            GameOver.Initialize();
            GameOver.Hide();
        }

        /// <summary>
        /// 创建UI面板物体（确保带RectTransform，并设为Canvas子物体）。
        /// </summary>
        private GameObject CreateUIPanelObject(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_uiRoot.transform, false);
            return go;
        }

        /// <summary>
        /// 显示游戏结束面板。
        /// </summary>
        public void ShowGameOver(bool isVictory)
        {
            GameOver.Show(isVictory);
        }

        /// <summary>
        /// 创建一个标准UI按钮。
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.7f, 0.9f);

            var button = go.AddComponent<Button>();

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            // 文字
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textComp = textGo.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.fontSize = 20;
            textComp.fontStyle = FontStyle.Bold;

            var textRect = textComp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// 创建一个标准UI文本。
        /// </summary>
        public static Text CreateText(Transform parent, string name, string content, int fontSize,
            TextAnchor anchor, Vector2 size, Vector2 anchoredPos, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color ?? Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            return text;
        }

        /// <summary>
        /// 创建一个面板背景。
        /// </summary>
        public static Image CreatePanel(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPos,
            Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = color;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            if (anchorMin.HasValue) rect.anchorMin = anchorMin.Value;
            if (anchorMax.HasValue) rect.anchorMax = anchorMax.Value;

            return image;
        }
    }
}
