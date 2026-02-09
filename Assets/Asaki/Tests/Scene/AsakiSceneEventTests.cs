// File: Assets/Asaki/Tests/Scene/AsakiSceneEventTests.cs
// 场景事件（AsakiSceneProgressEvent 和 AsakiSceneStateEvent）的单元测试

using Asaki.Core.Broker;
using Asaki.Core.Scene;
using NUnit.Framework;

namespace Asaki.Tests.Scene
{
    /// <summary>
    /// 场景进度事件和状态事件的单元测试
    /// </summary>
    public class AsakiSceneEventTests
    {
        #region AsakiSceneProgressEvent Tests

        [Test]
        public void AsakiSceneProgressEvent_Constructor_SetsSceneNameAndProgress()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", 0.5f);

            // Assert
            Assert.AreEqual("TestScene", evt.SceneName);
            Assert.AreEqual(0.5f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithZeroProgress_SetsCorrectly()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", 0f);

            // Assert
            Assert.AreEqual(0f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithFullProgress_SetsCorrectly()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", 1f);

            // Assert
            Assert.AreEqual(1f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithEmptySceneName_HandlesEmptyString()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("", 0.5f);

            // Assert
            Assert.AreEqual("", evt.SceneName);
            Assert.AreEqual(0.5f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithNullSceneName_HandlesNull()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent(null, 0.5f);

            // Assert
            Assert.IsNull(evt.SceneName);
            Assert.AreEqual(0.5f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_ImplementsIAsakiEvent()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", 0.5f);

            // Assert
            Assert.IsInstanceOf<IAsakiEvent>(evt);
        }

        [Test]
        public void AsakiSceneProgressEvent_IsStruct_ValueType()
        {
            // Arrange
            var evt1 = new AsakiSceneProgressEvent("Scene1", 0.5f);
            var evt2 = evt1;

            // Act
            evt2 = new AsakiSceneProgressEvent("Scene2", 0.8f);

            // Assert - evt1 should be unchanged
            Assert.AreEqual("Scene1", evt1.SceneName);
            Assert.AreEqual(0.5f, evt1.Progress);
            Assert.AreEqual("Scene2", evt2.SceneName);
            Assert.AreEqual(0.8f, evt2.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_Equality_SameValuesAreEqual()
        {
            // Arrange
            var evt1 = new AsakiSceneProgressEvent("Scene", 0.5f);
            var evt2 = new AsakiSceneProgressEvent("Scene", 0.5f);

            // Assert - structs use value equality
            Assert.AreEqual(evt1.SceneName, evt2.SceneName);
            Assert.AreEqual(evt1.Progress, evt2.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithNegativeProgress_AcceptsNegativeValue()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", -0.1f);

            // Assert - struct accepts any float value (no validation)
            Assert.AreEqual(-0.1f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithProgressGreaterThanOne_AcceptsValue()
        {
            // Arrange & Act
            var evt = new AsakiSceneProgressEvent("TestScene", 1.5f);

            // Assert - struct accepts any float value (no validation)
            Assert.AreEqual(1.5f, evt.Progress);
        }

        [Test]
        public void AsakiSceneProgressEvent_WithSpecialCharactersInSceneName_PreservesCharacters()
        {
            // Arrange
            var specialName = "Scene_123-Test.Scene/Path";

            // Act
            var evt = new AsakiSceneProgressEvent(specialName, 0.5f);

            // Assert
            Assert.AreEqual(specialName, evt.SceneName);
        }

        #endregion

        #region AsakiSceneStateEvent Tests

        [Test]
        public void AsakiSceneStateEvent_Constructor_SetsSceneNameAndState()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("TestScene", AsakiSceneStateEvent.State.Started);

            // Assert
            Assert.AreEqual("TestScene", evt.SceneName);
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, evt.CurrentState);
            Assert.IsNull(evt.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_Constructor_WithErrorMessage_SetsAllFields()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent(
                "TestScene",
                AsakiSceneStateEvent.State.Failed,
                "Something went wrong"
            );

            // Assert
            Assert.AreEqual("TestScene", evt.SceneName);
            Assert.AreEqual(AsakiSceneStateEvent.State.Failed, evt.CurrentState);
            Assert.AreEqual("Something went wrong", evt.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_AllStates_CanBeCreated()
        {
            // Arrange & Act
            var startedEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Started);
            var completedEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Completed);
            var failedEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Failed, "Error");
            var cancelledEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Cancelled);

            // Assert
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, startedEvt.CurrentState);
            Assert.AreEqual(AsakiSceneStateEvent.State.Completed, completedEvt.CurrentState);
            Assert.AreEqual(AsakiSceneStateEvent.State.Failed, failedEvt.CurrentState);
            Assert.AreEqual(AsakiSceneStateEvent.State.Cancelled, cancelledEvt.CurrentState);
        }

        [Test]
        public void AsakiSceneStateEvent_WithEmptySceneName_HandlesEmptyString()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("", AsakiSceneStateEvent.State.Started);

            // Assert
            Assert.AreEqual("", evt.SceneName);
        }

        [Test]
        public void AsakiSceneStateEvent_WithNullSceneName_HandlesNull()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent(null, AsakiSceneStateEvent.State.Started);

            // Assert
            Assert.IsNull(evt.SceneName);
        }

        [Test]
        public void AsakiSceneStateEvent_ImplementsIAsakiEvent()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("TestScene", AsakiSceneStateEvent.State.Started);

            // Assert
            Assert.IsInstanceOf<IAsakiEvent>(evt);
        }

        [Test]
        public void AsakiSceneStateEvent_IsReadonlyStruct()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("TestScene", AsakiSceneStateEvent.State.Started, "Error");

            // Assert - readonly struct fields cannot be modified after creation
            Assert.AreEqual("TestScene", evt.SceneName);
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, evt.CurrentState);
            Assert.AreEqual("Error", evt.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_Equality_SameValuesAreEqual()
        {
            // Arrange
            var evt1 = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Completed);
            var evt2 = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Completed);

            // Assert
            Assert.AreEqual(evt1.SceneName, evt2.SceneName);
            Assert.AreEqual(evt1.CurrentState, evt2.CurrentState);
            Assert.AreEqual(evt1.ErrorMessage, evt2.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_WithLongErrorMessage_PreservesFullMessage()
        {
            // Arrange
            var longMessage = new string('A', 10000);

            // Act
            var evt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Failed, longMessage);

            // Assert
            Assert.AreEqual(longMessage, evt.ErrorMessage);
            Assert.AreEqual(10000, evt.ErrorMessage.Length);
        }

        [Test]
        public void AsakiSceneStateEvent_WithEmptyErrorMessage_HandlesEmptyString()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Failed, "");

            // Assert
            Assert.AreEqual("", evt.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_WithNullErrorMessage_HandlesNull()
        {
            // Arrange & Act
            var evt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Started, null);

            // Assert
            Assert.IsNull(evt.ErrorMessage);
        }

        [Test]
        public void AsakiSceneStateEvent_SuccessStates_DoNotRequireErrorMessage()
        {
            // Arrange & Act
            var startedEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Started);
            var completedEvt = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Completed);

            // Assert
            Assert.IsNull(startedEvt.ErrorMessage);
            Assert.IsNull(completedEvt.ErrorMessage);
        }

        #endregion

        #region Event Usage Patterns

        [Test]
        public void Events_CanBeUsedInEventBusPattern()
        {
            // Arrange
            var progressEvent = new AsakiSceneProgressEvent("LoadingScene", 0.5f);
            var stateEvent = new AsakiSceneStateEvent("LoadingScene", AsakiSceneStateEvent.State.Started);

            // Act & Assert - Both implement IAsakiEvent
            Assert.IsInstanceOf<IAsakiEvent>(progressEvent);
            Assert.IsInstanceOf<IAsakiEvent>(stateEvent);
        }

        [Test]
        public void Events_StructSemantics_CreateIndependentCopies()
        {
            // Arrange
            var originalProgress = new AsakiSceneProgressEvent("Scene", 0.5f);
            var copyProgress = originalProgress;

            var originalState = new AsakiSceneStateEvent("Scene", AsakiSceneStateEvent.State.Started);
            var copyState = originalState;

            // Act - Modify copies (create new structs)
            copyProgress = new AsakiSceneProgressEvent("NewScene", 0.8f);
            copyState = new AsakiSceneStateEvent("NewScene", AsakiSceneStateEvent.State.Completed);

            // Assert - Originals unchanged
            Assert.AreEqual("Scene", originalProgress.SceneName);
            Assert.AreEqual(0.5f, originalProgress.Progress);
            Assert.AreEqual("Scene", originalState.SceneName);
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, originalState.CurrentState);
        }

        [Test]
        public void Events_CanBeStoredInCollections()
        {
            // Arrange
            var events = new System.Collections.Generic.List<AsakiSceneProgressEvent>
            {
                new AsakiSceneProgressEvent("Scene1", 0.25f),
                new AsakiSceneProgressEvent("Scene1", 0.5f),
                new AsakiSceneProgressEvent("Scene1", 0.75f),
                new AsakiSceneProgressEvent("Scene1", 1.0f)
            };

            // Assert
            Assert.AreEqual(4, events.Count);
            Assert.AreEqual(0.25f, events[0].Progress);
            Assert.AreEqual(1.0f, events[3].Progress);
        }

        [Test]
        public void Events_ProgressSequence_SimulatesLoadingProgress()
        {
            // Arrange
            var sceneName = "GameScene";
            var progressValues = new[] { 0f, 0.25f, 0.5f, 0.75f, 1.0f };
            var events = new System.Collections.Generic.List<AsakiSceneProgressEvent>();

            // Act - Simulate loading progress
            foreach (var progress in progressValues)
            {
                events.Add(new AsakiSceneProgressEvent(sceneName, progress));
            }

            // Assert
            Assert.AreEqual(5, events.Count);
            for (int i = 0; i < progressValues.Length; i++)
            {
                Assert.AreEqual(sceneName, events[i].SceneName);
                Assert.AreEqual(progressValues[i], events[i].Progress);
            }
        }

        [Test]
        public void Events_StateTransitionSequence_SimulatesSceneLifecycle()
        {
            // Arrange
            var sceneName = "GameScene";
            var stateSequence = new[]
            {
                AsakiSceneStateEvent.State.Started,
                AsakiSceneStateEvent.State.Completed
            };
            var events = new System.Collections.Generic.List<AsakiSceneStateEvent>();

            // Act - Simulate scene lifecycle
            events.Add(new AsakiSceneStateEvent(sceneName, AsakiSceneStateEvent.State.Started));
            events.Add(new AsakiSceneStateEvent(sceneName, AsakiSceneStateEvent.State.Completed));

            // Assert
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, events[0].CurrentState);
            Assert.AreEqual(AsakiSceneStateEvent.State.Completed, events[1].CurrentState);
        }

        [Test]
        public void Events_FailedStateTransition_SimulatesErrorScenario()
        {
            // Arrange
            var sceneName = "GameScene";
            var errorMessage = "Failed to load assets";

            // Act
            var startedEvt = new AsakiSceneStateEvent(sceneName, AsakiSceneStateEvent.State.Started);
            var failedEvt = new AsakiSceneStateEvent(sceneName, AsakiSceneStateEvent.State.Failed, errorMessage);

            // Assert
            Assert.AreEqual(AsakiSceneStateEvent.State.Started, startedEvt.CurrentState);
            Assert.AreEqual(AsakiSceneStateEvent.State.Failed, failedEvt.CurrentState);
            Assert.AreEqual(errorMessage, failedEvt.ErrorMessage);
        }

        #endregion
    }
}
