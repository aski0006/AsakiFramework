// File: Assets/Asaki/Tests/Scene/AsakiSceneEnumsTests.cs
// 场景管理相关枚举的单元测试

using Asaki.Core.Scene;
using NUnit.Framework;

namespace Asaki.Tests.Scene
{
    /// <summary>
    /// 场景加载模式和激活方式的枚举测试
    /// </summary>
    public class AsakiSceneEnumsTests
    {
        #region AsakiLoadSceneMode Tests

        [Test]
        public void AsakiLoadSceneMode_Single_HasValueZero()
        {
            // Assert
            Assert.AreEqual(0, (int)AsakiLoadSceneMode.Single);
        }

        [Test]
        public void AsakiLoadSceneMode_Additive_HasValueOne()
        {
            // Assert
            Assert.AreEqual(1, (int)AsakiLoadSceneMode.Additive);
        }

        [Test]
        public void AsakiLoadSceneMode_Values_AreSequential()
        {
            // Assert
            Assert.AreEqual(0, (int)AsakiLoadSceneMode.Single);
            Assert.AreEqual(1, (int)AsakiLoadSceneMode.Additive);
        }

        [Test]
        public void AsakiLoadSceneMode_Single_IsDefaultValue()
        {
            // Arrange & Act
            AsakiLoadSceneMode mode = default;

            // Assert
            Assert.AreEqual(AsakiLoadSceneMode.Single, mode);
        }

        [Test]
        public void AsakiLoadSceneMode_CanBeCastToInt()
        {
            // Arrange & Act
            int singleValue = (int)AsakiLoadSceneMode.Single;
            int additiveValue = (int)AsakiLoadSceneMode.Additive;

            // Assert
            Assert.AreEqual(0, singleValue);
            Assert.AreEqual(1, additiveValue);
        }

        [Test]
        public void AsakiLoadSceneMode_CanBeParsedFromInt()
        {
            // Arrange & Act
            var singleMode = (AsakiLoadSceneMode)0;
            var additiveMode = (AsakiLoadSceneMode)1;

            // Assert
            Assert.AreEqual(AsakiLoadSceneMode.Single, singleMode);
            Assert.AreEqual(AsakiLoadSceneMode.Additive, additiveMode);
        }

        [Test]
        public void AsakiLoadSceneMode_Equality_ComparesCorrectly()
        {
            // Arrange
            var mode1 = AsakiLoadSceneMode.Single;
            var mode2 = AsakiLoadSceneMode.Single;
            var mode3 = AsakiLoadSceneMode.Additive;

            // Assert
            Assert.AreEqual(mode1, mode2);
            Assert.AreNotEqual(mode1, mode3);
        }

        #endregion

        #region AsakiSceneActivation Tests

        [Test]
        public void AsakiSceneActivation_Immediate_HasValueZero()
        {
            // Assert
            Assert.AreEqual(0, (int)AsakiSceneActivation.Immediate);
        }

        [Test]
        public void AsakiSceneActivation_ManualConfirm_HasValueOne()
        {
            // Assert
            Assert.AreEqual(1, (int)AsakiSceneActivation.ManualConfirm);
        }

        [Test]
        public void AsakiSceneActivation_Values_AreSequential()
        {
            // Assert
            Assert.AreEqual(0, (int)AsakiSceneActivation.Immediate);
            Assert.AreEqual(1, (int)AsakiSceneActivation.ManualConfirm);
        }

        [Test]
        public void AsakiSceneActivation_Immediate_IsDefaultValue()
        {
            // Arrange & Act
            AsakiSceneActivation activation = default;

            // Assert
            Assert.AreEqual(AsakiSceneActivation.Immediate, activation);
        }

        [Test]
        public void AsakiSceneActivation_CanBeCastToInt()
        {
            // Arrange & Act
            int immediateValue = (int)AsakiSceneActivation.Immediate;
            int manualValue = (int)AsakiSceneActivation.ManualConfirm;

            // Assert
            Assert.AreEqual(0, immediateValue);
            Assert.AreEqual(1, manualValue);
        }

        [Test]
        public void AsakiSceneActivation_CanBeParsedFromInt()
        {
            // Arrange & Act
            var immediateActivation = (AsakiSceneActivation)0;
            var manualActivation = (AsakiSceneActivation)1;

            // Assert
            Assert.AreEqual(AsakiSceneActivation.Immediate, immediateActivation);
            Assert.AreEqual(AsakiSceneActivation.ManualConfirm, manualActivation);
        }

        [Test]
        public void AsakiSceneActivation_Equality_ComparesCorrectly()
        {
            // Arrange
            var activation1 = AsakiSceneActivation.Immediate;
            var activation2 = AsakiSceneActivation.Immediate;
            var activation3 = AsakiSceneActivation.ManualConfirm;

            // Assert
            Assert.AreEqual(activation1, activation2);
            Assert.AreNotEqual(activation1, activation3);
        }

        #endregion

        #region Combined Enum Tests

        [Test]
        public void Enums_CanBeUsedInSwitchStatement()
        {
            // Arrange
            var mode = AsakiLoadSceneMode.Additive;
            var activation = AsakiSceneActivation.ManualConfirm;

            string modeResult = "";
            string activationResult = "";

            // Act
            switch (mode)
            {
                case AsakiLoadSceneMode.Single:
                    modeResult = "Single";
                    break;
                case AsakiLoadSceneMode.Additive:
                    modeResult = "Additive";
                    break;
            }

            switch (activation)
            {
                case AsakiSceneActivation.Immediate:
                    activationResult = "Immediate";
                    break;
                case AsakiSceneActivation.ManualConfirm:
                    activationResult = "ManualConfirm";
                    break;
            }

            // Assert
            Assert.AreEqual("Additive", modeResult);
            Assert.AreEqual("ManualConfirm", activationResult);
        }

        [Test]
        public void Enums_CanBeUsedAsDictionaryKeys()
        {
            // Arrange
            var dictionary = new System.Collections.Generic.Dictionary<AsakiLoadSceneMode, string>
            {
                [AsakiLoadSceneMode.Single] = "Single Mode Description",
                [AsakiLoadSceneMode.Additive] = "Additive Mode Description"
            };

            // Act & Assert
            Assert.AreEqual("Single Mode Description", dictionary[AsakiLoadSceneMode.Single]);
            Assert.AreEqual("Additive Mode Description", dictionary[AsakiLoadSceneMode.Additive]);
        }

        [Test]
        public void Enums_ToString_ReturnsEnumName()
        {
            // Arrange & Act
            var singleString = AsakiLoadSceneMode.Single.ToString();
            var additiveString = AsakiLoadSceneMode.Additive.ToString();
            var immediateString = AsakiSceneActivation.Immediate.ToString();
            var manualString = AsakiSceneActivation.ManualConfirm.ToString();

            // Assert
            Assert.AreEqual("Single", singleString);
            Assert.AreEqual("Additive", additiveString);
            Assert.AreEqual("Immediate", immediateString);
            Assert.AreEqual("ManualConfirm", manualString);
        }

        [Test]
        public void Enums_Parse_ConvertsStringToEnum()
        {
            // Arrange & Act
            var singleMode = (AsakiLoadSceneMode)System.Enum.Parse(typeof(AsakiLoadSceneMode), "Single");
            var additiveMode = (AsakiLoadSceneMode)System.Enum.Parse(typeof(AsakiLoadSceneMode), "Additive");
            var immediateActivation = (AsakiSceneActivation)System.Enum.Parse(typeof(AsakiSceneActivation), "Immediate");
            var manualActivation = (AsakiSceneActivation)System.Enum.Parse(typeof(AsakiSceneActivation), "ManualConfirm");

            // Assert
            Assert.AreEqual(AsakiLoadSceneMode.Single, singleMode);
            Assert.AreEqual(AsakiLoadSceneMode.Additive, additiveMode);
            Assert.AreEqual(AsakiSceneActivation.Immediate, immediateActivation);
            Assert.AreEqual(AsakiSceneActivation.ManualConfirm, manualActivation);
        }

        [Test]
        public void Enums_TryParse_HandlesInvalidValues()
        {
            // Arrange & Act
            bool singleResult = System.Enum.TryParse<AsakiLoadSceneMode>("Single", out var singleMode);
            bool invalidResult = System.Enum.TryParse<AsakiLoadSceneMode>("Invalid", out var invalidMode);

            // Assert
            Assert.IsTrue(singleResult);
            Assert.AreEqual(AsakiLoadSceneMode.Single, singleMode);
            Assert.IsFalse(invalidResult);
        }

        [Test]
        public void Enums_GetValues_ReturnsAllValues()
        {
            // Arrange & Act
            var loadModes = (AsakiLoadSceneMode[])System.Enum.GetValues(typeof(AsakiLoadSceneMode));
            var activationModes = (AsakiSceneActivation[])System.Enum.GetValues(typeof(AsakiSceneActivation));

            // Assert
            Assert.AreEqual(2, loadModes.Length);
            Assert.Contains(AsakiLoadSceneMode.Single, loadModes);
            Assert.Contains(AsakiLoadSceneMode.Additive, loadModes);

            Assert.AreEqual(2, activationModes.Length);
            Assert.Contains(AsakiSceneActivation.Immediate, activationModes);
            Assert.Contains(AsakiSceneActivation.ManualConfirm, activationModes);
        }

        [Test]
        public void Enums_GetNames_ReturnsAllNames()
        {
            // Arrange & Act
            var loadModeNames = System.Enum.GetNames(typeof(AsakiLoadSceneMode));
            var activationNames = System.Enum.GetNames(typeof(AsakiSceneActivation));

            // Assert
            Assert.AreEqual(2, loadModeNames.Length);
            Assert.Contains("Single", loadModeNames);
            Assert.Contains("Additive", loadModeNames);

            Assert.AreEqual(2, activationNames.Length);
            Assert.Contains("Immediate", activationNames);
            Assert.Contains("ManualConfirm", activationNames);
        }

        #endregion

        #region AsakiSceneStateEvent.State Tests

        [Test]
        public void AsakiSceneStateEvent_State_Enum_HasCorrectValues()
        {
            // Assert - Verify the enum values are sequential starting from 0
            Assert.AreEqual(0, (int)AsakiSceneStateEvent.State.Started);
            Assert.AreEqual(1, (int)AsakiSceneStateEvent.State.Completed);
            Assert.AreEqual(2, (int)AsakiSceneStateEvent.State.Failed);
            Assert.AreEqual(3, (int)AsakiSceneStateEvent.State.Cancelled);
        }

        [Test]
        public void AsakiSceneStateEvent_State_CanBeUsedInSwitch()
        {
            // Arrange
            var state = AsakiSceneStateEvent.State.Completed;
            string result = "";

            // Act
            switch (state)
            {
                case AsakiSceneStateEvent.State.Started:
                    result = "Started";
                    break;
                case AsakiSceneStateEvent.State.Completed:
                    result = "Completed";
                    break;
                case AsakiSceneStateEvent.State.Failed:
                    result = "Failed";
                    break;
                case AsakiSceneStateEvent.State.Cancelled:
                    result = "Cancelled";
                    break;
            }

            // Assert
            Assert.AreEqual("Completed", result);
        }

        [Test]
        public void AsakiSceneStateEvent_State_GetValues_ReturnsAllValues()
        {
            // Arrange & Act
            var states = (AsakiSceneStateEvent.State[])System.Enum.GetValues(typeof(AsakiSceneStateEvent.State));

            // Assert
            Assert.AreEqual(4, states.Length);
            Assert.Contains(AsakiSceneStateEvent.State.Started, states);
            Assert.Contains(AsakiSceneStateEvent.State.Completed, states);
            Assert.Contains(AsakiSceneStateEvent.State.Failed, states);
            Assert.Contains(AsakiSceneStateEvent.State.Cancelled, states);
        }

        #endregion
    }
}
