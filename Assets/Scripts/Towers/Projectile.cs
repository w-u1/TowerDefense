using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Systems;

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

        // 复用列表避免GC
        private static readonly List<Enemy> _splashTargets = new List<Enemy>(32);

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
                // 子弹精灵已由 ProjectileVisualFactory 程序化生成，包含颜色，不再覆盖
                _spriteRenderer.color = Color.white;
                switch (towerData.Type)
                {
                    case TowerType.Archer:
                        _transform.localScale = Vector3.one * 1.0f;
                        break;
                    case TowerType.Cannon:
                        _transform.localScale = Vector3.one * 0.8f;
                        break;
                    case TowerType.Frost:
                        _transform.localScale = Vector3.one * 0.7f;
                        break;
                    case TowerType.Laser:
                        _transform.localScale = Vector3.one * 0.9f;
                        break;
                    case TowerType.Poison:
                        _transform.localScale = Vector3.one * 0.8f;
                        break;
                    default:
                        _transform.localScale = Vector3.one * 0.8f;
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

        private void MoveTowardsTarget()
        {
            if (_target != null && _target.IsActive)
            {
                _lastKnownTargetPosition = _target.transform.position;
            }

            Vector3 direction = _lastKnownTargetPosition - _transform.position;
            float distance = direction.magnitude;

            if (distance < 0.2f)
            {
                HitTarget();
                return;
            }

            if ((_target == null || !_target.IsActive) && distance < 0.3f)
            {
                Deactivate();
                return;
            }

            float step = _speed * Time.deltaTime;
            _transform.position += direction.normalized * Mathf.Min(step, distance);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void HitTarget()
        {
            // 命中特效走对象池
            Color effectColor = _towerData.ProjectileColor;
            float effectSize = 0.8f;
            switch (_towerData.Type)
            {
                case TowerType.Cannon:
                    effectColor = new Color(1f, 0.5f, 0.2f, 0.95f);
                    effectSize = _towerData.SplashRadius * 1.5f;
                    break;
                case TowerType.Frost:
                    effectColor = new Color(0.6f, 0.9f, 1f, 0.9f);
                    effectSize = 0.9f;
                    break;
                case TowerType.Laser:
                    effectColor = new Color(1f, 0.4f, 0.9f, 0.95f);
                    effectSize = 0.9f;
                    break;
                case TowerType.Archer:
                    effectColor = new Color(1f, 0.9f, 0.4f, 0.85f);
                    effectSize = 0.6f;
                    break;
                case TowerType.Poison:
                    effectColor = new Color(0.4f, 1f, 0.2f, 0.9f);
                    effectSize = 0.7f;
                    break;
            }
            EffectsPool.Instance?.SpawnHitEffect(_transform.position, effectColor, effectSize);

            // 范围伤害
            if (_towerData.HasSplashDamage)
            {
                ApplySplashDamage();
            }
            else if (_target != null && _target.IsActive)
            {
                _target.TakeDamage(_damage);
                ApplySlowEffect(_target);
                ApplyPoisonEffect(_target);
            }

            Deactivate();
        }

        /// <summary>
        /// 范围伤害（使用EnemyRegistry空间网格加速）。
        /// </summary>
        private void ApplySplashDamage()
        {
            _splashTargets.Clear();

            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.GetEnemiesInRange(_transform.position, _towerData.SplashRadius, _splashTargets);
            }

            foreach (var enemy in _splashTargets)
            {
                if (!enemy.IsActive) continue;
                float dist = Vector3.Distance(enemy.transform.position, _transform.position);
                // 距离衰减
                float falloff = 1f - (dist / _towerData.SplashRadius) * 0.5f;
                enemy.TakeDamage(_damage * falloff);
                ApplySlowEffect(enemy);
                ApplyPoisonEffect(enemy);
            }
        }

        /// <summary>
        /// 应用中毒DoT效果。
        /// </summary>
        private void ApplyPoisonEffect(Enemy enemy)
        {
            if (!_towerData.HasPoisonEffect) return;
            enemy.ApplyPoison(_towerData.PoisonDps, _towerData.PoisonDuration);
        }

        /// <summary>
        /// 应用减速效果。
        /// </summary>
        private void ApplySlowEffect(Enemy enemy)
        {
            if (!_towerData.HasSlowEffect) return;
            enemy.ApplySlow(_towerData.SlowAmount, _towerData.SlowDuration);
        }

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
}
