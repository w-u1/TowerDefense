using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 防御塔实体。自动锁定范围内敌人并攻击，支持多种目标策略和特殊效果。
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _topRenderer;
        [SerializeField] private Transform _turretPivot;
        [SerializeField] private Transform _muzzlePoint;

        // 配置数据
        private TowerData _data;
        private int _currentLevel = 0;

        // 运行时状态
        private float _currentDamage;
        private float _currentRange;
        private float _currentAttackInterval;
        private float _attackTimer = 0f;
        private Enemy _currentTarget;
        private Transform _transform;
        private bool _isPlaced = false;
        private bool _isRecoiling = false;

        // 范围内敌人缓存
        private readonly List<Enemy> _enemiesInRange = new List<Enemy>();

        public TowerData Data => _data;
        public int CurrentLevel => _currentLevel;
        public float CurrentDamage => _currentDamage;
        public float CurrentRange => _currentRange;
        public bool IsPlaced => _isPlaced;

        private void Awake()
        {
            _transform = transform;
        }

        /// <summary>
        /// 初始化塔（建造时调用）。
        /// </summary>
        public void Initialize(TowerData data)
        {
            _data = data;
            _currentLevel = 0;
            _currentDamage = data.Damage;
            _currentRange = data.Range;
            _currentAttackInterval = data.AttackInterval;
            _isPlaced = true;

            // 自动查找子对象引用（兼容程序化创建和手动挂载）
            AutoResolveReferences();

            // 应用视觉
            ApplyVisuals();
            UpdateLevelStats();
        }

        /// <summary>
        /// 自动查找子对象引用（当通过代码创建塔时，序列化字段未赋值，需自动查找）。
        /// </summary>
        private void AutoResolveReferences()
        {
            if (_bodyRenderer == null)
            {
                var body = transform.Find("Body");
                if (body != null) _bodyRenderer = body.GetComponent<SpriteRenderer>();
            }
            if (_topRenderer == null)
            {
                var turret = transform.Find("Turret");
                if (turret != null) _topRenderer = turret.GetComponent<SpriteRenderer>();
            }
            if (_turretPivot == null)
            {
                var turret = transform.Find("Turret");
                if (turret != null) _turretPivot = turret;
            }
            if (_muzzlePoint == null)
            {
                var muzzle = transform.Find("Turret/Muzzle");
                if (muzzle != null) _muzzlePoint = muzzle;
            }
        }

        /// <summary>
        /// 应用视觉配置。
        /// </summary>
        private void ApplyVisuals()
        {
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = _data.BodyColor;
            }
            if (_topRenderer != null)
            {
                _topRenderer.color = _data.TopColor;
            }
            _transform.localScale = Vector3.one * _data.Size;
        }

        /// <summary>
        /// 根据当前等级更新属性。
        /// </summary>
        private void UpdateLevelStats()
        {
            _currentDamage = _data.Damage;
            _currentRange = _data.Range;
            _currentAttackInterval = _data.AttackInterval;

            for (int i = 0; i < _currentLevel && i < _data.Upgrades.Length; i++)
            {
                var upgrade = _data.Upgrades[i];
                _currentDamage *= upgrade.DamageMultiplier;
                _currentRange += upgrade.RangeBonus;
                _currentAttackInterval *= (1f - upgrade.AttackSpeedBonus);
            }
        }

        private void Update()
        {
            if (!_isPlaced) return;

            UpdateTarget();
            UpdateAttack();
            UpdateTurretRotation();
            UpdateLaserBeam();
        }

        private void UpdateLaserBeam()
        {
            if (!_data.IsContinuousDamage) return;
            if (_currentTarget != null && _currentTarget.IsActive)
            {
                ShowLaserBeam(_transform.position, _currentTarget.transform.position);
            }
            else
            {
                HideLaserBeam();
            }
        }

        /// <summary>
        /// 更新目标锁定。
        /// </summary>
        private void UpdateTarget()
        {
            // 清理失效目标
            if (_currentTarget != null && (!_currentTarget.IsActive ||
                Vector3.Distance(_currentTarget.transform.position, _transform.position) > _currentRange))
            {
                _currentTarget = null;
            }

            // 寻找新目标
            if (_currentTarget == null)
            {
                _currentTarget = FindTarget();
            }
        }

        /// <summary>
        /// 根据目标策略寻找敌人。
        /// </summary>
        private Enemy FindTarget()
        {
            _enemiesInRange.Clear();
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();

            foreach (var enemy in allEnemies)
            {
                if (!enemy.IsActive) continue;
                float dist = Vector3.Distance(enemy.transform.position, _transform.position);
                if (dist <= _currentRange)
                {
                    _enemiesInRange.Add(enemy);
                }
            }

            if (_enemiesInRange.Count == 0) return null;

            Enemy best = null;
            float bestValue = float.MinValue;

            switch (_data.Targeting)
            {
                case TargetingStrategy.First:
                    // 最靠近终点 = 路径索引最大（简化：用位置x坐标，假设路径从左到右）
                    foreach (var e in _enemiesInRange)
                    {
                        if (e.transform.position.x > bestValue || best == null)
                        {
                            bestValue = e.transform.position.x;
                            best = e;
                        }
                    }
                    break;
                case TargetingStrategy.Closest:
                    bestValue = float.MaxValue;
                    foreach (var e in _enemiesInRange)
                    {
                        float d = Vector3.Distance(e.transform.position, _transform.position);
                        if (d < bestValue)
                        {
                            bestValue = d;
                            best = e;
                        }
                    }
                    break;
                case TargetingStrategy.Strongest:
                    foreach (var e in _enemiesInRange)
                    {
                        if (e.CurrentHealth > bestValue || best == null)
                        {
                            bestValue = e.CurrentHealth;
                            best = e;
                        }
                    }
                    break;
                case TargetingStrategy.Weakest:
                    bestValue = float.MaxValue;
                    foreach (var e in _enemiesInRange)
                    {
                        if (e.CurrentHealth < bestValue)
                        {
                            bestValue = e.CurrentHealth;
                            best = e;
                        }
                    }
                    break;
            }

            return best;
        }

        /// <summary>
        /// 更新攻击逻辑。
        /// </summary>
        private void UpdateAttack()
        {
            _attackTimer -= Time.deltaTime;

            if (_currentTarget == null) return;
            if (_attackTimer > 0) return;

            _attackTimer = _currentAttackInterval;
            Fire();
        }

        /// <summary>
        /// 开火。
        /// </summary>
        private void Fire()
        {
            Vector3 muzzlePos = _muzzlePoint != null ? _muzzlePoint.position : _transform.position;

            if (_data.IsContinuousDamage)
            {
                // 持续伤害：直接对目标造成伤害
                if (_currentTarget != null && _currentTarget.IsActive)
                {
                    _currentTarget.TakeDamage(_currentDamage * _currentAttackInterval);
                }
            }
            else
            {
                // 发射投射物
                TowerPlacer.Instance.SpawnProjectile(_data, _currentTarget, muzzlePos, _currentDamage);
            }

            // 后坐力动画：炮塔短暂后缩再恢复（避免重复协程）
            if (!_isRecoiling)
            {
                _isRecoiling = true;
                StartCoroutine(RecoilCoroutine());
            }
            // 炮口闪光
            MuzzleFlash(muzzlePos);
        }

        private GameObject _laserBeam;

        private void ShowLaserBeam(Vector3 from, Vector3 to)
        {
            if (_laserBeam == null)
            {
                _laserBeam = new GameObject("LaserBeam");
                var sr = _laserBeam.AddComponent<SpriteRenderer>();
                sr.sprite = GenerateBeamSprite();
                sr.sortingOrder = 8;
            }
            var beamSr = _laserBeam.GetComponent<SpriteRenderer>();
            beamSr.color = new Color(1f, 0.3f, 0.9f, 0.8f);

            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.1f) { _laserBeam.SetActive(false); return; }

            // 中心在两点中间
            _laserBeam.transform.position = (from + to) / 2f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _laserBeam.transform.rotation = Quaternion.Euler(0, 0, angle);
            _laserBeam.transform.localScale = new Vector3(length, 0.2f, 1f);
            _laserBeam.SetActive(true);
        }

        private void HideLaserBeam()
        {
            if (_laserBeam != null)
            {
                _laserBeam.SetActive(false);
                _laserBeam.transform.localScale = Vector3.one;
            }
        }

        private static Sprite GenerateBeamSprite()
        {
            int w = 32, h = 8;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dy = Mathf.Abs(y - h / 2f) / (h / 2f);
                    float a = 1f - dy;
                    tex.SetPixel(x, y, new Color(1, 0.5f, 1, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        /// <summary>
        /// 后坐力协程。
        /// </summary>
        private System.Collections.IEnumerator RecoilCoroutine()
        {
            if (_turretPivot == null) yield break;
            // 记录原始位置（固定值，避免多次协程叠加偏移）
            Vector3 original = new Vector3(0, 0.15f, 0);
            float recoilDist = 0.08f;
            float recoilTime = 0.06f;

            // 后缩
            float t = 0;
            while (t < recoilTime)
            {
                t += Time.deltaTime;
                float p = t / recoilTime;
                _turretPivot.localPosition = Vector3.Lerp(original, original + Vector3.back * recoilDist, p);
                yield return null;
            }
            // 恢复
            t = 0;
            while (t < recoilTime * 2)
            {
                t += Time.deltaTime;
                float p = t / (recoilTime * 2);
                _turretPivot.localPosition = Vector3.Lerp(original + Vector3.back * recoilDist, original, p);
                yield return null;
            }
            _turretPivot.localPosition = original;
            _isRecoiling = false;
        }

        /// <summary>
        /// 炮口闪光特效。
        /// </summary>
        private void MuzzleFlash(Vector3 pos)
        {
            var flash = new GameObject("MuzzleFlash");
            flash.transform.position = pos;
            var sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateFlashSprite();
            sr.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            sr.sortingOrder = 10;
            flash.transform.localScale = Vector3.one * 0.3f;
            // 快速消失
            Destroy(flash, 0.1f);
        }

        private static Sprite GenerateFlashSprite()
        {
            int size = 16;
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
        /// 更新炮塔旋转朝向目标。
        /// </summary>
        private void UpdateTurretRotation()
        {
            if (_turretPivot == null || _currentTarget == null) return;

            Vector3 direction = _currentTarget.transform.position - _turretPivot.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _turretPivot.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 升级塔。
        /// </summary>
        public bool Upgrade()
        {
            if (_currentLevel >= _data.Upgrades.Length) return false;

            var upgrade = _data.Upgrades[_currentLevel];
            if (!GameManager.Instance.TrySpendGold(upgrade.Cost)) return false;

            _currentLevel++;
            UpdateLevelStats();
            UpdateVisualAppearance();
            return true;
        }

        /// <summary>
        /// 根据等级更新塔的外观（颜色变亮、添加光环、放大等）。
        /// </summary>
        private void UpdateVisualAppearance()
        {
            // 塔顶（Turret）颜色随等级明显变化
            var turretRenderer = transform.Find("Turret")?.GetComponent<SpriteRenderer>();
            if (turretRenderer != null)
            {
                // 基于原始TopColor，每级明显变亮并偏白
                Color baseColor = _data.TopColor;
                float brightness = 1f + _currentLevel * 0.3f;
                Color c = new Color(
                    Mathf.Min(1f, baseColor.r * brightness),
                    Mathf.Min(1f, baseColor.g * brightness),
                    Mathf.Min(1f, baseColor.b * brightness),
                    1f);
                turretRenderer.color = c;
                // 塔顶随等级明显放大（每级25%）
                turretRenderer.transform.localScale = Vector3.one * (1f + _currentLevel * 0.25f);
            }

            // 底座（Body）同步放大
            var bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                bodyTransform.localScale = Vector3.one * (1f + _currentLevel * 0.12f);
            }

            // 添加/更新等级光环（1级以上显示）
            if (_currentLevel >= 1)
            {
                var aura = transform.Find("LevelAura");
                if (aura == null)
                {
                    var auraGo = new GameObject("LevelAura");
                    auraGo.transform.SetParent(transform);
                    auraGo.transform.localPosition = Vector3.zero;
                    var auraRenderer = auraGo.AddComponent<SpriteRenderer>();
                    auraRenderer.sprite = GenerateAuraSprite();
                    auraRenderer.sortingOrder = 1;
                    auraGo.transform.localScale = Vector3.one * 1.5f;
                    aura = auraGo.transform;
                }
                var auraRendererComp = aura.GetComponent<SpriteRenderer>();
                if (auraRendererComp != null)
                {
                    // 光环颜色随等级变化：1级蓝、2级紫、3级金
                    Color auraColor;
                    switch (_currentLevel)
                    {
                        case 1: auraColor = new Color(0.3f, 0.7f, 1f, 0.5f); break;
                        case 2: auraColor = new Color(0.8f, 0.4f, 1f, 0.6f); break;
                        default: auraColor = new Color(1f, 0.85f, 0.2f, 0.7f); break;
                    }
                    auraRendererComp.color = auraColor;
                    // 光环每级明显变大
                    aura.localScale = Vector3.one * (1.6f + _currentLevel * 0.3f);
                }
            }
        }

        /// <summary>
        /// 生成光环Sprite（径向渐变）。
        /// </summary>
        private static Sprite GenerateAuraSprite()
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
                        float alpha = (1f - dist / radius) * 0.6f;
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
        /// 获取下一级升级数据（没有则返回null）。
        /// </summary>
        public TowerUpgradeData GetNextUpgrade()
        {
            if (_currentLevel < _data.Upgrades.Length)
            {
                return _data.Upgrades[_currentLevel];
            }
            return null;
        }

        /// <summary>
        /// 出售塔，返还金币。
        /// </summary>
        public int Sell()
        {
            int totalSpent = _data.BuildCost;
            for (int i = 0; i < _currentLevel && i < _data.Upgrades.Length; i++)
            {
                totalSpent += _data.Upgrades[i].Cost;
            }
            int refund = Mathf.RoundToInt(totalSpent * _data.SellRefundRatio);
            GameManager.Instance.AddGold(refund);
            if (_laserBeam != null) Destroy(_laserBeam);
            Destroy(gameObject);
            return refund;
        }

        void OnDestroy()
        {
            if (_laserBeam != null) Destroy(_laserBeam);
        }

        /// <summary>
        /// 显示/隐藏攻击范围指示。
        /// </summary>
        public void ShowRangeIndicator(bool show)
        {
            var rangeIndicator = transform.Find("RangeIndicator");
            if (rangeIndicator != null)
            {
                rangeIndicator.gameObject.SetActive(show);
                // Sprite半径=1世界单位(pixelsPerUnit=64, texture=128)，塔自身有_size缩放
                // RangeIndicator是塔子对象，世界半径 = localScale * _data.Size * 1
                // 要世界半径 = _currentRange → localScale = _currentRange / _data.Size
                float scale = _currentRange / _data.Size;
                rangeIndicator.localScale = new Vector3(scale, scale, 1f);

                // 添加/移除脉冲动画
                var pulse = rangeIndicator.GetComponent<RangePulse>();
                if (show && pulse == null)
                {
                    rangeIndicator.gameObject.AddComponent<RangePulse>();
                }
                else if (!show && pulse != null)
                {
                    Destroy(pulse);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _currentRange);
        }
    }

    /// <summary>
    /// 范围指示器脉冲动画（呼吸效果）。
    /// </summary>
    public class RangePulse : MonoBehaviour
    {
        private Vector3 _baseScale;
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _time;

        private void Start()
        {
            _baseScale = transform.localScale;
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float pulse = Mathf.Sin(_time * 3f) * 0.06f + 1f; // 6%呼吸幅度
            transform.localScale = _baseScale * pulse;

            if (_renderer != null)
            {
                Color c = _baseColor;
                c.a = _baseColor.a * (0.85f + Mathf.Sin(_time * 3f) * 0.15f);
                _renderer.color = c;
            }
        }
    }
}
