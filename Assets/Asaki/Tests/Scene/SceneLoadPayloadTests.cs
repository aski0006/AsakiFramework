// File: Assets/Asaki/Tests/Scene/SceneLoadPayloadTests.cs
// SceneLoadPayload 单元测试

using Asaki.Core.Scene;
using Asaki.Core.Scene.SceneManagement;
using Asaki.Unity.Services.Scene.SceneManagement;
using NUnit.Framework;

namespace Asaki.Tests.Scene
{
    /// <summary>
    /// SceneLoadPayload 的单元测试
    /// </summary>
    public class SceneLoadPayloadTests
    {
        [Test]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var payload = new SceneLoadPayload();

            // Assert
            Assert.IsNull(payload.TargetSceneName);
            Assert.AreEqual("LoadingScene", payload.LoadingSceneName);
            Assert.AreEqual(AsakiLoadSceneMode.Single, payload.LoadMode);
            Assert.AreEqual(AsakiSceneActivation.Immediate, payload.Activation);
            Assert.IsNull(payload.CustomData);
            Assert.IsTrue(payload.UsePreload);
            Assert.AreEqual(30, payload.TimeoutSeconds);
        }

        [Test]
        public void Create_WithTargetSceneName_SetsTargetAndDefaultLoadingScene()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.Create("GameScene");

            // Assert
            Assert.AreEqual("GameScene", payload.TargetSceneName);
            Assert.AreEqual("LoadingScene", payload.LoadingSceneName);
            Assert.IsTrue(payload.UsePreload);
        }

        [Test]
        public void Create_WithCustomLoadingScene_SetsBothScenes()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.Create("GameScene", "CustomLoading");

            // Assert
            Assert.AreEqual("GameScene", payload.TargetSceneName);
            Assert.AreEqual("CustomLoading", payload.LoadingSceneName);
            Assert.IsTrue(payload.UsePreload);
        }

        [Test]
        public void CreateWithoutPreload_SetsUsePreloadToFalse()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.CreateWithoutPreload("GameScene");

            // Assert
            Assert.AreEqual("GameScene", payload.TargetSceneName);
            Assert.IsFalse(payload.UsePreload);
            Assert.AreEqual("LoadingScene", payload.LoadingSceneName);
        }

        [Test]
        public void PropertySetters_CanModifyValues()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act
            payload.TargetSceneName = "NewTarget";
            payload.LoadingSceneName = "NewLoading";
            payload.LoadMode = AsakiLoadSceneMode.Additive;
            payload.Activation = AsakiSceneActivation.ManualConfirm;
            payload.CustomData = new { Level = 5 };
            payload.UsePreload = false;
            payload.TimeoutSeconds = 60;

            // Assert
            Assert.AreEqual("NewTarget", payload.TargetSceneName);
            Assert.AreEqual("NewLoading", payload.LoadingSceneName);
            Assert.AreEqual(AsakiLoadSceneMode.Additive, payload.LoadMode);
            Assert.AreEqual(AsakiSceneActivation.ManualConfirm, payload.Activation);
            Assert.IsNotNull(payload.CustomData);
            Assert.IsFalse(payload.UsePreload);
            Assert.AreEqual(60, payload.TimeoutSeconds);
        }

        [Test]
        public void Create_WithNullTargetSceneName_AllowsNull()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.Create(null);

            // Assert
            Assert.IsNull(payload.TargetSceneName);
        }

        [Test]
        public void Create_WithEmptySceneNames_AllowsEmptyStrings()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.Create("", "");

            // Assert
            Assert.AreEqual("", payload.TargetSceneName);
            Assert.AreEqual("", payload.LoadingSceneName);
        }

        [Test]
        public void CustomData_CanStoreComplexObject()
        {
            // Arrange
            var payload = new SceneLoadPayload();
            var customData = new TestCustomData
            {
                Level = 10,
                Difficulty = "Hard",
                IsNewGame = true
            };

            // Act
            payload.CustomData = customData;

            // Assert
            Assert.IsInstanceOf<TestCustomData>(payload.CustomData);
            var retrieved = (TestCustomData)payload.CustomData;
            Assert.AreEqual(10, retrieved.Level);
            Assert.AreEqual("Hard", retrieved.Difficulty);
            Assert.IsTrue(retrieved.IsNewGame);
        }

        [Test]
        public void CustomData_CanStorePrimitiveTypes()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act & Assert - Test various primitive types
            payload.CustomData = 42;
            Assert.AreEqual(42, payload.CustomData);

            payload.CustomData = "test string";
            Assert.AreEqual("test string", payload.CustomData);

            payload.CustomData = 3.14f;
            Assert.AreEqual(3.14f, payload.CustomData);

            payload.CustomData = true;
            Assert.AreEqual(true, payload.CustomData);
        }

        [Test]
        public void TimeoutSeconds_CanBeSetToZero()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act
            payload.TimeoutSeconds = 0;

            // Assert
            Assert.AreEqual(0, payload.TimeoutSeconds);
        }

        [Test]
        public void TimeoutSeconds_CanBeSetToNegativeValue()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act
            payload.TimeoutSeconds = -1;

            // Assert
            Assert.AreEqual(-1, payload.TimeoutSeconds);
        }

        [Test]
        public void TimeoutSeconds_CanBeSetToLargeValue()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act
            payload.TimeoutSeconds = int.MaxValue;

            // Assert
            Assert.AreEqual(int.MaxValue, payload.TimeoutSeconds);
        }

        [Test]
        public void MultiplePayloads_AreIndependent()
        {
            // Arrange
            var payload1 = SceneLoadPayload.Create("Scene1");
            var payload2 = SceneLoadPayload.Create("Scene2", "Loading2");

            // Act
            payload1.TargetSceneName = "Modified1";

            // Assert
            Assert.AreEqual("Modified1", payload1.TargetSceneName);
            Assert.AreEqual("Scene2", payload2.TargetSceneName);
            Assert.AreEqual("Loading2", payload2.LoadingSceneName);
        }

        [Test]
        public void Payload_WithSpecialCharactersInSceneName_PreservesCharacters()
        {
            // Arrange
            var specialTarget = "Scene_123-Test.Scene/Path";
            var specialLoading = "Loading_Scene-Test";

            // Act
            var payload = SceneLoadPayload.Create(specialTarget, specialLoading);

            // Assert
            Assert.AreEqual(specialTarget, payload.TargetSceneName);
            Assert.AreEqual(specialLoading, payload.LoadingSceneName);
        }

        [Test]
        public void Payload_CreateMultipleTimes_CreatesNewInstances()
        {
            // Arrange & Act
            var payload1 = SceneLoadPayload.Create("Scene1");
            var payload2 = SceneLoadPayload.Create("Scene1");

            // Modify one
            payload1.TargetSceneName = "Modified";

            // Assert - They should be independent
            Assert.AreEqual("Modified", payload1.TargetSceneName);
            Assert.AreEqual("Scene1", payload2.TargetSceneName);
        }

        [Test]
        public void Payload_AllLoadModes_CanBeSet()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act & Assert
            payload.LoadMode = AsakiLoadSceneMode.Single;
            Assert.AreEqual(AsakiLoadSceneMode.Single, payload.LoadMode);

            payload.LoadMode = AsakiLoadSceneMode.Additive;
            Assert.AreEqual(AsakiLoadSceneMode.Additive, payload.LoadMode);
        }

        [Test]
        public void Payload_AllActivationModes_CanBeSet()
        {
            // Arrange
            var payload = new SceneLoadPayload();

            // Act & Assert
            payload.Activation = AsakiSceneActivation.Immediate;
            Assert.AreEqual(AsakiSceneActivation.Immediate, payload.Activation);

            payload.Activation = AsakiSceneActivation.ManualConfirm;
            Assert.AreEqual(AsakiSceneActivation.ManualConfirm, payload.Activation);
        }

        [Test]
        public void Payload_CreateWithoutPreload_DoesNotAffectOtherProperties()
        {
            // Arrange & Act
            var payload = SceneLoadPayload.CreateWithoutPreload("TargetScene");

            // Assert - Verify other properties have default values
            Assert.AreEqual("TargetScene", payload.TargetSceneName);
            Assert.AreEqual("LoadingScene", payload.LoadingSceneName);
            Assert.IsFalse(payload.UsePreload);
            Assert.AreEqual(AsakiLoadSceneMode.Single, payload.LoadMode);
            Assert.AreEqual(AsakiSceneActivation.Immediate, payload.Activation);
            Assert.AreEqual(30, payload.TimeoutSeconds);
        }

        [Test]
        public void Payload_CustomData_CanBeNull()
        {
            // Arrange
            var payload = new SceneLoadPayload { CustomData = new object() };

            // Act
            payload.CustomData = null;

            // Assert
            Assert.IsNull(payload.CustomData);
        }

        private class TestCustomData
        {
            public int Level { get; set; }
            public string Difficulty { get; set; }
            public bool IsNewGame { get; set; }
        }
    }
}
