using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// 关卡选择面板。游戏开始时显示，选择后才进入游戏。
    /// </summary>
    public class LevelSelectPanel : MonoBehaviour
    {
        private GameObject _panel;
        private int _selectedLevel = 1;

        public void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _panel = gameObject;

            // 半透明背景
            UIManager.CreatePanel(transform, "Overlay", new Color(0.05f, 0.08f, 0.12f, 0.95f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 标题
            UIManager.CreateText(transform, "Title", "选择关卡", 48,
                TextAnchor.MiddleCenter, new Vector2(600, 80), new Vector2(0, 200),
                new Color(1f, 0.9f, 0.4f));

            // 副标题
            UIManager.CreateText(transform, "Subtitle", "塔防大作战", 24,
                TextAnchor.MiddleCenter, new Vector2(600, 40), new Vector2(0, 140),
                new Color(0.7f, 0.85f, 1f));

            // 五个关卡按钮（2行布局）
            CreateLevelButton("Level1Btn", "第一关", "草原战场", "经典S形路径", new Vector2(-350, 50),
                new Color(0.3f, 0.7f, 0.35f), 1);
            CreateLevelButton("Level2Btn", "第二关", "沙漠峡谷", "曲折之字路", new Vector2(0, 50),
                new Color(0.85f, 0.65f, 0.25f), 2);
            CreateLevelButton("Level3Btn", "第三关", "冰封要塞", "环形包围战", new Vector2(350, 50),
                new Color(0.4f, 0.7f, 0.95f), 3);
            CreateLevelButton("Level4Btn", "第四关", "火山熔岩", "迂回多弯", new Vector2(-175, -130),
                new Color(0.85f, 0.35f, 0.2f), 4);
            CreateLevelButton("Level5Btn", "第五关", "迷雾森林", "迷宫路径", new Vector2(175, -130),
                new Color(0.2f, 0.55f, 0.3f), 5);
            // 开始按钮
            var startBtn = UIManager.CreateButton(transform, "StartBtn", "开始游戏",
                new Vector2(260, 70), new Vector2(0, -280));
            startBtn.onClick.AddListener(OnStartClicked);
            var startText = startBtn.GetComponentInChildren<Text>();
            startText.fontSize = 28;

            // 返回主界面按钮
            var backBtn = UIManager.CreateButton(transform, "BackBtn", "返回主界面",
                new Vector2(260, 60), new Vector2(0, -365));
            var backColors = backBtn.colors;
            backColors.normalColor = new Color(0.5f, 0.5f, 0.6f, 0.9f);
            backBtn.colors = backColors;
            backBtn.onClick.AddListener(() => {
                Hide();
                UIManager.Instance.MainMenu.Show();
            });

            // 退出游戏按钮
            var exitBtn = UIManager.CreateButton(transform, "ExitBtn", "退出游戏",
                new Vector2(260, 60), new Vector2(0, -445));
            var exitColors = exitBtn.colors;
            exitColors.normalColor = new Color(0.7f, 0.3f, 0.3f, 0.9f);
            exitBtn.colors = exitColors;
            exitBtn.onClick.AddListener(() => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

        }

        private void CreateLevelButton(string name, string title, string desc, string pathDesc,
            Vector2 pos, Color color, int level)
        {
            var btn = UIManager.CreateButton(transform, name, "", new Vector2(280, 180), pos);
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            btn.colors = colors;

            // 关卡号
            UIManager.CreateText(btn.transform, "LevelNum", $"第{level}关", 32,
                TextAnchor.MiddleCenter, new Vector2(260, 50), new Vector2(0, 45),
                Color.white);
            // 场景名
            UIManager.CreateText(btn.transform, "SceneName", title, 22,
                TextAnchor.MiddleCenter, new Vector2(260, 35), new Vector2(0, 5),
                new Color(1f, 1f, 0.8f));
            // 路径描述
            UIManager.CreateText(btn.transform, "PathDesc", pathDesc, 18,
                TextAnchor.MiddleCenter, new Vector2(260, 30), new Vector2(0, -35),
                new Color(0.9f, 0.9f, 0.9f));

            int capturedLevel = level;
            btn.onClick.AddListener(() => {
                _selectedLevel = capturedLevel;
                // 高亮选中
                HighlightSelection(capturedLevel);
            });
        }

        private void HighlightSelection(int level)
        {
            for (int i = 1; i <= 3; i++)
            {
                var btn = transform.Find($"Level{i}Btn")?.GetComponent<Button>();
                if (btn != null)
                {
                    var colors = btn.colors;
                    if (i == level)
                    {
                        colors.normalColor = Color.white;
                    }
                    else
                    {
                        colors.normalColor = new Color(0.5f, 0.55f, 0.6f, 0.9f);
                    }
                    btn.colors = colors;
                }
            }
        }

        private void OnStartClicked()
        {
            GameBootstrapper.CurrentLevel = _selectedLevel;
            Hide();
            Time.timeScale = 1f;
            // 隐藏游戏结束面板
            if (UIManager.Instance != null && UIManager.Instance.GameOver != null)
                UIManager.Instance.GameOver.Hide();

            // 如果游戏已经在运行，完全重置
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Initializing)
            {
                GameManager.Instance.ResetGame();
                GameBootstrapper.Instance.ResetGame();
            }
            else
            {
                GameBootstrapper.Instance.StartGame();
            }
        }

        public void Show()
        {
            _panel.SetActive(true);
            // 移到最前面，避免被其他UI遮挡
            transform.SetAsLastSibling();
        }
        public void Hide() { _panel.SetActive(false); }
    }
}
