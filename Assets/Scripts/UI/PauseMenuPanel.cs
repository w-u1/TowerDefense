using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// 游戏暂停菜单。点击HUD菜单按钮时显示。
    /// 选项：继续游戏、重新开始、返回主界面。
    /// </summary>
    public class PauseMenuPanel : MonoBehaviour
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
            UIManager.CreatePanel(transform, "Overlay", new Color(0.05f, 0.05f, 0.1f, 0.85f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 标题
            UIManager.CreateText(transform, "Title", "游戏暂停", 48,
                TextAnchor.MiddleCenter, new Vector2(400, 70), new Vector2(0, 120),
                new Color(1f, 0.9f, 0.4f));

            // 继续游戏按钮
            var resumeBtn = UIManager.CreateButton(transform, "ResumeBtn", "继续游戏",
                new Vector2(280, 65), new Vector2(0, 30));
            resumeBtn.onClick.AddListener(OnResumeClicked);
            var resumeText = resumeBtn.GetComponentInChildren<Text>();
            resumeText.fontSize = 26;
            var resumeColors = resumeBtn.colors;
            resumeColors.normalColor = new Color(0.3f, 0.65f, 0.35f, 0.95f);
            resumeBtn.colors = resumeColors;

            // 重新开始按钮
            var restartBtn = UIManager.CreateButton(transform, "RestartBtn", "重新开始",
                new Vector2(280, 65), new Vector2(0, -55));
            restartBtn.onClick.AddListener(OnRestartClicked);
            var restartText = restartBtn.GetComponentInChildren<Text>();
            restartText.fontSize = 26;
            var restartColors = restartBtn.colors;
            restartColors.normalColor = new Color(0.3f, 0.55f, 0.85f, 0.95f);
            restartBtn.colors = restartColors;

            // 返回主界面按钮
            var menuBtn = UIManager.CreateButton(transform, "MenuBtn", "返回主界面",
                new Vector2(280, 65), new Vector2(0, -140));
            menuBtn.onClick.AddListener(OnBackToMenuClicked);
            var menuText = menuBtn.GetComponentInChildren<Text>();
            menuText.fontSize = 26;
            var menuColors = menuBtn.colors;
            menuColors.normalColor = new Color(0.7f, 0.35f, 0.35f, 0.95f);
            menuBtn.colors = menuColors;
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            Time.timeScale = 1f;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
                GameManager.Instance.TogglePause();
        }

        private void OnRestartClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
                GameBootstrapper.Instance.ResetGame();
            }
        }

        private void OnBackToMenuClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            // 重置游戏状态
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
            }
            if (GameBootstrapper.Instance != null)
            {
                GameBootstrapper.Instance.ResetGame();
            }
            UIManager.Instance.MainMenu.Show();
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
