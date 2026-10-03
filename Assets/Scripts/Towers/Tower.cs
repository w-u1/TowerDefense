using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Systems;

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
        private Obstacle _targetObstacle;
        private Transform _transform;
        private bool _isPlaced = false;
        private bool _isRecoiling = false;

        // 范围内敌人缓存（复用List避免GC）
        private static readonly List<Enemy> _enemiesInRange = new List<Enemy>(64);

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
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                if (_bodyRenderer == null) _bodyRenderer = visual.GetComponent<SpriteRenderer>();
                if (_turretPivot == null) _turretPivot = visual;
            }
            if (_topRenderer == null && visual != null)
            {
                _topRenderer = visual.GetComponent<SpriteRenderer>();
            }
            if (_muzzlePoint == null)
            {
                var muzzle = transform.Find("Visual/Muzzle");
                if (muzzle != null) _muzzlePoint = muzzle;
            }
        }

        /// <summary>
        /// 应用视觉配置。
        /// </summary>
        private void ApplyVisuals()
        {
            // 塔的颜色和造型已由 TowerVisualFactory 程序化生成在精灵中
            // 这里只控制整体缩放
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

            // 辅助塔：不攻击，只给周围塔加buff
            if (_data.IsSupportTower)
            {
                UpdateSupportBuff();
                return;
            }

            UpdateTarget();
            UpdateAttack();
            UpdateTurretRotation();
            UpdateLaserBeam();
        }

        /// <summary>
        /// 辅助塔：给范围内的其他塔加攻速和伤害buff。
        /// </summary>
        private void UpdateSupportBuff()
        {
            // 从TowerPlacer获取所有已建造的塔
            var allTowers = TowerPlacer.Instance.GetBuiltTowers();
            float buffRange = _currentRange;

            foreach (var tower in allTowers)
            {
                if (tower == null || tower == this) continue;
                if (tower.Data.IsSupportTower) continue; // 辅助塔不叠加

                float dist = Vector3.Distance(tower.transform.position, transform.position);
                if (dist <= buffRange)
                {
                    tower.AddSupportBuff(_data.SupportAttackSpeedBonus, _data.SupportDamageBonus);
                }
                else
                {
                    tower.RemoveSupportBuff(this);
                }
            }
        }

        /// <summary>
        /// 被辅助塔施加的buff（每帧调用，记录buff来源）。
        /// </summary>
        private readonly List<Tower> _supportSources = new List<Tower>();
        private float _buffAttackSpeedMult = 1f;
        private float _buffDamageMult = 1f;

        public void AddSupportBuff(float attackSpeedBonus, float damageBonus)
        {
            // 简单处理：取最高buff（不叠加多个辅助塔）
            _buffAttackSpeedMult = Mathf.Max(_buffAttackSpeedMult, 1f - attackSpeedBonus);
            _buffDamageMult = Mathf.Max(_buffDamageMult, 1f + damageBonus);
        }

        public void RemoveSupportBuff(Tower source)
        {
            // 简化处理：不实时移除（辅助塔被卖掉时才清理）
            // 实际项目中应该重新计算所有buff，这里简化
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
            // 优先攻击选中的障碍物
            if (Obstacle.Selected != null)
            {
                float distToObs = Vector3.Distance(Obstacle.Selected.transform.position, _transform.position);
                if (distToObs <= _currentRange)
                {
                    _targetObstacle = Obstacle.Selected;
                    _currentTarget = null;
                    return;
                }
            }
            // 障碍物超出范围或未选中，清除障碍物目标
            if (_targetObstacle != null)
            {
                _targetObstacle = null;
            }

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
        /// 根据目标策略寻找敌人。使用EnemyRegistry空间网格加速。
        /// </summary>
        private Enemy FindTarget()
        {
            _enemiesInRange.Clear();

            // 从注册表获取范围内敌人（空间网格加速，替代FindObjectsOfType）
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.GetEnemiesInRange(_transform.position, _currentRange, _enemiesInRange);
            }

            // 过滤：根据塔类型决定能打什么敌人
            FilterTargetsByTowerType();

            if (_enemiesInRange.Count == 0) return null;

            Enemy best = null;
            float bestValue = float.MinValue;

            switch (_data.Targeting)
            {
                case TargetingStrategy.First:
                    // 最靠近终点 = x坐标最大（简化：假设路径从左到右）
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
        /// 根据塔类型过滤可攻击目标：
        /// - 隐身敌人：只有箭塔和激光塔能看到
        /// - 飞行敌人：炮塔打不到（地面炮弹），其他塔都能打
        /// </summary>
        private void FilterTargetsByTowerType()
        {
            for (int i = _enemiesInRange.Count - 1; i >= 0; i--)
            {
                var e = _enemiesInRange[i];
                if (e == null || e.Data == null)
                {
                    _enemiesInRange.RemoveAt(i);
                    continue;
                }

                // 隐身敌人：只有 Archer 和 Laser 能攻击
                if (e.Data.IsStealth)
                {
                    if (_data.Type != TowerType.Archer && _data.Type != TowerType.Laser)
                    {
                        _enemiesInRange.RemoveAt(i);
                        continue;
                    }
                }

                // 飞行敌人：Cannon 塔打不到（地面溅射）
                if (e.Data.IsFlying)
                {
                    if (_data.Type == TowerType.Cannon)
                    {
                        _enemiesInRange.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 更新攻击逻辑。
        /// </summary>
        private void UpdateAttack()
        {
            _attackTimer -= Time.deltaTime;

            if (_currentTarget == null && _targetObstacle == null) return;
            if (_attackTimer > 0) return;

            _attackTimer = _currentAttackInterval;
            Fire();
        }

        /// <summary>
        /// 开火。
        /// </summary>
        private void Fire()
        {
            // 攻击音效
            AudioManager.Instance?.PlayTowerShoot(_data.Type);

            Vector3 muzzlePos = _muzzlePoint != null ? _muzzlePoint.position : _transform.position;

            // 优先攻击选中的障碍物
            if (_targetObstacle != null)
            {
                _targetObstacle.TakeDamage(_currentDamage);
                // 命中特效（根据塔类型变色）
                Color hitColor = GetTowerColor();

                if (_data.IsContinuousDamage)
                {
                    // 激光塔：显示激光束到障碍物
                    ShowLaserBeam(muzzlePos, _targetObstacle.transform.position);
                }
                else
                {
                    // 投射物塔：发射子弹飞行特效
                    StartCoroutine(FireProjectileAtObstacle(muzzlePos, _targetObstacle.transform.position, hitColor));
                }

                EffectsPool.Instance?.SpawnHitEffect(_targetObstacle.transform.position, hitColor, 1.2f, 0.5f);
                // 后坐力动画
                if (!_isRecoiling)
                {
                    _isRecoiling = true;
                    StartCoroutine(RecoilCoroutine());
                }
                EffectsPool.Instance?.SpawnMuzzleFlash(muzzlePos,
                    new Color(1f, 0.9f, 0.4f, 0.8f), 0.3f);
                return;
            }

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

            // 后坐力动画
            if (!_isRecoiling)
            {
                _isRecoiling = true;
                StartCoroutine(RecoilCoroutine());
            }
            // 炮口闪光（走对象池）
            EffectsPool.Instance?.SpawnMuzzleFlash(muzzlePos,
                new Color(1f, 0.9f, 0.4f, 0.8f), 0.3f);
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

        /// <summary>根据塔类型返回代表色（用于命中特效）。</summary>
        private Color GetTowerColor()
        {
            switch (_data.Type)
            {
                case TowerType.Archer: return new Color(0.5f, 0.9f, 0.5f);
                case TowerType.Cannon: return new Color(1f, 0.5f, 0.2f);
                case TowerType.Frost: return new Color(0.4f, 0.8f, 1f);
                case TowerType.Laser: return new Color(1f, 0.3f, 0.9f);
                case TowerType.Poison: return new Color(0.6f, 1f, 0.3f);
                default: return Color.white;
            }
        }

        /// <summary>向障碍物发射子弹飞行特效。</summary>
        private System.Collections.IEnumerator FireProjectileAtObstacle(Vector3 from, Vector3 to, Color color)
        {
            var bulletGo = new GameObject("ObstacleBullet");
            bulletGo.transform.position = from;
            var sr = bulletGo.AddComponent<SpriteRenderer>();
            sr.sprite = ProjectileVisualFactory.GetProjectileSprite(_data.Type);
            sr.color = color;
            sr.sortingOrder = 9;
            bulletGo.transform.localScale = Vector3.one * 0.8f;

            float duration = 0.15f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                bulletGo.transform.position = Vector3.Lerp(from, to, t);
                // 子弹朝向目标
                Vector3 dir = to - from;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                bulletGo.transform.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            Destroy(bulletGo);
        }

        /// <summary>
        /// 后坐力协程。
        /// </summary>
        private System.Collections.IEnumerator RecoilCoroutine()
        {
            if (_turretPivot == null) yield break;
            Vector3 original = new Vector3(0, 0.15f, 0);
            float recoilDist = 0.08f;
            float recoilTime = 0.06f;

            float t = 0;
            while (t < recoilTime)
            {
                t += Time.deltaTime;
                float p = t / recoilTime;
                _turretPivot.localPosition = Vector3.Lerp(original, original + Vector3.back * recoilDist, p);
                yield return null;
            }
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
        /// 更新炮塔旋转朝向目标。
        /// </summary>
        private void UpdateTurretRotation()
        {
            if (_turretPivot == null) return;

            Vector3 targetPos;
            if (_targetObstacle != null)
                targetPos = _targetObstacle.transform.position;
            else if (_currentTarget != null)
                targetPos = _currentTarget.transform.position;
            else
                return;

            Vector3 direction = targetPos - _turretPivot.position;
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

            // 升级音效
            AudioManager.Instance?.PlayUpgrade();

            return true;
        }

        /// <summary>
        /// 根据等级更新塔的外观。
        /// </summary>
        private void UpdateVisualAppearance()
        {
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = Vector3.one * 1.5f * (1f + _currentLevel * 0.15f);
            }

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
                    Color auraColor;
                    switch (_currentLevel)
                    {
                        case 1: auraColor = new Color(0.3f, 0.7f, 1f, 0.5f); break;
                        case 2: auraColor = new Color(0.8f, 0.4f, 1f, 0.6f); break;
                        default: auraColor = new Color(1f, 0.85f, 0.2f, 0.7f); break;
                    }
                    auraRendererComp.color = auraColor;
                    aura.localScale = Vector3.one * (1.6f + _currentLevel * 0.3f);
                }

                // 毒塔特殊升级外观：绿色毒液泡
                if (_data.Type == TowerType.Poison && _currentLevel >= 1)
                {
                    var poisonFx = transform.Find("PoisonUpgradeFx");
                    if (poisonFx == null)
                    {
                        var fxGo = new GameObject("PoisonUpgradeFx");
                        fxGo.transform.SetParent(transform);
                        fxGo.transform.localPosition = Vector3.zero;
                        var sr = fxGo.AddComponent<SpriteRenderer>();
                        sr.sprite = GeneratePoisonUpgradeSprite();
                        sr.sortingOrder = 8;
                        fxGo.transform.localScale = Vector3.one * 0.5f;
                    }
                    var srComp = poisonFx?.GetComponent<SpriteRenderer>();
                    if (srComp != null)
                    {
                        float alpha = 0.3f + _currentLevel * 0.15f;
                        srComp.color = new Color(0.4f, 1f, 0.2f, alpha);
                        poisonFx.localScale = Vector3.one * (0.5f + _currentLevel * 0.2f);
                    }
                }

                // 辅助塔特殊升级外观：金色光芒
                if (_data.Type == TowerType.Support && _currentLevel >= 1)
                {
                    var supportFx = transform.Find("SupportUpgradeFx");
                    if (supportFx == null)
                    {
                        var fxGo = new GameObject("SupportUpgradeFx");
                        fxGo.transform.SetParent(transform);
                        fxGo.transform.localPosition = Vector3.zero;
                        var sr = fxGo.AddComponent<SpriteRenderer>();
                        sr.sprite = GenerateSupportUpgradeSprite();
                        sr.sortingOrder = 8;
                        fxGo.transform.localScale = Vector3.one * 0.5f;
                    }
                    var srComp = supportFx?.GetComponent<SpriteRenderer>();
                    if (srComp != null)
                    {
                        float alpha = 0.4f + _currentLevel * 0.15f;
                        srComp.color = new Color(1f, 0.9f, 0.3f, alpha);
                        supportFx.localScale = Vector3.one * (0.6f + _currentLevel * 0.25f);
                    }
                }
            }
        }

        /// <summary>毒塔升级特效：绿色气泡圈。</summary>
        private static Sprite GeneratePoisonUpgradeSprite()
        {
            int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    // 外圈亮，内圈透明（气泡环）
                    float ring = Mathf.Abs(dist - radius) / (radius * 0.3f);
                    float alpha = Mathf.Clamp01(1f - ring) * 0.8f;
                    pixels[idx] = new Color(0.5f, 1f, 0.2f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>辅助塔升级特效：金色星形光芒。</summary>
        private static Sprite GenerateSupportUpgradeSprite()
        {
            int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;
                    // 星形：中心亮，边缘有尖刺感
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float spikes = 0.8f + 0.2f * Mathf.Cos(angle * 4f);
                    float r = size * 0.35f * spikes;
                    float alpha = Mathf.Clamp01(1f - dist / r) * 0.9f;
                    pixels[idx] = new Color(1f, 0.95f, 0.4f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

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
        /// 获取下一级升级数据。
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
        /// 获取出售返还金币数量。
        /// </summary>
        public int GetSellPrice()
        {
            int totalSpent = _data.BuildCost;
            for (int i = 0; i < _currentLevel && i < _data.Upgrades.Length; i++)
            {
                totalSpent += _data.Upgrades[i].Cost;
            }
            return Mathf.RoundToInt(totalSpent * _data.SellRefundRatio);
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
                float scale = _currentRange / _data.Size;
                rangeIndicator.localScale = new Vector3(scale, scale, 1f);

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
    /// 范围指示器脉冲动画。
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
            float pulse = Mathf.Sin(_time * 3f) * 0.06f + 1f;
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


