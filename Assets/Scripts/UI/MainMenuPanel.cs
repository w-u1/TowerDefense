using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// 游戏主界面。显示游戏标题和菜单选项。
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        private GameObject _panel;

        public void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _panel = gameObject;

            // 半透明背景
            UIManager.CreatePanel(transform, "Overlay", new Color(0.05f, 0.08f, 0.15f, 0.98f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 游戏标题
            UIManager.CreateText(transform, "GameTitle", "塔防大作战", 72,
                TextAnchor.MiddleCenter, new Vector2(800, 120), new Vector2(0, 120),
                new Color(1f, 0.85f, 0.3f));

            // 选择关卡按钮
            var levelBtn = UIManager.CreateButton(transform, "LevelBtn", "选择关卡",
                new Vector2(320, 80), new Vector2(0, -80));
            levelBtn.onClick.AddListener(OnLevelSelectClicked);
            var levelText = levelBtn.GetComponentInChildren<Text>();
            levelText.fontSize = 32;
            var levelColors = levelBtn.colors;
            levelColors.normalColor = new Color(0.3f, 0.65f, 0.35f, 0.95f);
            levelBtn.colors = levelColors;

            // 退出游戏按钮
            var exitBtn = UIManager.CreateButton(transform, "ExitBtn", "退出游戏",
                new Vector2(320, 80), new Vector2(0, -190));
            exitBtn.onClick.AddListener(OnExitClicked);
            var exitText = exitBtn.GetComponentInChildren<Text>();
            exitText.fontSize = 32;
            var exitColors = exitBtn.colors;
            exitColors.normalColor = new Color(0.7f, 0.3f, 0.3f, 0.95f);
            exitBtn.colors = exitColors;

            // 底部版本信息
            UIManager.CreateText(transform, "Version", "v1.0 正式版", 18,
                TextAnchor.MiddleCenter, new Vector2(200, 30), new Vector2(0, -320),
                new Color(0.5f, 0.55f, 0.6f));
        }

        private void OnLevelSelectClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            UIManager.Instance.LevelSelect.Show();
        }

        private void OnExitClicked()
        {
            AudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Show()
        {
            _panel.SetActive(true);
            transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }
    }
}
