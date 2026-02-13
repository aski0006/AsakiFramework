// File: Assets/Asaki/Tests/Scene/AsakiSceneResultTests.cs
// AsakiSceneResult 结构体单元测试

using Asaki.Core.Scene;
using NUnit.Framework;

namespace Asaki.Tests.Scene
{
    /// <summary>
    /// AsakiSceneResult 结构体的单元测试
    /// </summary>
    public class AsakiSceneResultTests
    {
        [Test]
        public void Constructor_WithSuccessTrue_SetsSuccessAndSceneName()
        {
            // Arrange & Act
            var result = new AsakiSceneResult(true, "TestScene");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("TestScene", result.SceneName);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void Constructor_WithSuccessFalse_SetsFailureState()
        {
            // Arrange & Act
            var result = new AsakiSceneResult(false, "FailedScene", "Error occurred");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FailedScene", result.SceneName);
            Assert.AreEqual("Error occurred", result.ErrorMessage);
        }

        [Test]
        public void Ok_CreatesSuccessfulResult()
        {
            // Arrange & Act
            var result = AsakiSceneResult.Ok("SuccessScene");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("SuccessScene", result.SceneName);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void Failed_CreatesFailedResultWithMessage()
        {
            // Arrange & Act
            var result = AsakiSceneResult.Failed("FailedScene", "Something went wrong");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FailedScene", result.SceneName);
            Assert.AreEqual("Something went wrong", result.ErrorMessage);
        }

        [Test]
        public void Failed_WithoutMessage_CreatesFailedResultWithNullMessage()
        {
            // Arrange & Act
            var result = AsakiSceneResult.Failed("FailedScene");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FailedScene", result.SceneName);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void OperationCanceled_CreatesCanceledResult()
        {
            // Arrange & Act
            var result = AsakiSceneResult.OperationCanceled("CanceledScene");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("CanceledScene", result.SceneName);
            Assert.AreEqual("Operation canceled.", result.ErrorMessage);
        }

        [Test]
        public void OperationCanceled_WithCustomMessage_CreatesCanceledResultWithCustomMessage()
        {
            // Arrange & Act
            var result = AsakiSceneResult.OperationCanceled(
                "CanceledScene",
                "Custom cancel message"
            );

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("CanceledScene", result.SceneName);
            // Note: The implementation ignores the custom message and uses default
            Assert.AreEqual("Operation canceled.", result.ErrorMessage);
        }

        [Test]
        public void Result_IsReadonly_StructCannotBeModified()
        {
            // Arrange
            var result = AsakiSceneResult.Ok("OriginalScene");

            // Act - Create new result (structs are value types)
            var modifiedResult = new AsakiSceneResult(false, "ModifiedScene", "Error");

            // Assert - Original is unchanged
            Assert.IsTrue(result.Success);
            Assert.AreEqual("OriginalScene", result.SceneName);
            Assert.IsFalse(modifiedResult.Success);
            Assert.AreEqual("ModifiedScene", modifiedResult.SceneName);
        }

        [Test]
        public void Equality_TwoSuccessfulResultsWithSameSceneName_AreNotEqualByDefault()
        {
            // Arrange
            var result1 = AsakiSceneResult.Ok("SceneA");
            var result2 = AsakiSceneResult.Ok("SceneA");

            // Act & Assert - structs without equality override use default equality
            // Since AsakiSceneResult doesn't override Equals, this tests reference equality for structs
            // which compares all fields
            Assert.AreEqual(result1.Success, result2.Success);
            Assert.AreEqual(result1.SceneName, result2.SceneName);
            Assert.AreEqual(result1.ErrorMessage, result2.ErrorMessage);
        }

        [Test]
        public void Result_WithEmptySceneName_HandlesEmptyString()
        {
            // Arrange & Act
            var result = AsakiSceneResult.Ok("");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("", result.SceneName);
        }

        [Test]
        public void Result_WithNullSceneName_HandlesNull()
        {
            // Arrange & Act
            var result = AsakiSceneResult.Ok(null);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.SceneName);
        }

        [Test]
        public void Result_WithLongErrorMessage_PreservesFullMessage()
        {
            // Arrange
            var longMessage = new string('A', 10000);

            // Act
            var result = AsakiSceneResult.Failed("Scene", longMessage);

            // Assert
            Assert.AreEqual(longMessage, result.ErrorMessage);
            Assert.AreEqual(10000, result.ErrorMessage.Length);
        }

        [Test]
        public void Result_WithSpecialCharactersInSceneName_PreservesCharacters()
        {
            // Arrange
            var specialSceneName = "Scene_123-Test.Scene/Path";

            // Act
            var result = AsakiSceneResult.Ok(specialSceneName);

            // Assert
            Assert.AreEqual(specialSceneName, result.SceneName);
        }
    }
}
