using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// 敌人实体。沿路径点移动，被防御塔攻击，到达终点扣除玩家生命值。
    /// 使用对象池回收，死亡时触发事件。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // 运行时数据
        private EnemyData _data;
        private Transform[] _pathPoints;
        private int _currentPathIndex = 0;
        private float _currentHealth;
        private bool _isActive = false;
        private Vector3 _moveDirection;

        // 血条
        private GameObject _healthBarRoot;
        private SpriteRenderer _healthBarBg;
        private SpriteRenderer _healthBarFill;
        private float _healthBarWidth = 0.8f;

        // 引用缓存
        private Transform _transform;

        public EnemyData Data => _data;
        public float CurrentHealth => _currentHealth;
        public bool IsActive => _isActive;
        public Vector3 MoveDirection => _moveDirection;

        private void Awake()
        {
            _transform = transform;
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 初始化敌人（从对象池取出时调用）。
        /// </summary>
        public void Initialize(EnemyData data, Transform[] pathPoints)
        {
            // 懒初始化：从对象池取出的未激活对象Awake可能尚未执行
            if (_transform == null) _transform = transform;
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            _data = data;
            _pathPoints = pathPoints;
            _currentHealth = data.MaxHealth;
            _currentPathIndex = 0;
            _isActive = true;

            // 应用视觉配置
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = data.BodyColor;
            }
            _transform.localScale = Vector3.one * data.Size;

            // 创建/更新血条
            CreateHealthBar();
            UpdateHealthBar();

            // 放置到路径起点
            if (_pathPoints != null && _pathPoints.Length > 0)
            {
                _transform.position = _pathPoints[0].position;
            }
        }

        /// <summary>
        /// 创建血条（世界空间Sprite）。
        /// </summary>
        private void CreateHealthBar()
        {
            if (_healthBarRoot != null) return;

            _healthBarRoot = new GameObject("HealthBar");
            _healthBarRoot.transform.SetParent(_transform);
            _healthBarRoot.transform.localPosition = new Vector3(0, _data.Size * 0.7f + 0.15f, 0);

            // 背景
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(_healthBarRoot.transform);
            bgGo.transform.localPosition = Vector3.zero;
            _healthBarBg = bgGo.AddComponent<SpriteRenderer>();
            _healthBarBg.sprite = GenerateWhiteSprite();
            _healthBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            _healthBarBg.sortingOrder = 10;
            bgGo.transform.localScale = new Vector3(_healthBarWidth, 0.12f, 1);

            // 填充
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_healthBarRoot.transform);
            fillGo.transform.localPosition = Vector3.zero;
            _healthBarFill = fillGo.AddComponent<SpriteRenderer>();
            _healthBarFill.sprite = GenerateWhiteSprite();
            _healthBarFill.color = new Color(0.2f, 0.85f, 0.3f, 1f);
            _healthBarFill.sortingOrder = 11;
            fillGo.transform.localScale = new Vector3(_healthBarWidth, 0.1f, 1);
        }

        /// <summary>
        /// 更新血条显示。
        /// </summary>
        private void UpdateHealthBar()
        {
            if (_healthBarFill == null || _data == null) return;

            float ratio = Mathf.Clamp01(_currentHealth / _data.MaxHealth);
            float fillWidth = _healthBarWidth * ratio;
            _healthBarFill.transform.localScale = new Vector3(fillWidth, 0.1f, 1);
            // 左对齐填充
            _healthBarFill.transform.localPosition = new Vector3(-(_healthBarWidth - fillWidth) / 2f, 0, 0);

            // 颜色随血量变化
            if (ratio > 0.6f)
                _healthBarFill.color = new Color(0.2f, 0.85f, 0.3f, 1f);
            else if (ratio > 0.3f)
                _healthBarFill.color = new Color(0.95f, 0.75f, 0.2f, 1f);
            else
                _healthBarFill.color = new Color(0.9f, 0.25f, 0.25f, 1f);
        }

        /// <summary>
        /// 生成白色Sprite（用于血条）。
        /// </summary>
        private static Sprite GenerateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void Update()
        {
            if (!_isActive || _pathPoints == null || _pathPoints.Length == 0) return;

            MoveAlongPath();
        }

        /// <summary>
        /// 沿路径点移动。
        /// </summary>
        private void MoveAlongPath()
        {
            if (_currentPathIndex >= _pathPoints.Length)
            {
                ReachEnd();
                return;
            }

            Vector3 target = _pathPoints[_currentPathIndex].position;
            Vector3 direction = target - _transform.position;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                _currentPathIndex++;
                return;
            }

            _moveDirection = direction.normalized;
            float step = _data.MoveSpeed * Time.deltaTime;
            _transform.position += _moveDirection * Mathf.Min(step, distance);
        }

        /// <summary>
        /// 受到伤害。
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!_isActive) return;

            // 护甲减伤
            float actualDamage = damage * (1f - _data.Armor);
            _currentHealth -= actualDamage;
            UpdateHealthBar();

            // 显示伤害飘字（偶尔显示，避免太密集）
            if (Random.value < 0.4f && TowerDefense.UI.FloatingTextManager.Instance != null)
            {
                TowerDefense.UI.FloatingTextManager.Instance.ShowDamage(_transform.position, actualDamage);
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 敌人死亡。
        /// </summary>
        private void Die()
        {
            _isActive = false;

            // 显示金币飘字
            if (TowerDefense.UI.FloatingTextManager.Instance != null)
            {
                TowerDefense.UI.FloatingTextManager.Instance.ShowGold(_transform.position, _data.RewardGold);
            }

            // 死亡爆炸特效
            SpawnDeathEffect();

            // 触发击杀事件
            EventBus.Publish(new EnemyKilledEvent
            {
                RewardGold = _data.RewardGold,
                Position = _transform.position
            });

            // 分裂逻辑
            if (_data.SplitOnDeath && _data.SplitChildData != null && _data.SplitCount > 0)
            {
                SpawnSplitChildren();
            }

            // 归还对象池
            EnemySpawner.Instance.ReturnEnemy(this);
        }

        /// <summary>
        /// 死亡爆炸特效（短暂扩散+淡出）。
        /// </summary>
        private void SpawnDeathEffect()
        {
            var effectGo = new GameObject("DeathEffect");
            effectGo.transform.position = _transform.position;
            var sr = effectGo.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateDeathSprite();
            sr.color = new Color(1f, 0.9f, 0.5f, 0.8f);
            sr.sortingOrder = 12;
            effectGo.transform.localScale = Vector3.one * 0.3f;
            effectGo.AddComponent<DeathEffectAnim>();
        }

        private static Sprite GenerateDeathSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - dist / (size * 0.5f));
                    pixels[y * size + x] = new Color(1, 1, 0.8f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 分裂出小敌人。
        /// </summary>
        private void SpawnSplitChildren()
        {
            for (int i = 0; i < _data.SplitCount; i++)
            {
                var child = EnemySpawner.Instance.SpawnEnemy(_data.SplitChildData, _pathPoints);
                if (child != null)
                {
                    // 子敌人从当前位置继续沿路径前进
                    child.transform.position = _transform.position + Random.insideUnitSphere * 0.3f;
                    child.SetPathIndex(_currentPathIndex);
                }
            }
        }

        /// <summary>
        /// 设置当前路径索引（用于分裂敌人从中间位置继续）。
        /// </summary>
        public void SetPathIndex(int index)
        {
            _currentPathIndex = Mathf.Clamp(index, 0, _pathPoints.Length - 1);
        }

        /// <summary>
        /// 到达终点。
        /// </summary>
        private void ReachEnd()
        {
            _isActive = false;
            // 显示生命损失飘字
            if (TowerDefense.UI.FloatingTextManager.Instance != null)
            {
                TowerDefense.UI.FloatingTextManager.Instance.ShowLifeLost(_transform.position, _data.Damage);
            }
            EventBus.Publish(new EnemyReachedEndEvent { Damage = _data.Damage });
            EnemySpawner.Instance.ReturnEnemy(this);
        }

        /// <summary>
        /// 强制停用（对象池回收时调用）。
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 敌人死亡爆炸动画。
    /// </summary>
    public class DeathEffectAnim : MonoBehaviour
    {
        private float _timer;
        private float _duration = 0.35f;
        private SpriteRenderer _sr;
        private Color _startColor;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _startColor = _sr.color;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);
            // 扩散
            float scale = Mathf.Lerp(0.3f, 1.2f, t);
            transform.localScale = Vector3.one * scale;
            // 淡出
            if (_sr != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - t);
                _sr.color = c;
            }
            if (_timer >= _duration) Destroy(gameObject);
        }
    }
}
