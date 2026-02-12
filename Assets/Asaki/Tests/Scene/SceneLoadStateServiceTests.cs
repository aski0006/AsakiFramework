// File: Assets/Asaki/Tests/Scene/SceneLoadStateServiceTests.cs
// SceneLoadStateService 单元测试

using Asaki.Core.Scene;
using Asaki.Core.Scene.SceneManagement;
using Asaki.Unity.Services.Scene.SceneManagement;
using NUnit.Framework;

namespace Asaki.Tests.Scene
{
    /// <summary>
    /// SceneLoadStateService 的单元测试
    /// </summary>
    public class SceneLoadStateServiceTests
    {
        [SetUp]
        public void Setup()
        {
            // 确保每个测试前状态都是清空的
            SceneLoadStateService.ClearPayload();
        }

        [TearDown]
        public void TearDown()
        {
            // 确保每个测试后状态都是清空的
            SceneLoadStateService.ClearPayload();
        }

        [Test]
        public void HasPayload_WhenNoPayloadSet_ReturnsFalse()
        {
            // Arrange - Setup clears payload

            // Act & Assert
            Assert.IsFalse(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void HasPayload_WhenPayloadSet_ReturnsTrue()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");
            SceneLoadStateService.SetPayload(payload);

            // Act & Assert
            Assert.IsTrue(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void SetPayload_WithValidPayload_SetsHasPayloadToTrue()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");

            // Act
            SceneLoadStateService.SetPayload(payload);

            // Assert
            Assert.IsTrue(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void SetPayload_WithNullPayload_SetsHasPayloadToFalse()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            // Act
            SceneLoadStateService.SetPayload(null);

            // Assert
            Assert.IsFalse(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void GetPayload_WhenHasPayload_ReturnsPayloadAndClearsIt()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene", "LoadingScene");
            SceneLoadStateService.SetPayload(payload);

            // Act
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("TestScene", retrieved.TargetSceneName);
            Assert.AreEqual("LoadingScene", retrieved.LoadingSceneName);
            Assert.IsFalse(SceneLoadStateService.HasPayload); // Should be cleared after get
        }

        [Test]
        public void GetPayload_WhenNoPayload_ReturnsNull()
        {
            // Arrange - no payload set

            // Act
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void GetPayload_CalledTwice_ReturnsNullOnSecondCall()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");
            SceneLoadStateService.SetPayload(payload);

            // Act
            var first = SceneLoadStateService.GetPayload();
            var second = SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsNotNull(first);
            Assert.IsNull(second);
        }

        [Test]
        public void ClearPayload_WhenHasPayload_ClearsPayload()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            // Act
            SceneLoadStateService.ClearPayload();

            // Assert
            Assert.IsFalse(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void ClearPayload_WhenNoPayload_DoesNotThrow()
        {
            // Arrange - no payload

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => SceneLoadStateService.ClearPayload());
            Assert.IsFalse(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void PeekPayload_WhenHasPayload_ReturnsPayloadWithoutClearing()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");
            SceneLoadStateService.SetPayload(payload);

            // Act
            var firstPeek = SceneLoadStateService.PeekPayload();
            var secondPeek = SceneLoadStateService.PeekPayload();

            // Assert
            Assert.IsNotNull(firstPeek);
            Assert.IsNotNull(secondPeek);
            Assert.AreEqual("TestScene", firstPeek.TargetSceneName);
            Assert.AreEqual("TestScene", secondPeek.TargetSceneName);
            Assert.IsTrue(SceneLoadStateService.HasPayload); // Still has payload
        }

        [Test]
        public void PeekPayload_WhenNoPayload_ReturnsNull()
        {
            // Arrange - no payload

            // Act
            var peeked = SceneLoadStateService.PeekPayload();

            // Assert
            Assert.IsNull(peeked);
        }

        [Test]
        public void PayloadPersistence_AcrossMultipleOperations_MaintainsCorrectState()
        {
            // Arrange & Act - Complex sequence of operations
            Assert.IsFalse(SceneLoadStateService.HasPayload);

            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("Scene1"));
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            var peek1 = SceneLoadStateService.PeekPayload();
            Assert.AreEqual("Scene1", peek1.TargetSceneName);
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            var get1 = SceneLoadStateService.GetPayload();
            Assert.AreEqual("Scene1", get1.TargetSceneName);
            Assert.IsFalse(SceneLoadStateService.HasPayload);

            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("Scene2"));
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            SceneLoadStateService.ClearPayload();
            Assert.IsFalse(SceneLoadStateService.HasPayload);

            // Assert - Final state
            Assert.IsNull(SceneLoadStateService.GetPayload());
            Assert.IsNull(SceneLoadStateService.PeekPayload());
        }

        [Test]
        public void SetPayload_MultipleTimes_OverwritesPreviousPayload()
        {
            // Arrange
            var payload1 = SceneLoadPayload.Create("Scene1");
            var payload2 = SceneLoadPayload.Create("Scene2");

            // Act
            SceneLoadStateService.SetPayload(payload1);
            SceneLoadStateService.SetPayload(payload2);

            // Assert
            var retrieved = SceneLoadStateService.GetPayload();
            Assert.AreEqual("Scene2", retrieved.TargetSceneName);
        }

        [Test]
        public void Payload_WithCustomData_PreservesCustomData()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");
            payload.CustomData = new TestCustomData { Level = 5, Difficulty = "Hard" };

            // Act
            SceneLoadStateService.SetPayload(payload);
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsNotNull(retrieved.CustomData);
            var data = (TestCustomData)retrieved.CustomData;
            Assert.AreEqual(5, data.Level);
            Assert.AreEqual("Hard", data.Difficulty);
        }

        [Test]
        public void Payload_WithAllPropertiesSet_PreservesAllProperties()
        {
            // Arrange
            var payload = new SceneLoadPayload
            {
                TargetSceneName = "Target",
                LoadingSceneName = "Loading",
                LoadMode = AsakiLoadSceneMode.Additive,
                Activation = AsakiSceneActivation.ManualConfirm,
                CustomData = "test data",
                UsePreload = false,
                TimeoutSeconds = 60,
            };

            // Act
            SceneLoadStateService.SetPayload(payload);
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.AreEqual("Target", retrieved.TargetSceneName);
            Assert.AreEqual("Loading", retrieved.LoadingSceneName);
            Assert.AreEqual(AsakiLoadSceneMode.Additive, retrieved.LoadMode);
            Assert.AreEqual(AsakiSceneActivation.ManualConfirm, retrieved.Activation);
            Assert.AreEqual("test data", retrieved.CustomData);
            Assert.IsFalse(retrieved.UsePreload);
            Assert.AreEqual(60, retrieved.TimeoutSeconds);
        }

        [Test]
        public void GetPayload_AfterClear_ReturnsNull()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));
            SceneLoadStateService.ClearPayload();

            // Act
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void PeekPayload_AfterClear_ReturnsNull()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));
            SceneLoadStateService.ClearPayload();

            // Act
            var peeked = SceneLoadStateService.PeekPayload();

            // Assert
            Assert.IsNull(peeked);
        }

        [Test]
        public void SetPayload_WithComplexSceneNames_PreservesNames()
        {
            // Arrange
            var targetName = "Level_01-BossFight.Scene";
            var loadingName = "Custom_Loading-Scene_v2";
            var payload = SceneLoadPayload.Create(targetName, loadingName);

            // Act
            SceneLoadStateService.SetPayload(payload);
            var retrieved = SceneLoadStateService.GetPayload();

            // Assert
            Assert.AreEqual(targetName, retrieved.TargetSceneName);
            Assert.AreEqual(loadingName, retrieved.LoadingSceneName);
        }

        [Test]
        public void HasPayload_AfterGet_ReturnsFalse()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));
            Assert.IsTrue(SceneLoadStateService.HasPayload);

            // Act
            SceneLoadStateService.GetPayload();

            // Assert
            Assert.IsFalse(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void HasPayload_AfterPeek_ReturnsTrue()
        {
            // Arrange
            SceneLoadStateService.SetPayload(SceneLoadPayload.Create("TestScene"));

            // Act
            SceneLoadStateService.PeekPayload();

            // Assert
            Assert.IsTrue(SceneLoadStateService.HasPayload);
        }

        [Test]
        public void MultipleSetAndGetOperations_WorkCorrectly()
        {
            // Arrange & Act
            for (int i = 0; i < 5; i++)
            {
                var payload = SceneLoadPayload.Create($"Scene{i}");
                SceneLoadStateService.SetPayload(payload);
                Assert.IsTrue(SceneLoadStateService.HasPayload);

                var retrieved = SceneLoadStateService.GetPayload();
                Assert.AreEqual($"Scene{i}", retrieved.TargetSceneName);
                Assert.IsFalse(SceneLoadStateService.HasPayload);
            }
        }

        [Test]
        public void Payload_ReferenceEquality_RetrievedIsSameInstance()
        {
            // Arrange
            var payload = SceneLoadPayload.Create("TestScene");
            SceneLoadStateService.SetPayload(payload);

            // Act
            var retrieved = SceneLoadStateService.PeekPayload();

            // Assert - Should be the same reference
            Assert.AreSame(payload, retrieved);
        }

        /// <summary>
        /// 测试用的自定义数据类
        /// </summary>
        private class TestCustomData
        {
            public int Level { get; set; }
            public string Difficulty { get; set; }
        }
    }
}
