using System;
using System.Collections;
using System.IO;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Serialization
{
    /// <summary>
    /// 保存服务单元测试
    /// </summary>
    [TestFixture]
    public class AsakiSaveServiceTests
    {
        private AsakiSaveService _saveService;
        private string _testRootPath;
        private AsakiEventService _eventService;

        [SetUp]
        public void Setup()
        {
            // 每次测试使用唯一目录，避免测试间干扰
            _testRootPath = Path.Combine(
                Application.temporaryCachePath,
                "AsakiSaveTests",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(_testRootPath);

            _eventService = new AsakiEventService();
            _saveService = new AsakiSaveService(_eventService);

            // 手动初始化服务（使用反射设置私有字段）
            var rootPathField = typeof(AsakiSaveService).GetField(
                "_rootPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            rootPathField?.SetValue(_saveService, _testRootPath);

            var debugField = typeof(AsakiSaveService).GetField(
                "_isDebug",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            debugField?.SetValue(_saveService, true);

            _saveService.OnInit();
        }

        [TearDown]
        public void TearDown()
        {
            _saveService?.OnDispose();

            // 清理测试目录
            try
            {
                if (Directory.Exists(_testRootPath))
                {
                    Directory.Delete(Path.GetDirectoryName(_testRootPath), true);
                }
            }
            catch { }
        }

        /// <summary>
        /// 测试：保存目录路径
        /// </summary>
        [Test]
        public void SaveDirectoryPath_ReturnsCorrectPath()
        {
            Assert.IsNotNull(_saveService.SaveDirectoryPath);
            Assert.IsTrue(_saveService.SaveDirectoryPath.Contains("Save"));
        }

        /// <summary>
        /// 测试：最大支持槽位数
        /// </summary>
        [Test]
        public void MaxSupportedSlots_ReturnsPositiveNumber()
        {
            Assert.Greater(_saveService.MaxSupportedSlots, 0);
            Assert.AreEqual(999, _saveService.MaxSupportedSlots);
        }

        /// <summary>
        /// 测试：槽位不存在时返回 false
        /// </summary>
        [Test]
        public void SlotExists_NonExistentSlot_ReturnsFalse()
        {
            bool exists = _saveService.SlotExists(999);
            Assert.IsFalse(exists);
        }

        /// <summary>
        /// 测试：获取已使用槽位列表
        /// </summary>
        [UnityTest]
        public IEnumerator GetUsedSlots_NoSaves_ReturnsEmptyList()
        {
            var slots = _saveService.GetUsedSlots();

            Assert.IsNotNull(slots);
            Assert.AreEqual(0, slots.Count);
            yield return null;
        }

        /// <summary>
        /// 测试：保存和加载存档
        /// </summary>
        [UnityTest]
        public IEnumerator SaveAndLoadSlot_RoundTrip_PreservesData() =>
            UniTask.ToCoroutine(async () =>
            {
                // Arrange
                int slotId = 1;
                var meta = new TestSlotMeta
                {
                    SlotId = slotId,
                    SaveName = "Test Save",
                    LastSaveTime = 0, // 会被自动填充
                };
                var data = new TestSaveData
                {
                    PlayerName = "Hero",
                    PlayerLevel = 10,
                    Score = 1000,
                };

                // Act - Save
                var saveResult = await _saveService.SaveSlotWithResultAsync(slotId, meta, data);
                Assert.IsTrue(saveResult.Success, $"Save failed: {saveResult.ErrorMessage}");

                // Act - Load
                var loadResult = await _saveService.LoadSlotWithResultAsync<
                    TestSlotMeta,
                    TestSaveData
                >(slotId);
                Assert.IsTrue(loadResult.Success, $"Load failed: {loadResult.ErrorMessage}");

                // Assert
                Assert.AreEqual(data.PlayerName, loadResult.Data.PlayerName);
                Assert.AreEqual(data.PlayerLevel, loadResult.Data.PlayerLevel);
                Assert.AreEqual(data.Score, loadResult.Data.Score);
                Assert.AreEqual(meta.SaveName, loadResult.Meta.SaveName);
            });

        /// <summary>
        /// 测试：删除槽位
        /// </summary>
        [UnityTest]
        public IEnumerator DeleteSlot_ExistingSlot_RemovesSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                // Arrange - Create a save
                int slotId = 2;
                var meta = new TestSlotMeta { SaveName = "To Delete" };
                var data = new TestSaveData { PlayerName = "Test" };

                await _saveService.SaveSlotAsync(slotId, meta, data);
                Assert.IsTrue(_saveService.SlotExists(slotId));

                // Act
                bool deleted = _saveService.DeleteSlot(slotId);

                // Assert
                Assert.IsTrue(deleted);
                Assert.IsFalse(_saveService.SlotExists(slotId));
            });

        /// <summary>
        /// 测试：删除不存在的槽位返回 false
        /// </summary>
        [Test]
        public void DeleteSlot_NonExistentSlot_ReturnsFalse()
        {
            bool deleted = _saveService.DeleteSlot(999);
            Assert.IsFalse(deleted);
        }

        /// <summary>
        /// 测试：批量删除槽位
        /// </summary>
        [UnityTest]
        public IEnumerator DeleteSlots_MultipleSlots_DeletesCorrectly() =>
            UniTask.ToCoroutine(async () =>
            {
                // Arrange
                for (int i = 10; i < 15; i++)
                {
                    await _saveService.SaveSlotAsync(
                        i,
                        new TestSlotMeta { SaveName = $"Slot {i}" },
                        new TestSaveData()
                    );
                }

                // Act
                int deletedCount = _saveService.DeleteSlots(new[] { 10, 11, 12 });

                // Assert
                Assert.AreEqual(3, deletedCount);
                Assert.IsFalse(_saveService.SlotExists(10));
                Assert.IsFalse(_saveService.SlotExists(11));
                Assert.IsFalse(_saveService.SlotExists(12));
                Assert.IsTrue(_saveService.SlotExists(13));
                Assert.IsTrue(_saveService.SlotExists(14));
            });

        /// <summary>
        /// 测试：尝试加载不存在的槽位返回失败结果
        /// </summary>
        [UnityTest]
        public IEnumerator LoadSlot_NonExistentSlot_ReturnsFailure() =>
            UniTask.ToCoroutine(async () =>
            {
                var result = await _saveService.LoadSlotWithResultAsync<TestSlotMeta, TestSaveData>(
                    999
                );

                Assert.IsFalse(result.Success);
                Assert.IsNotNull(result.ErrorMessage);
            });

        /// <summary>
        /// 测试：TryLoadSlot 对不存在的槽位返回 null
        /// </summary>
        [UnityTest]
        public IEnumerator TryLoadSlot_NonExistentSlot_ReturnsNull() =>
            UniTask.ToCoroutine(async () =>
            {
                var result = await _saveService.TryLoadSlotAsync<TestSlotMeta, TestSaveData>(999);

                Assert.IsNull(result);
            });

        /// <summary>
        /// 测试：复制槽位
        /// </summary>
        [UnityTest]
        public IEnumerator CopySlot_ExistingSlot_CreatesCopy() =>
            UniTask.ToCoroutine(async () =>
            {
                // Arrange
                int sourceSlot = 20;
                int targetSlot = 21;
                var meta = new TestSlotMeta { SaveName = "Original" };
                var data = new TestSaveData { PlayerName = "OriginalData" };

                await _saveService.SaveSlotAsync(sourceSlot, meta, data);

                // Act
                bool copied = await _saveService.CopySlotAsync(sourceSlot, targetSlot);

                // Assert
                Assert.IsTrue(copied);
                Assert.IsTrue(_saveService.SlotExists(targetSlot));
            });

        /// <summary>
        /// 测试：复制到相同槽位返回 false
        /// </summary>
        [UnityTest]
        public IEnumerator CopySlot_SameSlot_ReturnsFalse() =>
            UniTask.ToCoroutine(async () =>
            {
                int slotId = 22;
                await _saveService.SaveSlotAsync(slotId, new TestSlotMeta(), new TestSaveData());

                bool copied = await _saveService.CopySlotAsync(slotId, slotId);

                Assert.IsFalse(copied);
            });

        /// <summary>
        /// 测试：获取槽位文件大小
        /// </summary>
        [UnityTest]
        public IEnumerator GetSlotFileSize_ExistingSlot_ReturnsSize() =>
            UniTask.ToCoroutine(async () =>
            {
                int slotId = 30;
                var data = new TestSaveData { PlayerName = new string('x', 1000) }; // 较大的数据

                await _saveService.SaveSlotAsync(slotId, new TestSlotMeta(), data);

                long size = _saveService.GetSlotFileSize(slotId);

                Assert.Greater(size, 0);
            });

        /// <summary>
        /// 测试：获取槽位修改时间
        /// </summary>
        [UnityTest]
        public IEnumerator GetSlotLastModifiedTime_ExistingSlot_ReturnsTime() =>
            UniTask.ToCoroutine(async () =>
            {
                int slotId = 31;
                var beforeSave = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _saveService.SaveSlotAsync(slotId, new TestSlotMeta(), new TestSaveData());

                long modifiedTime = _saveService.GetSlotLastModifiedTime(slotId);

                Assert.GreaterOrEqual(modifiedTime, beforeSave - 1); // 允许1秒误差
            });

        // ===== 测试用的数据类 =====

        public class TestSlotMeta : IAsakiSlotMeta
        {
            public int SlotId { get; set; }
            public long LastSaveTime { get; set; }
            public string SaveName { get; set; }

            public void Serialize(IAsakiWriter writer)
            {
                writer.BeginObject(nameof(TestSlotMeta));
                writer.WriteInt(nameof(SlotId), SlotId);
                writer.WriteLong(nameof(LastSaveTime), LastSaveTime);
                writer.WriteString(nameof(SaveName), SaveName ?? "");
                writer.EndObject();
            }

            public void Deserialize(IAsakiReader reader)
            {
                SlotId = reader.ReadInt(nameof(SlotId));
                LastSaveTime = reader.ReadLong(nameof(LastSaveTime));
                SaveName = reader.ReadString(nameof(SaveName));
            }
        }

        public class TestSaveData : IAsakiSavable
        {
            public string PlayerName;
            public int PlayerLevel;
            public int Score;

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("PlayerName", PlayerName ?? "");
                writer.WriteInt("PlayerLevel", PlayerLevel);
                writer.WriteInt("Score", Score);
            }

            public void Deserialize(IAsakiReader reader)
            {
                PlayerName = reader.ReadString("PlayerName");
                PlayerLevel = reader.ReadInt("PlayerLevel");
                Score = reader.ReadInt("Score");
            }
        }
    }
}
