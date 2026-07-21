using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 防御塔类型枚举。
    /// </summary>
    public enum TowerType
    {
        /// <summary>箭塔：单体物理伤害，攻速快</summary>
        Archer,
        /// <summary>炮塔：范围伤害，攻速慢</summary>
        Cannon,
        /// <summary>冰塔：减速敌人，伤害低</summary>
        Frost,
        /// <summary>激光塔：持续伤害，高DPS</summary>
        Laser,
        /// <summary>毒塔：持续DoT伤害</summary>
        Poison,
        /// <summary>辅助塔：提升周围塔的攻速和伤害</summary>
        Support
    }

    /// <summary>
    /// 目标选择策略。
    /// </summary>
    public enum TargetingStrategy
    {
        /// <summary>最靠近终点的敌人</summary>
        First,
        /// <summary>最靠近塔的敌人</summary>
        Closest,
        /// <summary>血量最高的敌人</summary>
        Strongest,
        /// <summary>血量最低的敌人</summary>
        Weakest
    }

    /// <summary>
    /// 防御塔数据配置（ScriptableObject）。
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData_", menuName = "TowerDefense/Tower Data", order = 1)]
    public class TowerData : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("塔类型")]
        public TowerType Type = TowerType.Archer;

        [Tooltip("显示名称")]
        public string DisplayName = "箭塔";

        [Tooltip("塔描述（商店里显示）")]
        [TextArea] public string Description = "";

        [Tooltip("建造花费")]
        [Min(0)] public int BuildCost = 50;

        [Tooltip("出售返还比例")]
        [Range(0f, 1f)] public float SellRefundRatio = 0.7f;

        [Header("战斗属性")]
        [Tooltip("攻击范围")]
        [Range(1f, 15f)] public float Range = 3f;

        [Tooltip("伤害")]
        [Min(0f)] public float Damage = 10f;

        [Tooltip("攻击间隔（秒）")]
        [Range(0.1f, 5f)] public float AttackInterval = 1f;

        [Tooltip("子弹飞行速度")]
        [Range(1f, 30f)] public float ProjectileSpeed = 8f;

        [Tooltip("目标选择策略")]
        public TargetingStrategy Targeting = TargetingStrategy.First;

        [Header("特殊效果")]
        [Tooltip("是否造成范围伤害")]
        public bool HasSplashDamage = false;

        [Tooltip("范围伤害半径")]
        [Range(0.5f, 5f)] public float SplashRadius = 1.5f;

        [Tooltip("是否减速敌人")]
        public bool HasSlowEffect = false;

        [Tooltip("减速比例（0-1）")]
        [Range(0f, 0.9f)] public float SlowAmount = 0.4f;

        [Tooltip("减速持续时间（秒）")]
        [Range(0.1f, 5f)] public float SlowDuration = 2f;

        [Tooltip("是否持续伤害（激光）")]
        public bool IsContinuousDamage = false;

        [Tooltip("是否施加中毒DoT")]
        public bool HasPoisonEffect = false;

        [Tooltip("中毒每秒伤害")]
        public float PoisonDps = 5f;

        [Tooltip("中毒持续时间（秒）")]
        public float PoisonDuration = 3f;

        [Tooltip("是否是辅助塔（增益周围塔）")]
        public bool IsSupportTower = false;

        [Tooltip("攻速加成（对周围塔）")]
        [Range(0f, 0.5f)] public float SupportAttackSpeedBonus = 0.2f;

        [Tooltip("伤害加成（对周围塔）")]
        [Range(0f, 1f)] public float SupportDamageBonus = 0.2f;

        [Header("视觉配置")]
        [Tooltip("塔主体颜色")]
        public Color BodyColor = Color.blue;

        [Tooltip("塔顶颜色")]
        public Color TopColor = Color.cyan;

        [Tooltip("子弹颜色")]
        public Color ProjectileColor = Color.yellow;

        [Tooltip("塔尺寸")]
        [Range(0.5f, 2f)] public float Size = 1f;

        [Header("升级")]
        [Tooltip("升级数据（可选，每级一份）")]
        public TowerUpgradeData[] Upgrades;
    }

    /// <summary>
    /// 防御塔升级数据。
    /// </summary>
    [System.Serializable]
    public class TowerUpgradeData
    {
        [Tooltip("升级花费")]
        public int Cost = 100;

        [Tooltip("伤害加成倍率")]
        public float DamageMultiplier = 1.5f;

        [Tooltip("范围加成")]
        public float RangeBonus = 0.5f;

        [Tooltip("攻速加成（间隔减少比例）")]
        [Range(0f, 0.5f)] public float AttackSpeedBonus = 0.1f;
    }
}
