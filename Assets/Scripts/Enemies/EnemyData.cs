using UnityEngine;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// 敌人类型枚举。
    /// </summary>
    public enum EnemyType
    {
        /// <summary>普通敌人：均衡属性</summary>
        Normal,
        /// <summary>快速敌人：低血量高速度</summary>
        Fast,
        /// <summary>坦克敌人：高血量低速度</summary>
        Tank,
        /// <summary>Boss：极高血量，每5波出现</summary>
        Boss,
        /// <summary>精英敌人：高血量高奖励，中型</summary>
        Elite
    }

    /// <summary>
    /// 敌人数据配置（ScriptableObject）。每种敌人一份配置，便于策划调参。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData_", menuName = "TowerDefense/Enemy Data", order = 0)]
    public class EnemyData : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("敌人类型")]
        public EnemyType Type = EnemyType.Normal;

        [Tooltip("显示名称")]
        public string DisplayName = "敌人";

        [Tooltip("最大生命值")]
        [Min(1)] public int MaxHealth = 100;

        [Tooltip("移动速度（单位/秒）")]
        [Range(0.5f, 10f)] public float MoveSpeed = 2f;

        [Tooltip("到达终点造成的伤害")]
        [Min(1)] public int Damage = 1;

        [Tooltip("击杀奖励金币")]
        [Min(0)] public int RewardGold = 10;

        [Header("视觉配置")]
        [Tooltip("敌人颜色（用于程序化生成的Sprite）")]
        public Color BodyColor = Color.red;

        [Tooltip("敌人尺寸")]
        [Range(0.3f, 3f)] public float Size = 0.8f;

        [Header("特殊属性")]
        [Tooltip("是否有护甲（减少受到的伤害百分比）")]
        [Range(0f, 0.9f)] public float Armor = 0f;

        [Tooltip("死亡时是否分裂出小敌人")]
        public bool SplitOnDeath = false;

        [Tooltip("分裂数量")]
        [Min(0)] public int SplitCount = 0;

        [Tooltip("分裂出的敌人数据")]
        public EnemyData SplitChildData = null;
    }
}
