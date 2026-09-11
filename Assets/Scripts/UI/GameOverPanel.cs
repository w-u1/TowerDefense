using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    /// <summary>
    /// 游戏结束面板。显示胜利/失败信息，提供重新开始和退出按钮。
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        private GameObject _panel;
        private Text _titleText;
        private Text _messageText;
        private Button _restartButton;
        private Button _nextLevelButton;
        private Button _levelSelectButton;
        private Button _quitButton;

        public void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _panel = gameObject;

            // 半透明遮罩
            UIManager.CreatePanel(transform, "Overlay", new Color(0, 0, 0, 0.7f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 主面板
            var mainPanel = UIManager.CreatePanel(transform, "MainPanel", new Color(0.1f, 0.12f, 0.18f, 0.98f),
                new Vector2(500, 400), Vector2.zero);
            mainPanel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mainPanel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mainPanel.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 标题
            _titleText = UIManager.CreateText(mainPanel.transform, "Title", "", 48,
                TextAnchor.MiddleCenter, new Vector2(400, 80), new Vector2(0, 120),
                Color.white);

            // 消息
            _messageText = UIManager.CreateText(mainPanel.transform, "Message", "", 22,
                TextAnchor.MiddleCenter, new Vector2(400, 100), new Vector2(0, 30),
                new Color(0.8f, 0.8f, 0.8f));

            // 统计信息
            UIManager.CreateText(mainPanel.transform, "Stats", "", 18,
                TextAnchor.MiddleCenter, new Vector2(400, 60), new Vector2(0, -40),
                new Color(0.7f, 0.7f, 0.7f));

            // 重新开始按钮
            _restartButton = UIManager.CreateButton(mainPanel.transform, "RestartButton", "重玩本关",
                new Vector2(200, 55), new Vector2(-110, -120));
            _restartButton.onClick.AddListener(OnRestartClicked);

            // 下一关按钮（胜利时显示）
            _nextLevelButton = UIManager.CreateButton(mainPanel.transform, "NextLevelButton", "下一关",
                new Vector2(200, 55), new Vector2(110, -120));
            var nextColors = _nextLevelButton.colors;
            nextColors.normalColor = new Color(0.2f, 0.6f, 0.3f, 0.9f);
            _nextLevelButton.colors = nextColors;
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            // 选关按钮
            _levelSelectButton = UIManager.CreateButton(mainPanel.transform, "LevelSelectButton", "选择关卡",
                new Vector2(200, 55), new Vector2(0, -190));
            var lsColors = _levelSelectButton.colors;
            lsColors.normalColor = new Color(0.3f, 0.35f, 0.5f, 0.9f);
            _levelSelectButton.colors = lsColors;
            _levelSelectButton.onClick.AddListener(OnLevelSelectClicked);

            // 退出按钮
            _quitButton = UIManager.CreateButton(mainPanel.transform, "QuitButton", "退出",
                new Vector2(200, 55), new Vector2(0, -260));
            var quitColors = _quitButton.colors;
            quitColors.normalColor = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            _quitButton.colors = quitColors;
            _quitButton.onClick.AddListener(OnQuitClicked);

            _panel.SetActive(false);
        }

        /// <summary>
        /// 显示游戏结束面板。
        /// </summary>
        public void Show(bool isVictory)
        {
            _panel.SetActive(true);

            if (isVictory)
            {
                _titleText.text = $"第{GameBootstrapper.CurrentLevel}关 胜利！";
                _titleText.color = new Color(0.3f, 1f, 0.4f);
                _messageText.text = "恭喜你成功抵御了所有敌人的进攻！";
                // 最后一关不显示下一关
                _nextLevelButton.gameObject.SetActive(GameBootstrapper.CurrentLevel < 3);
            }
            else
            {
                _titleText.text = "失败";
                _titleText.color = new Color(1f, 0.3f, 0.3f);
                _messageText.text = "基地被敌人摧毁了，再接再厉！";
                _nextLevelButton.gameObject.SetActive(false);
            }

            // 更新统计
            var statsText = _panel.transform.Find("MainPanel/Stats").GetComponent<Text>();
            statsText.text = $"坚持到第 {GameManager.Instance.CurrentWave} 波 / 共 {GameManager.Instance.TotalWaves} 波\n" +
                             $"剩余金币: {GameManager.Instance.CurrentGold}  剩余生命: {GameManager.Instance.CurrentLives}";
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            Hide();
            GameManager.Instance.ResetGame();
            TowerDefense.Core.GameBootstrapper.Instance?.ResetGame();
        }

        private void OnNextLevelClicked()
        {
            GameBootstrapper.CurrentLevel = Mathf.Min(3, GameBootstrapper.CurrentLevel + 1);
            Time.timeScale = 1f;
            Hide();
            GameManager.Instance.ResetGame();
            TowerDefense.Core.GameBootstrapper.Instance?.ResetGame();
        }

        private void OnLevelSelectClicked()
        {
            Time.timeScale = 1f;
            Hide();
            UIManager.Instance.LevelSelect.Show();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
