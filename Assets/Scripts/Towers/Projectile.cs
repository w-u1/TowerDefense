using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 投射物（子弹）。从塔发射向目标，命中后造成伤害并触发特殊效果。
    /// 使用对象池管理生命周期。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // 运行时数据
        private TowerData _towerData;
        private Enemy _target;
        private float _damage;
        private float _speed;
        private bool _isActive = false;
        private Vector3 _lastKnownTargetPosition;
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 初始化投射物。
        /// </summary>
        public void Initialize(TowerData towerData, Enemy target, Vector3 startPosition, float damage)
        {
            // 懒初始化：从对象池取出的未激活对象Awake可能尚未执行
            if (_transform == null) _transform = transform;
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            _towerData = towerData;
            _target = target;
            _damage = damage;
            _speed = towerData.ProjectileSpeed;
            _isActive = true;

            _transform.position = startPosition;
            _lastKnownTargetPosition = target != null ? target.transform.position : startPosition;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = towerData.ProjectileColor;
                // 根据塔类型调整投射物大小和形状
                switch (towerData.Type)
                {
                    case TowerType.Archer:
                        _transform.localScale = new Vector3(0.7f, 0.15f, 1f); // 细长箭矢
                        break;
                    case TowerType.Cannon:
                        _transform.localScale = new Vector3(0.7f, 0.7f, 1f); // 大圆炮弹
                        break;
                    case TowerType.Frost:
                        _transform.localScale = new Vector3(0.45f, 0.45f, 1f); // 小冰锥
                        break;
                    case TowerType.Laser:
                        // 激光：长条光束，两端发光
                        _transform.localScale = new Vector3(0.9f, 0.12f, 1f);
                        _spriteRenderer.color = new Color(1f, 0.4f, 0.9f, 0.9f);
                        // 添加发光效果（通过第二个Sprite）
                        break;
                }
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;
            MoveTowardsTarget();
        }

        /// <summary>
        /// 追踪目标移动。
        /// </summary>
        private void MoveTowardsTarget()
        {
            // 更新目标位置（如果目标存活）
            if (_target != null && _target.IsActive)
            {
                _lastKnownTargetPosition = _target.transform.position;
            }

            Vector3 direction = _lastKnownTargetPosition - _transform.position;
            float distance = direction.magnitude;

            // 命中检测
            if (distance < 0.2f)
            {
                HitTarget();
                return;
            }

            // 目标已失效且到达最后已知位置
            if ((_target == null || !_target.IsActive) && distance < 0.3f)
            {
                Deactivate();
                return;
            }

            float step = _speed * Time.deltaTime;
            _transform.position += direction.normalized * Mathf.Min(step, distance);

            // 旋转朝向目标
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 命中目标。
        /// </summary>
        private void HitTarget()
        {
            // 产生命中特效
            SpawnHitEffect();

            // 范围伤害
            if (_towerData.HasSplashDamage)
            {
                ApplySplashDamage();
            }
            else if (_target != null && _target.IsActive)
            {
                _target.TakeDamage(_damage);
                ApplySlowEffect(_target);
            }

            Deactivate();
        }

        /// <summary>
        /// 根据塔类型产生命中特效。
        /// </summary>
        private void SpawnHitEffect()
        {
            if (_towerData == null) return;

            Color effectColor = _towerData.ProjectileColor;
            float effectSize = 0.5f;

            switch (_towerData.Type)
            {
                case TowerType.Cannon:
                    effectColor = new Color(1f, 0.5f, 0.2f, 0.8f);
                    effectSize = _towerData.SplashRadius * 1.2f;
                    break;
                case TowerType.Frost:
                    effectColor = new Color(0.6f, 0.9f, 1f, 0.7f);
                    effectSize = 0.6f;
                    break;
                case TowerType.Laser:
                    effectColor = new Color(1f, 0.4f, 0.9f, 0.9f);
                    effectSize = 0.6f;
                    break;
                case TowerType.Archer:
                    effectColor = new Color(1f, 0.9f, 0.4f, 0.5f);
                    effectSize = 0.3f;
                    break;
            }

            // 创建特效对象（简单的扩散圆）
            var effectGo = new GameObject($"HitEffect_{_towerData.Type}");
            effectGo.transform.position = _transform.position;
            var renderer = effectGo.AddComponent<SpriteRenderer>();
            renderer.sprite = GenerateEffectSprite();
            renderer.color = effectColor;
            renderer.sortingOrder = 8;
            effectGo.transform.localScale = Vector3.one * effectSize * 0.3f;

            // 添加扩散动画组件
            var effect = effectGo.AddComponent<HitEffect>();
            effect.targetScale = effectSize;
            effect.duration = 0.3f;
        }

        /// <summary>
        /// 生成特效Sprite（径向渐变圆）。
        /// </summary>
        private static Sprite GenerateEffectSprite()
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
                        float alpha = 1f - (dist / radius);
                        pixels[idx] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 范围伤害。
        /// </summary>
        private void ApplySplashDamage()
        {
            // 查找范围内所有敌人（简单实现：遍历当前活跃敌人）
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();
            foreach (var enemy in allEnemies)
            {
                if (!enemy.IsActive) continue;
                float dist = Vector3.Distance(enemy.transform.position, _transform.position);
                if (dist <= _towerData.SplashRadius)
                {
                    // 距离衰减
                    float falloff = 1f - (dist / _towerData.SplashRadius) * 0.5f;
                    enemy.TakeDamage(_damage * falloff);
                    ApplySlowEffect(enemy);
                }
            }
        }

        /// <summary>
        /// 应用减速效果。
        /// </summary>
        private void ApplySlowEffect(Enemy enemy)
        {
            if (!_towerData.HasSlowEffect) return;
            // 减速通过修改敌人数据的MoveSpeed实现（简化版）
            // 实际项目中应有Buff系统，这里通过协程临时修改
            var slowEffect = enemy.gameObject.GetComponent<SlowEffect>();
            if (slowEffect == null)
            {
                slowEffect = enemy.gameObject.AddComponent<SlowEffect>();
            }
            slowEffect.Apply(_towerData.SlowAmount, _towerData.SlowDuration);
        }

        /// <summary>
        /// 停用并归还对象池。
        /// </summary>
        private void Deactivate()
        {
            _isActive = false;
            TowerPlacer.Instance.ReturnProjectile(this);
        }

        public void ForceDeactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 减速效果组件。临时降低敌人移动速度，持续一段时间后恢复。
    /// </summary>
    public class SlowEffect : MonoBehaviour
    {
        private float _originalSpeed;
        private float _slowTimer;
        private Enemy _enemy;
        private bool _isSlowed = false;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
        }

        public void Apply(float slowAmount, float duration)
        {
            if (!_isSlowed && _enemy != null)
            {
                _originalSpeed = _enemy.Data.MoveSpeed;
                _isSlowed = true;
            }
            _slowTimer = duration;
            // 直接修改data会影响所有同类型敌人，这里用一个临时字段
            // 简化处理：通过Enemy的一个临时速度倍率
        }

        private void Update()
        {
            if (_slowTimer > 0)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0)
                {
                    _isSlowed = false;
                    Destroy(this);
                }
            }
        }
    }

    /// <summary>
    /// 命中特效：扩散+淡出动画。
    /// </summary>
    public class HitEffect : MonoBehaviour
    {
        public float targetScale = 0.5f;
        public float duration = 0.3f;
        private float _timer;
        private SpriteRenderer _renderer;
        private Color _startColor;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _startColor = _renderer.color;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / duration);

            // 扩散
            float scale = Mathf.Lerp(transform.localScale.x, targetScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            // 淡出
            if (_renderer != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - t);
                _renderer.color = c;
            }

            if (_timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
