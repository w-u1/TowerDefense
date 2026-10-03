using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Systems;

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

        // 空间网格索引（由EnemyRegistry维护）
        [System.NonSerialized] public int GridIndex = -1;

        // 速度倍率（减速效果）
        private float _speedMultiplier = 1f;
        private float _slowTimer = 0f;
        private float _slowAmount = 0f;

        // 中毒DoT效果
        private float _poisonDps = 0f;
        private float _poisonTimer = 0f;
        private float _poisonTickTimer = 0f;
        private SpriteRenderer _poisonOverlay;

        // 血条
        private GameObject _healthBarRoot;
        private SpriteRenderer _healthBarBg;
        private SpriteRenderer _healthBarFill;
        private float _healthBarWidth = 0.8f;

        // 冰冻视觉效果
        private SpriteRenderer _freezeOverlay;

        // 引用缓存
        private Transform _transform;

        public EnemyData Data => _data;
        public float CurrentHealth => _currentHealth;
        public bool IsActive => _isActive;
        public Vector3 MoveDirection => _moveDirection;
        public float SpeedMultiplier => _speedMultiplier;

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
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _data = data;
            _pathPoints = pathPoints;
            _currentHealth = data.MaxHealth;
            _currentPathIndex = 0;
            _isActive = true;
            _speedMultiplier = 1f;
            _slowTimer = 0f;
            _slowAmount = 0f;


            // 强制刷新颜色（对象池复用可能残留旧颜色）
            if (_spriteRenderer != null) _spriteRenderer.color = data.BodyColor;

            // 应用视觉配置（素材精灵保持原色，只处理特殊状态）
            if (_spriteRenderer != null)
            {
                Color spriteColor = data.BodyColor; // 按关卡主题色染色

                // 隐身敌人：半透明
                if (data.IsStealth)
                {
                    spriteColor.a = 0.4f;
                    _spriteRenderer.sortingOrder = 5;
                }

                // 飞行敌人：在更高层级，视觉上浮
                if (data.IsFlying)
                {
                    _spriteRenderer.sortingOrder = 7;
                    _transform.localPosition = new Vector3(0, 0.4f, 0);
                }

                _spriteRenderer.color = spriteColor;
            }
            _transform.localScale = Vector3.one * data.Size;

            // 对象池复用时重置朝向（避免残留上一次的旋转）
            _transform.rotation = Quaternion.identity;
            _moveDirection = Vector3.zero;

            // 创建/更新血条
            CreateHealthBar();
            UpdateHealthBar();

            // 放置到路径起点
            if (_pathPoints != null && _pathPoints.Length > 0)
            {
                _transform.position = _pathPoints[0].position;
            }

            // 注册到敌人注册表
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Register(this);
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

            // 减速计时
            if (_slowTimer > 0)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0)
                {
                    _speedMultiplier = 1f;
                    SetFreezeVisual(false);
                }
            }

            // 中毒DoT计时
            if (_poisonTimer > 0)
            {
                _poisonTimer -= Time.deltaTime;
                _poisonTickTimer -= Time.deltaTime;
                // 每0.5秒跳一次伤害
                if (_poisonTickTimer <= 0f)
                {
                    _poisonTickTimer = 0.5f;
                    TakeDamage(_poisonDps * 0.5f);
                }
                if (_poisonTimer <= 0)
                {
                    SetPoisonVisual(false);
                }
            }

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
            float step = _data.MoveSpeed * _speedMultiplier * Time.deltaTime;
            _transform.position += _moveDirection * Mathf.Min(step, distance);
            UpdateFacing();

            // 更新网格位置（每隔几格更新一次即可，这里每帧都更新因为敌人在移动）
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.UpdateGridPosition(this);
            }
        }

        /// <summary>
        /// 根据移动方向旋转敌人精灵，让"头"朝向移动方向。
        /// 僵尸/机器人头朝右(+x)；飞机头朝上(+y)。
        /// </summary>
        private void UpdateFacing()
        {
            if (_moveDirection.sqrMagnitude < 0.01f) return;

            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;

            if (_data.IsFlying)
            {
                // 飞机机头朝上，需要额外 -90 度偏移
                angle -= 90f;
            }

            _transform.rotation = Quaternion.Euler(0, 0, angle);

            // 血条反向补偿，保持水平不随敌人转动
            if (_healthBarRoot != null)
            {
                _healthBarRoot.transform.rotation = Quaternion.Inverse(_transform.rotation);
            }
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
        /// 应用减速效果。
        /// </summary>
        public void ApplySlow(float slowAmount, float duration)
        {
            if (slowAmount >= _slowAmount || _slowTimer <= 0)
            {
                _slowAmount = slowAmount;
                _speedMultiplier = 1f - slowAmount;
            }
            _slowTimer = Mathf.Max(_slowTimer, duration);
            SetFreezeVisual(true);
        }

        /// <summary>
        /// 应用中毒DoT效果。
        /// </summary>
        public void ApplyPoison(float dps, float duration)
        {
            // 刷新中毒（取更高DPS）
            if (dps >= _poisonDps || _poisonTimer <= 0)
            {
                _poisonDps = dps;
            }
            _poisonTimer = Mathf.Max(_poisonTimer, duration);
            _poisonTickTimer = 0f; // 立即跳一次
            SetPoisonVisual(true);
        }

        /// <summary>
        /// 设置中毒视觉效果（绿色气泡）。
        /// </summary>
        private void SetPoisonVisual(bool active)
        {
            if (_poisonOverlay == null && active)
            {
                var go = new GameObject("PoisonOverlay");
                go.transform.SetParent(_transform);
                go.transform.localPosition = Vector3.zero;
                _poisonOverlay = go.AddComponent<SpriteRenderer>();
                _poisonOverlay.sprite = GeneratePoisonSprite();
                _poisonOverlay.color = new Color(0.4f, 0.9f, 0.3f, 0.5f);
                _poisonOverlay.sortingOrder = 7;
                go.transform.localScale = Vector3.one * 1.1f;
            }

            if (_poisonOverlay != null)
            {
                _poisonOverlay.gameObject.SetActive(active);
            }
        }

        private static Sprite GeneratePoisonSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    if (dist < radius)
                    {
                        // 气泡感：边缘更亮
                        float bubble = 1f - Mathf.Abs(dist - radius * 0.6f) / (radius * 0.4f);
                        bubble = Mathf.Clamp01(bubble);
                        float alpha = 0.2f + bubble * 0.3f;
                        pixels[idx] = new Color(0.5f, 1f, 0.3f, alpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// 设置冰冻视觉效果。
        /// </summary>
        private void SetFreezeVisual(bool active)
        {
            if (_freezeOverlay == null && active)
            {
                // 创建冰冻覆盖层（半透明蓝色冰晶）
                var go = new GameObject("FreezeOverlay");
                go.transform.SetParent(_transform);
                go.transform.localPosition = Vector3.zero;
                _freezeOverlay = go.AddComponent<SpriteRenderer>();
                _freezeOverlay.sprite = GenerateFreezeSprite();
                _freezeOverlay.color = new Color(0.5f, 0.85f, 1f, 0.45f);
                _freezeOverlay.sortingOrder = 6; // 在敌人身体上面
                go.transform.localScale = Vector3.one * 1.15f;
            }

            if (_freezeOverlay != null)
            {
                _freezeOverlay.gameObject.SetActive(active);
            }
        }

        private static Sprite GenerateFreezeSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    if (dist < radius)
                    {
                        // 边缘更亮（冰晶感）
                        float edge = 1f - Mathf.Abs(dist - radius * 0.8f) / (radius * 0.2f);
                        edge = Mathf.Clamp01(edge);
                        float alpha = 0.2f + edge * 0.3f;
                        pixels[idx] = new Color(0.7f, 0.9f, 1f, alpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>
        /// 敌人死亡。
        /// </summary>
        private void Die()
        {
            _isActive = false;

            // 从注册表注销
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
            }

            // 显示金币飘字
            if (TowerDefense.UI.FloatingTextManager.Instance != null)
            {
                TowerDefense.UI.FloatingTextManager.Instance.ShowGold(_transform.position, _data.RewardGold);
            }

            // 死亡爆炸特效（走对象池）
            EffectsPool.Instance?.SpawnDeathEffect(_transform.position);

            // 死亡音效
            AudioManager.Instance?.PlayEnemyDeath();

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
            if (_pathPoints != null && _pathPoints.Length > 0)
            {
                _currentPathIndex = Mathf.Clamp(index, 0, _pathPoints.Length - 1);
            }
        }

        /// <summary>
        /// 到达终点。
        /// </summary>
        private void ReachEnd()
        {
            _isActive = false;

            // 从注册表注销
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
            }

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

            // 确保从注册表注销
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
            }

            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 敌人死亡爆炸动画（池化版本，由EffectsPool管理）。
    /// </summary>
    public class DeathEffectAnim : MonoBehaviour
    {
        private float _timer;
        private float _duration = 0.35f;
        private SpriteRenderer _sr;
        private Color _startColor;
        private Vector3 _startScale;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Play(Vector3 position, float size)
        {
            transform.position = position;
            transform.localScale = Vector3.one * size * 0.35f;
            _startScale = transform.localScale;
            _timer = 0f;
            if (_sr != null)
            {
                _startColor = Color.white;
                _sr.color = _startColor;
            }
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);
            // 扩散
            float scale = Mathf.Lerp(_startScale.x, _startScale.x * 3.2f, t);
            transform.localScale = new Vector3(scale, scale, 1f);
            // 淡出
            if (_sr != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - t);
                _sr.color = c;
            }
            if (_timer >= _duration)
            {
                gameObject.SetActive(false);
            }
        }
    }
}



