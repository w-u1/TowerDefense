using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.Tests.EditMode
{
    /// <summary>
    /// 经济系统测试：验证金币增加、扣除、格式化等逻辑。
    /// </summary>
    public class EconomySystemTest
    {
        private EconomySystem _economySystem;

        [SetUp]
        public void SetUp()
        {
            // 清理可能残留的Singleton实例并销毁旧对象
            ClearSingletonAndDestroy<GameManager>();

            // 创建GameManager实例（Singleton会自动初始化金币）
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();

            // EditMode下OnSingletonAwake可能未执行，手动确保金币初始化
            if (GameManager.Instance.CurrentGold <= 0)
            {
                GameManager.Instance.AddGold(300);
            }

            // 创建EconomySystem实例
            var ecoGo = new GameObject("EconomySystem");
            _economySystem = ecoGo.AddComponent<EconomySystem>();

            // 验证Instance不为null
            Assert.IsNotNull(GameManager.Instance, "GameManager.Instance不应为null");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_economySystem.gameObject);
            ClearSingletonAndDestroy<GameManager>();
        }

        /// <summary>
        /// 清理Singleton的_instance字段并销毁场景中的旧对象。
        /// </summary>
        private void ClearSingletonAndDestroy<T>() where T : MonoBehaviour
        {
            var existing = Object.FindObjectsOfType<T>();
            foreach (var obj in existing)
            {
                Object.DestroyImmediate(obj.gameObject);
            }
            var field = typeof(Singleton<T>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null) field.SetValue(null, null);
        }

        [Test]
        public void AddGold_ShouldIncreaseCurrentGold()
        {
            // Arrange
            int initialGold = GameManager.Instance.CurrentGold;
            int addAmount = 50;

            // Act
            GameManager.Instance.AddGold(addAmount);

            // Assert
            Assert.AreEqual(initialGold + addAmount, GameManager.Instance.CurrentGold,
                $"增加{addAmount}金币后，当前金币应为{initialGold + addAmount}");
        }

        [Test]
        public void TrySpendGold_WithEnoughGold_ShouldDecreaseGoldAndReturnTrue()
        {
            // Arrange
            int initialGold = GameManager.Instance.CurrentGold;
            int spendAmount = 50;
            Assert.IsTrue(initialGold >= spendAmount, "初始金币应足够花费");

            // Act
            bool result = GameManager.Instance.TrySpendGold(spendAmount);

            // Assert
            Assert.IsTrue(result, "金币足够时应返回true");
            Assert.AreEqual(initialGold - spendAmount, GameManager.Instance.CurrentGold,
                $"花费{spendAmount}金币后，当前金币应为{initialGold - spendAmount}");
        }

        [Test]
        public void TrySpendGold_WithInsufficientGold_ShouldReturnFalseAndNotChangeGold()
        {
            // Arrange
            int initialGold = GameManager.Instance.CurrentGold;
            int spendAmount = initialGold + 100; // 超过当前金币

            // Act
            bool result = GameManager.Instance.TrySpendGold(spendAmount);

            // Assert
            Assert.IsFalse(result, "金币不足时应返回false");
            Assert.AreEqual(initialGold, GameManager.Instance.CurrentGold,
                "金币不足时不应扣除金币");
        }

        [Test]
        public void TrySpendGold_WithZeroOrNegativeAmount_ShouldReturnFalse()
        {
            // Arrange
            int initialGold = GameManager.Instance.CurrentGold;

            // Act & Assert
            Assert.IsFalse(GameManager.Instance.TrySpendGold(0), "花费0应返回false");
            Assert.IsFalse(GameManager.Instance.TrySpendGold(-10), "花费负数应返回false");
            Assert.AreEqual(initialGold, GameManager.Instance.CurrentGold, "金币不应变化");
        }

        [Test]
        public void CanAfford_ShouldReturnCorrectResult()
        {
            // Arrange
            int currentGold = GameManager.Instance.CurrentGold;

            // Act & Assert
            Assert.IsTrue(_economySystem.CanAfford(currentGold), "花费等于当前金币时应能负担");
            Assert.IsTrue(_economySystem.CanAfford(currentGold - 1), "花费小于当前金币时应能负担");
            Assert.IsFalse(_economySystem.CanAfford(currentGold + 1), "花费大于当前金币时应不能负担");
        }

        [Test]
        public void FormatGold_ShouldFormatLargeNumbers()
        {
            // Act & Assert
            Assert.AreEqual("100", _economySystem.FormatGold(100), "100应直接显示");
            Assert.AreEqual("10.0K", _economySystem.FormatGold(10000), "10000应显示为10.0K");
            Assert.AreEqual("1.0M", _economySystem.FormatGold(1000000), "1000000应显示为1.0M");
            Assert.AreEqual("9999", _economySystem.FormatGold(9999), "9999应直接显示");
        }

        [Test]
        public void AddGold_MultipleTimes_ShouldAccumulate()
        {
            // Arrange
            int initialGold = GameManager.Instance.CurrentGold;

            // Act
            GameManager.Instance.AddGold(30);
            GameManager.Instance.AddGold(20);
            GameManager.Instance.AddGold(50);

            // Assert
            Assert.AreEqual(initialGold + 100, GameManager.Instance.CurrentGold,
                "多次增加金币应累加");
        }
    }
}
