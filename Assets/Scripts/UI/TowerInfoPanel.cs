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
        private Text _descText;
        private Button _upgradeButton;
        private Button _sellButton;
        private Text _upgradeButtonText;
        private GameObject _panel;

        public void Initialize()
        {
            // 面板：左下角，固定大小
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(440, 520);
            rect.anchoredPosition = new Vector2(16, 80);

            _panel = gameObject;

            // 背景
            UIManager.CreatePanel(transform, "Background", new Color(0.08f, 0.08f, 0.12f, 0.95f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 标题（顶部居中）
            var titleText = UIManager.CreateText(transform, "Title", "塔信息", 44,
                TextAnchor.MiddleCenter, new Vector2(420, 56), new Vector2(0, -20),
                new Color(0.4f, 0.85f, 1f));
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -14);

            // 塔名（标题下面）
            _nameText = UIManager.CreateText(transform, "TowerName", "", 36,
                TextAnchor.MiddleCenter, new Vector2(420, 48), new Vector2(0, -75),
                new Color(1f, 0.95f, 0.6f));
            var nameRect = _nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1);
            nameRect.anchorMax = new Vector2(0.5f, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -72);

            // 属性（塔名下面）
            _statsText = UIManager.CreateText(transform, "Stats", "", 26,
                TextAnchor.UpperLeft, new Vector2(380, 110), new Vector2(20, -140),
                new Color(0.9f, 0.95f, 1f));
            var statsRect = _statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1);
            statsRect.anchorMax = new Vector2(0, 1);
            statsRect.pivot = new Vector2(0, 1);
            statsRect.anchoredPosition = new Vector2(20, -140);

            // 塔描述（属性下面）
            _descText = UIManager.CreateText(transform, "Description", "", 22,
                TextAnchor.UpperLeft, new Vector2(400, 100), new Vector2(15, -295),
                new Color(0.75f, 0.8f, 0.85f));
            _descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _descText.verticalOverflow = VerticalWrapMode.Truncate;
            var descRect = _descText.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(0, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(15, -295);

            // 升级按钮（底部上面一点）
            _upgradeButton = UIManager.CreateButton(transform, "UpgradeButton", "升级",
                new Vector2(360, 60), new Vector2(0, -410));
            var upgradeRect = _upgradeButton.GetComponent<RectTransform>();
            upgradeRect.anchorMin = new Vector2(0.5f, 1);
            upgradeRect.anchorMax = new Vector2(0.5f, 1);
            upgradeRect.pivot = new Vector2(0.5f, 1);
            upgradeRect.anchoredPosition = new Vector2(0, -410);
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            _upgradeButtonText = _upgradeButton.GetComponentInChildren<Text>();
            _upgradeButtonText.fontSize = 28;

            // 出售按钮（最底部）
            _sellButton = UIManager.CreateButton(transform, "SellButton", "出售",
                new Vector2(360, 60), new Vector2(0, -485));
            var sellRect = _sellButton.GetComponent<RectTransform>();
            sellRect.anchorMin = new Vector2(0.5f, 1);
            sellRect.anchorMax = new Vector2(0.5f, 1);
            sellRect.pivot = new Vector2(0.5f, 1);
            sellRect.anchoredPosition = new Vector2(0, -485);
            var sellColors = _sellButton.colors;
            sellColors.normalColor = new Color(0.7f, 0.25f, 0.25f, 0.95f);
            sellColors.highlightedColor = new Color(0.9f, 0.35f, 0.35f, 1f);
            _sellButton.colors = sellColors;
            _sellButton.onClick.AddListener(OnSellClicked);
            var sellBtnText = _sellButton.GetComponentInChildren<Text>();
            sellBtnText.fontSize = 28;

            // 关闭按钮（右上角）
            var closeButton = UIManager.CreateButton(transform, "CloseButton", "X",
                new Vector2(32, 32), new Vector2(-8, -8));
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
                $"攻速: {1f / _selectedTower.Data.AttackInterval:F2}/秒\n";

            // 辅助塔不显示目标，显示增益信息
            if (data.IsSupportTower)
            {
                _statsText.text += $"攻速加成: {data.SupportAttackSpeedBonus * 100:F0}%\n";
                _statsText.text += $"伤害加成: {data.SupportDamageBonus * 100:F0}%\n";
            }
            else
            {
                _statsText.text += $"目标: {TargetingToChinese(data.Targeting)}\n";
            }

            if (!string.IsNullOrEmpty(data.Description))
                _descText.text = data.Description;
            else
                _descText.text = "";

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

            // 出售按钮：显示返还金币
            _sellButton.GetComponentInChildren<Text>().text = $"出售 ({_selectedTower.GetSellPrice()}金)";
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
            // 按ESC关闭
            if (Input.GetKeyDown(KeyCode.Escape) && _panel.activeSelf)
            {
                Hide();
            }
        }
    }
}
