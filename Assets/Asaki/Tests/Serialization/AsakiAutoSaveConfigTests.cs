using Asaki.Core.Serialization;
using NUnit.Framework;

namespace Asaki.Tests.Serialization
{
    /// <summary>
    /// 自动保存配置单元测试
    /// </summary>
    [TestFixture]
    public class AsakiAutoSaveConfigTests
    {
        /// <summary>
        /// 测试：默认配置验证通过
        /// </summary>
        [Test]
        public void DefaultConfig_Validate_ReturnsTrue()
        {
            var config = new AsakiAutoSaveConfig();

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsTrue(isValid);
            Assert.IsNull(errorMessage);
        }

        /// <summary>
        /// 测试：禁用配置验证通过
        /// </summary>
        [Test]
        public void DisabledConfig_Validate_ReturnsTrue()
        {
            var config = AsakiAutoSaveConfig.CreateDisabled();

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsTrue(isValid);
            Assert.IsFalse(config.Enabled);
        }

        /// <summary>
        /// 测试：启用但未设置触发条件验证失败
        /// </summary>
        [Test]
        public void EnabledWithNoTriggers_Validate_ReturnsFalse()
        {
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.None,
            };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("触发条件"));
        }

        /// <summary>
        /// 测试：时间间隔过短验证失败
        /// </summary>
        [Test]
        public void TimeIntervalTooShort_Validate_ReturnsFalse()
        {
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.TimeInterval,
                TimeIntervalSeconds = 10f, // 小于30秒
            };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("30"));
        }

        /// <summary>
        /// 测试：最大存档数为0验证失败
        /// </summary>
        [Test]
        public void MaxAutoSaveCountZero_Validate_ReturnsFalse()
        {
            var config = new AsakiAutoSaveConfig { Enabled = true, MaxAutoSaveCount = 0 };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
        }

        /// <summary>
        /// 测试：缩略图尺寸过小验证失败
        /// </summary>
        [Test]
        public void ThumbnailSizeTooSmall_Validate_ReturnsFalse()
        {
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                GenerateThumbnail = true,
                ThumbnailWidth = 32,
                ThumbnailHeight = 32,
            };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("64"));
        }

        /// <summary>
        /// 测试：缩略图质量超出范围验证失败
        /// </summary>
        [TestCase(0)]
        [TestCase(101)]
        public void ThumbnailQualityOutOfRange_Validate_ReturnsFalse(int quality)
        {
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                GenerateThumbnail = true,
                ThumbnailQuality = quality,
            };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
        }

        /// <summary>
        /// 测试：创建默认配置
        /// </summary>
        [Test]
        public void CreateDefault_ReturnsDefaultSettings()
        {
            var config = AsakiAutoSaveConfig.CreateDefault();

            Assert.IsTrue(config.Enabled);
            Assert.AreEqual(
                AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause,
                config.Triggers
            );
            Assert.AreEqual(300f, config.TimeIntervalSeconds);
            Assert.AreEqual(3, config.MaxAutoSaveCount);
            Assert.IsTrue(config.ShowNotification);
        }

        /// <summary>
        /// 测试：创建频繁保存配置
        /// </summary>
        [Test]
        public void CreateFrequent_ReturnsAggressiveSettings()
        {
            var config = AsakiAutoSaveConfig.CreateFrequent();

            Assert.IsTrue(config.Enabled);
            Assert.AreEqual(AsakiAutoSaveTrigger.All, config.Triggers);
            Assert.AreEqual(60f, config.TimeIntervalSeconds);
            Assert.AreEqual(5, config.MaxAutoSaveCount);
        }

        /// <summary>
        /// 测试：创建保守配置
        /// </summary>
        [Test]
        public void CreateConservative_ReturnsMinimalSettings()
        {
            var config = AsakiAutoSaveConfig.CreateConservative();

            Assert.IsTrue(config.Enabled);
            Assert.AreEqual(
                AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause,
                config.Triggers
            );
            Assert.AreEqual(1, config.MaxAutoSaveCount);
            Assert.IsFalse(config.ShowNotification);
        }

        /// <summary>
        /// 测试：触发条件标志组合
        /// </summary>
        [Test]
        public void TriggerFlags_CanBeCombined()
        {
            var config = new AsakiAutoSaveConfig
            {
                Triggers = AsakiAutoSaveTrigger.TimeInterval | AsakiAutoSaveTrigger.Checkpoint,
            };

            Assert.IsTrue(config.Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval));
            Assert.IsTrue(config.Triggers.HasFlag(AsakiAutoSaveTrigger.Checkpoint));
            Assert.IsFalse(config.Triggers.HasFlag(AsakiAutoSaveTrigger.SceneChange));
        }

        /// <summary>
        /// 测试：有效配置属性设置
        /// </summary>
        [Test]
        public void ValidConfigProperties_AreSetCorrectly()
        {
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.TimeInterval,
                TimeIntervalSeconds = 60f,
                CountdownSeconds = 5f,
                ShowNotification = false,
                NotificationText = "Saving...",
                MaxAutoSaveCount = 5,
                AutoSaveSlotStartIndex = 0,
                GenerateThumbnail = true,
                ThumbnailWidth = 320,
                ThumbnailHeight = 180,
                ThumbnailQuality = 85,
                CheckStorageSpace = true,
                MinFreeSpaceMB = 500,
                MinIntervalBetweenSaves = 30f,
                KeepLatestAutoSave = true,
                SaveOnSceneEnter = false,
                SaveOnSceneExit = true,
            };

            bool isValid = config.Validate(out string errorMessage);

            Assert.IsTrue(isValid, $"Validation failed: {errorMessage}");
            Assert.AreEqual(60f, config.TimeIntervalSeconds);
            Assert.AreEqual(5f, config.CountdownSeconds);
            Assert.AreEqual("Saving...", config.NotificationText);
            Assert.AreEqual(5, config.MaxAutoSaveCount);
            Assert.AreEqual(320, config.ThumbnailWidth);
            Assert.AreEqual(180, config.ThumbnailHeight);
            Assert.AreEqual(85, config.ThumbnailQuality);
        }
    }
}
