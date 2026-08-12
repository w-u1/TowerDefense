using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// 地图障碍物组件。挂在树、花、岩石等装饰物上。
    /// 可被鼠标选中，选中后塔会优先攻击。有血量，被摧毁后消失。
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Header("属性")]
        public float MaxHealth = 100f;
        public float CurrentHealth;
        public float Radius = 0.5f;

        private SpriteRenderer _renderer;
        private static GameObject _globalMarker;
        private GameObject _globalHealthBarBg;
        private GameObject _globalHealthBarFill;
        private SpriteRenderer _globalHealthBarFillRenderer;

        /// <summary>当前选中的障碍物（全局唯一）。</summary>
        public static Obstacle Selected { get; private set; }

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            _renderer = GetComponentInChildren<SpriteRenderer>();
            // 添加碰撞体用于鼠标点击选中
            var col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.radius = Radius / Mathf.Max(transform.localScale.x, 0.01f);
                col.isTrigger = true;
            }
            CreateHealthBar();
        }

        private void Start()
        {
            // Start时MaxHealth已被外部设置正确，重置血量为满血
            CurrentHealth = MaxHealth;
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.radius = Radius / Mathf.Max(transform.localScale.x, 0.01f);
                col.isTrigger = true;
            }
        }


        private void CreateHealthBar()
        {
            if (_globalHealthBarBg != null) return;
            _globalHealthBarBg = new GameObject("ObstacleHealthBarBg");
            var bgSr = _globalHealthBarBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = GenerateBarSprite(new Color(0.2f, 0.1f, 0.1f, 0.9f));
            bgSr.sortingOrder = 30;
            _globalHealthBarBg.transform.localScale = new Vector3(1.2f, 0.15f, 1f);
            _globalHealthBarFill = new GameObject("ObstacleHealthBarFill");
            _globalHealthBarFill.transform.SetParent(_globalHealthBarBg.transform);
            _globalHealthBarFill.transform.localPosition = Vector3.zero;
            _globalHealthBarFillRenderer = _globalHealthBarFill.AddComponent<SpriteRenderer>();
            _globalHealthBarFillRenderer.sprite = GenerateBarSprite(new Color(0.8f, 0.2f, 0.2f, 1f));
            _globalHealthBarFillRenderer.sortingOrder = 31;
            _globalHealthBarBg.SetActive(false);
        }

        private void UpdateHealthBar()
        {
            if (_globalHealthBarFillRenderer == null) return;
            float ratio = Mathf.Clamp01(CurrentHealth / MaxHealth);
            _globalHealthBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
            // 左对齐
            _globalHealthBarFill.transform.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0, 0);
        }

        /// <summary>选中此障碍物（取消其他障碍物的选中）。</summary>
        public void Select()
        {
            if (Selected != null && Selected != this) Selected.Deselect();
            Selected = this;
            ShowGlobalMarker(true);
            UpdateHealthBar();
            if (_globalHealthBarBg != null) _globalHealthBarBg.SetActive(true);
        }

        /// <summary>取消选中。</summary>
        public void Deselect()
        {
            if (Selected == this) Selected = null;
            ShowGlobalMarker(false);
            if (_globalHealthBarBg != null) _globalHealthBarBg.SetActive(false);
        }

        private void ShowGlobalMarker(bool show)
        {
            if (_globalMarker == null)
            {
                _globalMarker = new GameObject("ObstacleSelectMarker");
                var sr = _globalMarker.AddComponent<SpriteRenderer>();
                sr.sprite = GenerateMarkerSprite();
                //把白色圆环染成金黄色
                sr.color = new Color(1f, 0.9f, 0.2f, 1f);
                sr.sortingOrder = 25;
                _globalMarker.transform.localScale = Vector3.one * 0.7f;
            }
            _globalMarker.SetActive(show);
            if (show) UpdateMarkerPosition();
        }

        private void UpdateMarkerPosition()
        {
            if (_globalMarker != null && _globalMarker.activeSelf)
            {
                Vector3 center = _renderer != null ? _renderer.bounds.center : transform.position;
                _globalMarker.transform.position = new Vector3(
                    center.x,
                    center.y + 1.0f,
                    center.z);
                if (_globalHealthBarBg != null && _globalHealthBarBg.activeSelf)
                {
                    _globalHealthBarBg.transform.position = new Vector3(center.x, center.y + 0.6f, center.z);
                }
            }
        }

        private static bool _mouseHandledThisFrame;

        private void Update()
        {
            if (Selected == this) UpdateMarkerPosition();

            if (Input.GetMouseButtonDown(0) && !_mouseHandledThisFrame)
            {
                _mouseHandledThisFrame = true;
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hits = Physics2D.OverlapPointAll(new Vector2(mousePos.x, mousePos.y));
                Obstacle closest = null;
                float minDist = float.MaxValue;
                foreach (var hit in hits)
                {
                    var obs = hit.GetComponent<Obstacle>();
                    if (obs != null && obs.isActiveAndEnabled)
                    {
                        float dist = Vector3.Distance(obs.transform.position, mousePos);
                        if (dist < minDist) { minDist = dist; closest = obs; }
                    }
                }
                if (closest != null) closest.Select();
            }
            if (!Input.GetMouseButton(0)) _mouseHandledThisFrame = false;
        }

        /// <summary>受到伤害。返回是否被摧毁。</summary>
        public bool TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            UpdateHealthBar();
            if (CurrentHealth <= 0)
            {
                Die();
                return true;
            }
            // 受击闪白
            if (_renderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(HitFlash());
            }
            return false;
        }

        private System.Collections.IEnumerator HitFlash()
        {
            if (_renderer == null) yield break;
            Color original = _renderer.color;
            _renderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            _renderer.color = original;
        }

        private void Die()
        {
            if (Selected == this) Deselect();
            // 清理障碍物获得金币（按血量计算，最低5金币）
            int goldReward = Mathf.Max(5, Mathf.RoundToInt(MaxHealth / 10f));
            GameManager.Instance.AddGold(goldReward);
            EventBus.Publish(new GoldChangedEvent { CurrentGold = GameManager.Instance.CurrentGold, Delta = goldReward });
            TowerDefense.Systems.AudioManager.Instance?.PlayGold();
            // 死亡特效
            TowerDefense.Systems.EffectsPool.Instance?.SpawnDeathEffect(transform.position, 0.5f);
            Destroy(gameObject);
        }

        /// <summary>生成选中标识。</summary>
        private static Sprite GenerateMarkerSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float radius = size * 0.4f;
            float innerRadius = size * 0.25f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius && dist >= innerRadius)
                        pixels[y * size + x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>生成纯色条精灵。</summary>
        private static Sprite GenerateBarSprite(Color color)
        {
            int w = 64, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64);
        }
    }
}
