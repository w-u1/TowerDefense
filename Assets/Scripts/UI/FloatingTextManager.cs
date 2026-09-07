using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TowerDefense.UI
{
    /// <summary>
    /// 浮动文字管理器。在世界空间显示伤害数字、金币获取等飘字。
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        private readonly Dictionary<TextMesh, Coroutine> _activeTexts = new Dictionary<TextMesh, Coroutine>();
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
            var go = new GameObject("FloatingText");
            go.transform.SetParent(_textContainer);
            go.transform.position = position + new Vector3(0, 0.5f, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 60;
            tm.characterSize = size * 0.015f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;
            // 面向相机
            if (Camera.main != null) go.transform.rotation = Camera.main.transform.rotation;
            _activeTexts[tm] = StartCoroutine(AnimateText(tm, duration));
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

        private IEnumerator AnimateText(TextMesh tm, float duration)
        {
            float timer = 0;
            Vector3 startPos = tm.transform.position;
            Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                // 向上飘 + 轻微左右摆动
                tm.transform.position = startPos + Vector3.up * t * 1.2f + randomOffset * Mathf.Sin(t * 6f);
                // 淡出
                Color c = tm.color;
                c.a = 1f - t;
                tm.color = c;
                yield return null;
            }

            _activeTexts.Remove(tm);
            Destroy(tm.gameObject);
        }
    }
}
