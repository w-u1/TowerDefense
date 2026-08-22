using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Systems;

namespace TowerDefense.Tests.EditMode
{
    /// <summary>
    /// 敌人注册表测试：验证空间网格查找敌人数量正确。
    /// </summary>
    public class EnemyRegistryTest
    {
        private EnemyData _enemyData;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();

            // 清理可能残留的Singleton实例并销毁旧对象
            ClearSingletonAndDestroy<EnemyRegistry>();

            // 创建EnemyData
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            _enemyData.MaxHealth = 100;
            _enemyData.BodyColor = Color.red;

            // 创建EnemyRegistry实例（必须在创建Enemy之前，因为Enemy.Initialize会自动注册）
            var go = new GameObject("EnemyRegistry");
            go.AddComponent<EnemyRegistry>();
            _createdObjects.Add(go);

            // 验证Instance不为null
            Assert.IsNotNull(EnemyRegistry.Instance, "EnemyRegistry.Instance不应为null");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            Object.DestroyImmediate(_enemyData);
            ClearSingletonAndDestroy<EnemyRegistry>();
        }

        /// <summary>
        /// 清理Singleton的_instance字段并销毁场景中的旧对象。
        /// </summary>
        private void ClearSingletonAndDestroy<T>() where T : MonoBehaviour
        {
            // 销毁场景中所有该类型的对象
            var existing = Object.FindObjectsOfType<T>();
            foreach (var obj in existing)
            {
                Object.DestroyImmediate(obj.gameObject);
            }

            // 清理静态实例
            var field = typeof(Singleton<T>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null) field.SetValue(null, null);
        }

        /// <summary>
        /// 创建一个位于指定位置的活跃敌人。
        /// </summary>
        private Enemy CreateEnemyAt(Vector3 position)
        {
            var go = new GameObject("Enemy");
            go.transform.position = position;
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(_enemyData, new Transform[0]);
            _createdObjects.Add(go);

            // 验证敌人已激活并注册
            Assert.IsTrue(enemy.IsActive, "Enemy.Initialize后IsActive应为true");
            return enemy;
        }

        [Test]
        public void Register_ShouldAddEnemyToRegistry()
        {
            // Arrange
            var enemy = CreateEnemyAt(Vector3.zero);

            // Act - Enemy.Initialize已自动注册
            int count = EnemyRegistry.Instance.GetAllActiveEnemies().Count;

            // Assert
            Assert.AreEqual(1, count, "注册后应有1个活跃敌人");
        }

        [Test]
        public void Unregister_ShouldRemoveEnemyFromRegistry()
        {
            // Arrange
            var enemy = CreateEnemyAt(Vector3.zero);

            // Act
            EnemyRegistry.Instance.Unregister(enemy);

            // Assert
            Assert.AreEqual(0, EnemyRegistry.Instance.GetAllActiveEnemies().Count,
                "注销后应有0个活跃敌人");
        }

        [Test]
        public void GetEnemiesInRange_WithEnemyInRange_ShouldReturnEnemy()
        {
            // Arrange
            var enemy = CreateEnemyAt(Vector3.zero);
            var results = new List<Enemy>();

            // Act
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);

            // Assert
            Assert.AreEqual(1, results.Count, "范围内应有1个敌人");
            Assert.AreSame(enemy, results[0], "返回的敌人应是注册的敌人");
        }

        [Test]
        public void GetEnemiesInRange_WithEnemyOutOfRange_ShouldReturnEmpty()
        {
            // Arrange
            var enemy = CreateEnemyAt(new Vector3(10f, 0f, 0f));
            var results = new List<Enemy>();

            // Act
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);

            // Assert
            Assert.AreEqual(0, results.Count, "范围外不应返回敌人");
        }

        [Test]
        public void GetEnemiesInRange_WithMultipleEnemies_ShouldReturnOnlyInRange()
        {
            // Arrange
            var enemy1 = CreateEnemyAt(Vector3.zero);           // 范围内
            var enemy2 = CreateEnemyAt(new Vector3(1f, 0f, 0f)); // 范围内
            var enemy3 = CreateEnemyAt(new Vector3(5f, 0f, 0f)); // 范围外
            var enemy4 = CreateEnemyAt(new Vector3(0f, 1.5f, 0f)); // 范围内
            var results = new List<Enemy>();

            // Act
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);

            // Assert
            Assert.AreEqual(3, results.Count, "范围内应有3个敌人（enemy1, enemy2, enemy4）");
            Assert.Contains(enemy1, results, "应包含enemy1");
            Assert.Contains(enemy2, results, "应包含enemy2");
            Assert.Contains(enemy4, results, "应包含enemy4");
            Assert.IsFalse(results.Contains(enemy3), "不应包含范围外的enemy3");
        }

        [Test]
        public void GetEnemiesInRange_OnBoundary_ShouldIncludeEnemy()
        {
            // Arrange - 敌人正好在范围边界上
            var enemy = CreateEnemyAt(new Vector3(2f, 0f, 0f));
            var results = new List<Enemy>();

            // Act
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);

            // Assert
            Assert.AreEqual(1, results.Count, "边界上的敌人应被包含（<= 半径）");
        }

        [Test]
        public void GetEnemiesInRange_AfterEnemyMoves_ShouldUpdatePosition()
        {
            // Arrange
            var enemy = CreateEnemyAt(Vector3.zero);
            var results = new List<Enemy>();

            // 初始在范围内
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);
            Assert.AreEqual(1, results.Count, "初始应在范围内");

            // Act - 移动敌人到范围外并更新网格
            enemy.transform.position = new Vector3(10f, 0f, 0f);
            EnemyRegistry.Instance.UpdateGridPosition(enemy);

            // Assert
            results.Clear();
            EnemyRegistry.Instance.GetEnemiesInRange(Vector3.zero, 2f, results);
            Assert.AreEqual(0, results.Count, "移动后应不在范围内");
        }

        [Test]
        public void Clear_ShouldRemoveAllEnemies()
        {
            // Arrange
            CreateEnemyAt(Vector3.zero);
            CreateEnemyAt(new Vector3(1f, 0f, 0f));
            CreateEnemyAt(new Vector3(2f, 0f, 0f));

            // Act
            EnemyRegistry.Instance.Clear();

            // Assert
            Assert.AreEqual(0, EnemyRegistry.Instance.GetAllActiveEnemies().Count, "清空后应有0个敌人");
        }

        [Test]
        public void Register_DuplicateEnemy_ShouldNotAddTwice()
        {
            // Arrange
            var enemy = CreateEnemyAt(Vector3.zero);

            // Act - 手动重复注册
            EnemyRegistry.Instance.Register(enemy);

            // Assert
            Assert.AreEqual(1, EnemyRegistry.Instance.GetAllActiveEnemies().Count,
                "重复注册同一敌人不应添加两次");
        }
    }
}
