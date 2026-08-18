using UnityEngine;
using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Towers;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 新塔和新敌人的配置数据。
    /// </summary>
    public static class NewGameDataConfig
    {
        /// <summary>添加新塔：毒塔、辅助塔。</summary>
        public static void AddNewTowers(List<TowerData> towerList)
        {
            // 毒塔：持续DoT伤害，绿色毒液
            var poisonTower = ScriptableObject.CreateInstance<TowerData>();
            poisonTower.name = "TowerData_Poison";
            poisonTower.Type = TowerType.Poison;
            poisonTower.DisplayName = "毒塔";
            poisonTower.Description = "发射毒液弹，命中后使敌人中毒，持续3秒每秒造成8点伤害。\n适合对付高血量敌人，可叠加在其他伤害上。";
            poisonTower.BuildCost = 80;
            poisonTower.Range = 3.0f;
            poisonTower.Damage = 12f;
            poisonTower.AttackInterval = 1.0f;
            poisonTower.ProjectileSpeed = 9f;
            poisonTower.Targeting = TargetingStrategy.First;
            poisonTower.HasPoisonEffect = true;
            poisonTower.PoisonDps = 8f;
            poisonTower.PoisonDuration = 3f;
            poisonTower.BodyColor = new Color(0.3f, 0.6f, 0.2f);
            poisonTower.TopColor = new Color(0.5f, 0.9f, 0.3f);
            poisonTower.ProjectileColor = new Color(0.5f, 1f, 0.2f);
            poisonTower.Size = 0.9f;
            poisonTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 100, DamageMultiplier = 1.5f, RangeBonus = 0.3f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 180, DamageMultiplier = 1.7f, RangeBonus = 0.4f, AttackSpeedBonus = 0.15f }
            };
            towerList.Add(poisonTower);

            // 辅助塔：给周围塔加攻速和伤害buff
            var supportTower = ScriptableObject.CreateInstance<TowerData>();
            supportTower.name = "TowerData_Support";
            supportTower.Type = TowerType.Support;
            supportTower.DisplayName = "辅助塔";
            supportTower.Description = "不直接攻击，为范围内所有其他塔提供25%攻速和20%伤害加成。\n放在密集塔群中间效果最佳。";
            supportTower.BuildCost = 120;
            supportTower.Range = 3.5f;
            supportTower.Damage = 0f;
            supportTower.AttackInterval = 999f;
            supportTower.IsSupportTower = true;
            supportTower.SupportAttackSpeedBonus = 0.25f;
            supportTower.SupportDamageBonus = 0.2f;
            supportTower.BodyColor = new Color(0.7f, 0.6f, 0.2f);
            supportTower.TopColor = new Color(1f, 0.85f, 0.3f);
            supportTower.ProjectileColor = Color.clear;
            supportTower.Size = 0.9f;
            supportTower.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 150, DamageMultiplier = 1f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 250, DamageMultiplier = 1f, RangeBonus = 0.5f, AttackSpeedBonus = 0.15f }
            };
            towerList.Add(supportTower);
        }
    }
}
