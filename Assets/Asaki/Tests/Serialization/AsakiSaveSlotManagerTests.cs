using System;
using System.Collections;
using System.IO;
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
    /// 保存槽位管理器单元测试
    /// </summary>
    [TestFixture]
    public class AsakiSaveSlotManagerTests
    {
        private AsakiSaveSlotManager _slotManager;
        private AsakiSaveService _saveService;
        private AsakiEventService _eventService;
        private string _testRootPath;

        [SetUp]
        public void Setup()
        {
            // 每次测试使用唯一目录，避免测试间干扰
            _testRootPath = Path.Combine(
                Application.temporaryCachePath,
                "AsakiSlotManagerTests",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(_testRootPath);

            _eventService = new AsakiEventService();
            _saveService = new AsakiSaveService(_eventService);

            // 初始化保存服务
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

            // 设置配置
            var configField = typeof(AsakiSaveService).GetField(
                "_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            configField?.SetValue(_saveService, new Asaki.Core.FrameworkSettings.AsakiSaveConfig());

            _saveService.OnInit();

            // 创建槽位管理器（使用新的构造函数）
            _slotManager = new AsakiSaveSlotManager(_saveService, _eventService);
            _slotManager.OnInit();
        }

        [TearDown]
        public void TearDown()
        {
            _slotManager?.OnDispose();
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
        /// 测试：最大槽位数量
        /// </summary>
        [Test]
        public void MaxSlots_ReturnsConfiguredValue()
        {
            Assert.Greater(_slotManager.MaxSlots, 0);
        }

        /// <summary>
        /// 测试：自动保存槽位索引
        /// </summary>
        [Test]
        public void AutoSaveSlotIndex_ReturnsConfiguredValue()
        {
            Assert.AreEqual(0, _slotManager.AutoSaveSlotIndex);
        }

        /// <summary>
        /// 测试：快速保存槽位索引
        /// </summary>
        [Test]
        public void QuickSaveSlotIndex_ReturnsConfiguredValue()
        {
            Assert.AreEqual(1, _slotManager.QuickSaveSlotIndex);
        }

        /// <summary>
        /// 测试：获取所有槽位包含空槽位
        /// </summary>
        [Test]
        public void GetAllSlots_ReturnsAllSlots()
        {
            var slots = _slotManager.GetAllSlots();

            Assert.IsNotNull(slots);
            Assert.AreEqual(_slotManager.MaxSlots, slots.Count);
        }

        /// <summary>
        /// 测试：初始时所有槽位为空
        /// </summary>
        [Test]
        public void GetOccupiedSlots_InitialState_ReturnsEmpty()
        {
            var slots = _slotManager.GetOccupiedSlots();

            Assert.IsNotNull(slots);
            Assert.AreEqual(0, slots.Count);
        }

        /// <summary>
        /// 测试：获取第一个空槽位
        /// </summary>
        [Test]
        public void GetFirstEmptySlot_InitialState_ReturnsZero()
        {
            int slotId = _slotManager.GetFirstEmptySlot();

            Assert.AreEqual(0, slotId);
        }

        /// <summary>
        /// 测试：检查槽位是否为空
        /// </summary>
        [Test]
        public void IsSlotEmpty_EmptySlot_ReturnsTrue()
        {
            Assert.IsTrue(_slotManager.IsSlotEmpty(0));
            Assert.IsTrue(_slotManager.IsSlotEmpty(5));
        }

        /// <summary>
        /// 测试：检查槽位是否有效
        /// </summary>
        [UnityTest]
        public IEnumerator IsSlotValid_AfterSave_ReturnsTrue() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("Test", new TestSaveData());

                Assert.IsTrue(_slotManager.IsSlotValid(0));
                Assert.IsFalse(_slotManager.IsSlotEmpty(0));
            });

        /// <summary>
        /// 测试：获取已使用槽位数量
        /// </summary>
        [UnityTest]
        public IEnumerator GetUsedSlotCount_AfterSaves_ReturnsCorrectCount()
        {
            return UniTask.ToCoroutine(async () =>
            {
                IAsakiSaveSlot slot1 = await _slotManager.CreateSaveAsync(
                    "Save 1",
                    new TestSaveData()
                );
                IAsakiSaveSlot slot2 = await _slotManager.CreateSaveAsync(
                    "Save 2",
                    new TestSaveData()
                );

                int count = _slotManager.GetUsedSlotCount();
                Assert.AreEqual(2, count);
                _slotManager.DeleteSave(slot1.SlotId);
                _slotManager.DeleteSave(slot2.SlotId);
            });
        }

        /// <summary>
        /// 测试：创建新存档
        /// </summary>
        [UnityTest]
        public IEnumerator CreateSaveAsync_CreatesNewSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                var slot = await _slotManager.CreateSaveAsync(
                    "My Save",
                    new TestSaveData { PlayerName = "Hero" }
                );

                Assert.IsNotNull(slot);
                Assert.AreEqual("My Save", slot.SaveName);
                Assert.AreEqual(AsakiSaveSlotStatus.Occupied, slot.Status);
            });

        /// <summary>
        /// 测试：覆盖现有存档
        /// </summary>
        [UnityTest]
        public IEnumerator OverwriteSaveAsync_UpdatesExistingSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                // 先创建一个存档
                var slot1 = await _slotManager.CreateSaveAsync("Original", new TestSaveData());

                // 覆盖第一个存档
                var slot = await _slotManager.OverwriteSaveAsync(
                    slot1.SlotId,
                    "Updated",
                    new TestSaveData { PlayerName = "New" }
                );

                Assert.IsNotNull(slot);
                Assert.AreEqual("Updated", slot.SaveName);
            });

        /// <summary>
        /// 测试：加载存档
        /// </summary>
        [UnityTest]
        public IEnumerator LoadSaveAsync_LoadsCorrectData() =>
            UniTask.ToCoroutine(async () =>
            {
                TestSaveData originalData = new TestSaveData
                {
                    PlayerName = "TestPlayer",
                    PlayerLevel = 42,
                };
                IAsakiSaveSlot slot1 = await _slotManager.CreateSaveAsync("LoadTest", originalData);

                (IAsakiSaveSlot slot, TestSaveData data) =
                    await _slotManager.LoadSaveAsync<TestSaveData>(slot1.SlotId);

                Assert.AreEqual("TestPlayer", data.PlayerName);
                Assert.AreEqual(42, data.PlayerLevel);
            });

        /// <summary>
        /// 测试：加载最新的存档
        /// </summary>
        [UnityTest]
        public IEnumerator LoadLatestSaveAsync_ReturnsMostRecent()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("Older", new TestSaveData());
                await UniTask.Delay(100); // 确保时间不同
                await _slotManager.CreateSaveAsync("Newer", new TestSaveData());

                (IAsakiSaveSlot Slot, TestSaveData Data)? result =
                    await _slotManager.LoadLatestSaveAsync<TestSaveData>();

                Assert.IsTrue(result.HasValue);
                Assert.AreEqual("Newer", result.Value.Slot.SaveName);
            });
        }

        /// <summary>
        /// 测试：删除存档
        /// </summary>
        [UnityTest]
        public IEnumerator DeleteSave_RemovesSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("ToDelete", new TestSaveData());
                Assert.IsTrue(_slotManager.IsSlotValid(0));

                bool deleted = _slotManager.DeleteSave(0);

                Assert.IsTrue(deleted);
                Assert.IsTrue(_slotManager.IsSlotEmpty(0));
            });

        /// <summary>
        /// 测试：快速保存
        /// </summary>
        [UnityTest]
        public IEnumerator QuickSaveAsync_SavesToQuickSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                var slot = await _slotManager.QuickSaveAsync(
                    new TestSaveData { PlayerName = "Quick" }
                );

                Assert.AreEqual(_slotManager.QuickSaveSlotIndex, slot.SlotId);
                Assert.IsTrue(slot.SaveName.Contains("快速"));
            });

        /// <summary>
        /// 测试：自动保存
        /// </summary>
        [UnityTest]
        public IEnumerator AutoSaveAsync_SavesToAutoSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                var slot = await _slotManager.AutoSaveAsync(
                    new TestSaveData { PlayerName = "Auto" }
                );

                Assert.AreEqual(_slotManager.AutoSaveSlotIndex, slot.SlotId);
                Assert.IsTrue(slot.SaveName.Contains("自动"));
            });

        /// <summary>
        /// 测试：加载快速保存
        /// </summary>
        [UnityTest]
        public IEnumerator LoadQuickSaveAsync_LoadsQuickSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.QuickSaveAsync(new TestSaveData { PlayerName = "QuickLoad" });

                var result = await _slotManager.LoadQuickSaveAsync<TestSaveData>();

                Assert.IsTrue(result.HasValue);
                Assert.AreEqual("QuickLoad", result.Value.Data.PlayerName);
            });

        /// <summary>
        /// 测试：加载自动保存
        /// </summary>
        [UnityTest]
        public IEnumerator LoadAutoSaveAsync_LoadsAutoSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.AutoSaveAsync(new TestSaveData { PlayerName = "AutoLoad" });

                var result = await _slotManager.LoadAutoSaveAsync<TestSaveData>();

                Assert.IsTrue(result.HasValue);
                Assert.AreEqual("AutoLoad", result.Value.Data.PlayerName);
            });

        /// <summary>
        /// 测试：复制存档
        /// </summary>
        [UnityTest]
        public IEnumerator CopySaveAsync_CreatesCopy() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("Original", new TestSaveData());

                var newSlot = await _slotManager.CopySaveAsync(0);

                Assert.IsNotNull(newSlot);
                Assert.IsTrue(newSlot.SaveName.Contains("复制"));
            });

        /// <summary>
        /// 测试：查找最佳保存槽位
        /// </summary>
        [Test]
        public void FindBestSlotForSave_EmptySlots_ReturnsFirstEmpty()
        {
            int slotId = _slotManager.FindBestSlotForSave();

            Assert.AreEqual(0, slotId);
        }

        /// <summary>
        /// 测试：检查是否有空槽位
        /// </summary>
        [Test]
        public void HasEmptySlot_InitialState_ReturnsTrue()
        {
            Assert.IsTrue(_slotManager.HasEmptySlot());
        }

        /// <summary>
        /// 测试：获取槽位信息
        /// </summary>
        [Test]
        public void GetSlotInfo_EmptySlot_ReturnsEmptySlot()
        {
            var slot = _slotManager.GetSlotInfo(5);

            Assert.IsNotNull(slot);
            Assert.AreEqual(5, slot.SlotId);
        }

        /// <summary>
        /// 测试：锁定槽位
        /// </summary>
        [UnityTest]
        public IEnumerator LockSlot_LocksSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("ToLock", new TestSaveData());

                bool locked = _slotManager.LockSlot(0);

                Assert.IsTrue(locked);
                // 锁定后槽位状态应为 Locked
                var slot = _slotManager.GetSlotInfo(0);
                Assert.AreEqual(AsakiSaveSlotStatus.Locked, slot.Status);
            });

        /// <summary>
        /// 测试：解锁槽位
        /// </summary>
        [UnityTest]
        public IEnumerator UnlockSlot_UnlocksSlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await _slotManager.CreateSaveAsync("ToUnlock", new TestSaveData());
                _slotManager.LockSlot(0);

                bool unlocked = _slotManager.UnlockSlot(0);

                Assert.IsTrue(unlocked);
            });

        // ===== 测试数据类 =====

        public class TestSaveData : IAsakiSavable
        {
            public string PlayerName;
            public int PlayerLevel;

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("PlayerName", PlayerName ?? "");
                writer.WriteInt("PlayerLevel", PlayerLevel);
            }

            public void Deserialize(IAsakiReader reader)
            {
                PlayerName = reader.ReadString("PlayerName");
                PlayerLevel = reader.ReadInt("PlayerLevel");
            }
        }
    }
}
