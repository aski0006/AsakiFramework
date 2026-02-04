using System;
using Asaki.Core.Serialization;
using NUnit.Framework;

namespace Asaki.Tests.Serialization
{
    /// <summary>
    /// 保存槽位信息单元测试
    /// </summary>
    [TestFixture]
    public class AsakiSaveSlotTests
    {
        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// 测试：默认槽位状态为空
        /// </summary>
        [Test]
        public void DefaultSlotStatus_IsEmpty()
        {
            AsakiSaveSlot slot = new AsakiSaveSlot
            {
                SlotId = 0
            };

            Assert.AreEqual(AsakiSaveSlotStatus.Empty, slot.Status);
            Assert.IsTrue(slot.IsEmpty);
            Assert.IsFalse(slot.IsValid);
        }

        /// <summary>
        /// 测试：已占用槽位状态
        /// </summary>
        [Test]
        public void OccupiedSlot_StatusIsOccupied()
        {
            var slot = new AsakiSaveSlot
            {
                SlotId = 1,
                Status = AsakiSaveSlotStatus.Occupied,
                SaveName = "Test Save"
            };

            Assert.AreEqual(AsakiSaveSlotStatus.Occupied, slot.Status);
            Assert.IsFalse(slot.IsEmpty);
            Assert.IsTrue(slot.IsValid);
        }

        /// <summary>
        /// 测试：格式化游戏时长 - 小于1小时
        /// </summary>
        [Test]
        public void GetFormattedPlayTime_LessThanOneHour_ReturnsMinutesAndSeconds()
        {
            var slot = new AsakiSaveSlot
            {
                PlayTimeSeconds = 3661 // 1小时1分1秒
            };

            var result = slot.GetFormattedPlayTime();

            Assert.AreEqual("1h 1m", result);
        }

        /// <summary>
        /// 测试：格式化游戏时长 - 大于1小时
        /// </summary>
        [Test]
        public void GetFormattedPlayTime_MoreThanOneHour_ReturnsHoursAndMinutes()
        {
            var slot = new AsakiSaveSlot
            {
                PlayTimeSeconds = 3661 // 1小时1分1秒
            };

            var result = slot.GetFormattedPlayTime();

            Assert.AreEqual("1h 1m", result);
        }

        /// <summary>
        /// 测试：格式化游戏时长 - 0秒
        /// </summary>
        [Test]
        public void GetFormattedPlayTime_ZeroSeconds_ReturnsZeroMinutes()
        {
            var slot = new AsakiSaveSlot
            {
                PlayTimeSeconds = 0
            };

            var result = slot.GetFormattedPlayTime();

            Assert.AreEqual("0m 0s", result);
        }

        /// <summary>
        /// 测试：格式化保存时间 - 无效时间
        /// </summary>
        [Test]
        public void GetFormattedSaveTime_InvalidTime_ReturnsDash()
        {
            var slot = new AsakiSaveSlot
            {
                LastSaveTime = 0
            };

            var result = slot.GetFormattedSaveTime();

            Assert.AreEqual("--", result);
        }

        /// <summary>
        /// 测试：槽位元数据属性
        /// </summary>
        [Test]
        public void SlotProperties_SetAndGetCorrectly()
        {
            var slot = new AsakiSaveSlot
            {
                SlotId = 5,
                SaveName = "My Save",
                LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PlayTimeSeconds = 3600,
                ProgressPercent = 75.5f,
                CurrentLevel = "Level 3-2",
                PlayerLevel = 10,
                PlayerName = "Hero",
                GameVersion = "1.0.0",
                Description = "Test description",
                Tags = new[] { "checkpoint", "boss" }
            };

            Assert.AreEqual(5, slot.SlotId);
            Assert.AreEqual("My Save", slot.SaveName);
            Assert.AreEqual(3600, slot.PlayTimeSeconds);
            Assert.AreEqual(75.5f, slot.ProgressPercent);
            Assert.AreEqual("Level 3-2", slot.CurrentLevel);
            Assert.AreEqual(10, slot.PlayerLevel);
            Assert.AreEqual("Hero", slot.PlayerName);
            Assert.AreEqual("1.0.0", slot.GameVersion);
            Assert.AreEqual("Test description", slot.Description);
            Assert.AreEqual(2, slot.Tags.Length);
        }

        /// <summary>
        /// 测试：序列化和反序列化槽位信息
        /// </summary>
        [Test]
        public void SerializeDeserialize_RoundTrip_PreservesData()
        {
            var original = new AsakiSaveSlot
            {
                SlotId = 1,
                SaveName = "Test Save",
                LastSaveTime = 1234567890,
                Status = AsakiSaveSlotStatus.Occupied,
                PlayTimeSeconds = 3600,
                ProgressPercent = 50.0f,
                CurrentLevel = "Level 1",
                PlayerLevel = 5,
                PlayerName = "TestPlayer",
                GameVersion = "1.0.0",
                Description = "Test",
                Tags = new[] { "tag1", "tag2" }
            };

            // 序列化到内存流
            using var stream = new System.IO.MemoryStream();
            var writer = new Asaki.Unity.Services.Serialization.AsakiBinaryWriter(stream);
            original.Serialize(writer);

            // 反序列化
            stream.Position = 0;
            var reader = new Asaki.Unity.Services.Serialization.AsakiBinaryReader(stream);
            var deserialized = new AsakiSaveSlot();
            deserialized.Deserialize(reader);

            // 验证
            Assert.AreEqual(original.SlotId, deserialized.SlotId);
            Assert.AreEqual(original.SaveName, deserialized.SaveName);
            Assert.AreEqual(original.LastSaveTime, deserialized.LastSaveTime);
            Assert.AreEqual(original.Status, deserialized.Status);
            Assert.AreEqual(original.PlayTimeSeconds, deserialized.PlayTimeSeconds);
            Assert.AreEqual(original.ProgressPercent, deserialized.ProgressPercent);
            Assert.AreEqual(original.CurrentLevel, deserialized.CurrentLevel);
            Assert.AreEqual(original.PlayerLevel, deserialized.PlayerLevel);
            Assert.AreEqual(original.PlayerName, deserialized.PlayerName);
            Assert.AreEqual(original.GameVersion, deserialized.GameVersion);
            Assert.AreEqual(original.Description, deserialized.Description);
        }
    }
}
