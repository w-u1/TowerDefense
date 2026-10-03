using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TowerDefense.UI
{
    /// <summary>
    /// 浮动文字管理器。在世界空间显示伤害数字、金币获取等飘字。
    /// 优化：限制同屏最大数量，超出时复用最老的飘字对象。
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("设置")]
        [Tooltip("同屏最大飘字数量")]
        [SerializeField] private int _maxActiveTexts = 40;

        private readonly List<FloatingText> _activeTexts = new List<FloatingText>(64);
        private Transform _textContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var container = new GameObject("FloatingTexts");
            container.transform.SetParent(transform);
            _textContainer = container.transform;
        }

        /// <summary>
        /// 在世界位置弹出文字。
        /// </summary>
        public void ShowText(Vector3 position, string text, Color color, float duration = 0.8f, float size = 0.35f)
        {
            // 超出最大数量时，复用最老的飘字
            if (_activeTexts.Count >= _maxActiveTexts)
            {
                var oldest = _activeTexts[0];
                _activeTexts.RemoveAt(0);
                oldest.Play(position, text, color, duration, size);
                _activeTexts.Add(oldest);
                return;
            }

            var go = new GameObject("FloatingText");
            go.transform.SetParent(_textContainer);
            var ft = go.AddComponent<FloatingText>();
            ft.OnFinished += HandleTextFinished;
            ft.Play(position, text, color, duration, size);
            _activeTexts.Add(ft);
        }

        private void HandleTextFinished(FloatingText ft)
        {
            _activeTexts.Remove(ft);
            ft.OnFinished -= HandleTextFinished;
            Destroy(ft.gameObject);
        }

        /// <summary>
        /// 弹出伤害数字。
        /// </summary>
        public void ShowDamage(Vector3 position, float damage)
        {
            string text = Mathf.RoundToInt(damage).ToString();
            Color color = new Color(1f, 0.9f, 0.3f);
            ShowText(position, text, color, 0.6f, 0.3f);
        }

        /// <summary>
        /// 弹出金币获取。
        /// </summary>
        public void ShowGold(Vector3 position, int gold)
        {
            ShowText(position, $"+{gold}", new Color(1f, 0.85f, 0.2f), 0.8f, 0.35f);
        }

        /// <summary>
        /// 弹出生命损失（红色）。
        /// </summary>
        public void ShowLifeLost(Vector3 position, int damage)
        {
            ShowText(position, $"-{damage}", new Color(1f, 0.3f, 0.3f), 1f, 0.45f);
        }

        /// <summary>清空所有飘字。</summary>
        public void ClearAll()
        {
            foreach (var ft in _activeTexts)
            {
                if (ft != null) Destroy(ft.gameObject);
            }
            _activeTexts.Clear();
        }
    }

    /// <summary>
    /// 单个飘字组件。
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public System.Action<FloatingText> OnFinished;

        private TextMesh _tm;
        private float _duration;
        private float _timer;
        private Vector3 _startPos;
        private Vector3 _randomOffset;

        private void Awake()
        {
            _tm = gameObject.AddComponent<TextMesh>();
            _tm.fontSize = 60;
            _tm.anchor = TextAnchor.MiddleCenter;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;
        }

        public void Play(Vector3 position, string text, Color color, float duration, float size)
        {
            _duration = duration;
            _timer = 0;
            _startPos = position + new Vector3(0, 0.5f, 0);
            _randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);

            transform.position = _startPos;
            _tm.text = text;
            _tm.characterSize = size * 0.015f;
            _tm.color = color;

            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / _duration;

            if (t >= 1f)
            {
                OnFinished?.Invoke(this);
                return;
            }

            // 向上飘 + 轻微左右摆动
            transform.position = _startPos + Vector3.up * t * 1.2f + _randomOffset * Mathf.Sin(t * 6f);

            // 淡出
            Color c = _tm.color;
            c.a = 1f - t;
            _tm.color = c;
        }
    }
}
