using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    /// <summary>
    /// 塔信息面板。点击已建造的塔后显示，提供升级和出售选项。
    /// </summary>
    public class TowerInfoPanel : MonoBehaviour
    {
        private Tower _selectedTower;
        private Text _nameText;
        private Text _statsText;
        private Button _upgradeButton;
        private Button _sellButton;
        private Text _upgradeButtonText;
        private GameObject _panel;

        public void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(380, 0);
            rect.offsetMin = new Vector2(0, 70);
            rect.offsetMax = new Vector2(0, -70);

            _panel = gameObject;

            // 背景
            UIManager.CreatePanel(transform, "Background", new Color(0.08f, 0.08f, 0.12f, 0.95f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);
            // 顶部装饰线
            UIManager.CreatePanel(transform, "TopLine", new Color(0.2f, 0.45f, 0.7f, 0.6f),
                new Vector2(0, 2), new Vector2(0, 0),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1));

            // 标题
            UIManager.CreateText(transform, "Title", "塔信息", 42,
                TextAnchor.MiddleCenter, new Vector2(320, 54), new Vector2(-170, -42),
                new Color(0.4f, 0.85f, 1f));

            // 名称
            _nameText = UIManager.CreateText(transform, "TowerName", "", 34,
                TextAnchor.MiddleCenter, new Vector2(320, 44), new Vector2(-170, -100),
                new Color(1f, 0.95f, 0.6f));

            // 属性
            _statsText = UIManager.CreateText(transform, "Stats", "", 30,
                TextAnchor.UpperLeft, new Vector2(310, 200), new Vector2(-175, -230),
                new Color(0.9f, 0.95f, 1f));

            // 升级按钮
            _upgradeButton = UIManager.CreateButton(transform, "UpgradeButton", "升级",
                new Vector2(270, 72), new Vector2(-170, -350));
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            _upgradeButtonText = _upgradeButton.GetComponentInChildren<Text>();
            _upgradeButtonText.fontSize = 30;

            // 出售按钮
            _sellButton = UIManager.CreateButton(transform, "SellButton", "出售",
                new Vector2(270, 72), new Vector2(-170, -440));
            var sellColors = _sellButton.colors;
            sellColors.normalColor = new Color(0.7f, 0.25f, 0.25f, 0.95f);
            sellColors.highlightedColor = new Color(0.9f, 0.35f, 0.35f, 1f);
            _sellButton.colors = sellColors;
            _sellButton.onClick.AddListener(OnSellClicked);
            var sellBtnText = _sellButton.GetComponentInChildren<Text>();
            sellBtnText.fontSize = 30;

            // 关闭按钮
            var closeButton = UIManager.CreateButton(transform, "CloseButton", "X",
                new Vector2(48, 48), new Vector2(-32, -32));
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1));
            closeButton.onClick.AddListener(Hide);
        }

        private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private string TargetingToChinese(TargetingStrategy strategy)
        {
            switch (strategy)
            {
                case TargetingStrategy.First: return "最前";
                case TargetingStrategy.Strongest: return "最强";
                case TargetingStrategy.Closest: return "最近";
                case TargetingStrategy.Weakest: return "最弱";
                default: return strategy.ToString();
            }
        }

        /// <summary>
        /// 显示指定塔的信息。
        /// </summary>
        public void Show(Tower tower)
        {
            // 先隐藏之前塔的范围
            if (_selectedTower != null)
            {
                _selectedTower.ShowRangeIndicator(false);
            }

            _selectedTower = tower;
            if (tower == null || tower.Data == null)
            {
                Hide();
                return;
            }

            _panel.SetActive(true);
            UpdateDisplay();
            // 显示当前选中塔的范围
            _selectedTower.ShowRangeIndicator(true);
        }

        /// <summary>
        /// 更新显示内容。
        /// </summary>
        private void UpdateDisplay()
        {
            if (_selectedTower == null || _selectedTower.Data == null) return;

            var data = _selectedTower.Data;
            _nameText.text = $"{data.DisplayName} Lv.{_selectedTower.CurrentLevel + 1}";

            _statsText.text =
                $"伤害: {_selectedTower.CurrentDamage:F1}\n" +
                $"范围: {_selectedTower.CurrentRange:F1}\n" +
                $"攻速: {1f / _selectedTower.Data.AttackInterval:F2}/秒\n" +
                $"目标: {TargetingToChinese(data.Targeting)}\n";

            if (data.HasSplashDamage)
                _statsText.text += $"溅射半径: {data.SplashRadius}\n";
            if (data.HasSlowEffect)
                _statsText.text += $"减速: {data.SlowAmount * 100:F0}% / {data.SlowDuration}秒\n";

            // 升级按钮
            var nextUpgrade = _selectedTower.GetNextUpgrade();
            if (nextUpgrade != null)
            {
                _upgradeButton.interactable = GameManager.Instance.CurrentGold >= nextUpgrade.Cost;
                _upgradeButtonText.text = $"升级 ({nextUpgrade.Cost}金)";
            }
            else
            {
                _upgradeButton.interactable = false;
                _upgradeButtonText.text = "已满级";
            }
        }

        private void OnUpgradeClicked()
        {
            if (_selectedTower == null) return;
            if (_selectedTower.Upgrade())
            {
                UpdateDisplay();
            }
        }

        private void OnSellClicked()
        {
            if (_selectedTower == null) return;
            _selectedTower.Sell();
            Hide();
        }

        public void Hide()
        {
            if (_selectedTower != null)
            {
                _selectedTower.ShowRangeIndicator(false);
            }
            _selectedTower = null;
            _panel.SetActive(false);
        }

        private void Update()
        {
            // 点击空白处关闭
            if (Input.GetMouseButtonDown(0) && _panel.activeSelf)
            {
                // 简化：如果点击的不是UI，则关闭
                // 实际项目中应使用EventSystem检测
            }

            // 按ESC关闭
            if (Input.GetKeyDown(KeyCode.Escape) && _panel.activeSelf)
            {
                Hide();
            }
        }
    }
}
