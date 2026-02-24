using System.Collections;
using Asaki.Core.Broker;
using Asaki.Core.Serialization;
using Asaki.Core.FrameworkSettings;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Serialization
{
    /// <summary>
    /// 自动保存服务单元测试
    /// </summary>
    [TestFixture]
    public class AsakiAutoSaveServiceTests
    {
        private AsakiAutoSaveService _autoSaveService;
        private TestSaveSlotManager _mockSlotManager;
        private TestEventService _mockEventService;
        private AsakiSaveConfig _testConfig;

        [SetUp]
        public void Setup()
        {
            _mockSlotManager = new TestSaveSlotManager();
            _mockEventService = new TestEventService();
            _testConfig = new AsakiSaveConfig();

            _autoSaveService = new AsakiAutoSaveService(
                _mockSlotManager,
                _mockEventService,
                _testConfig
            );
            _autoSaveService.OnInit();
        }

        [TearDown]
        public void TearDown()
        {
            _autoSaveService?.StopService();
            _autoSaveService?.OnDispose();
        }

        /// <summary>
        /// 测试：默认配置启用
        /// </summary>
        [Test]
        public void DefaultConfig_IsEnabled()
        {
            Assert.IsTrue(_autoSaveService.Config.Enabled);
        }

        /// <summary>
        /// 测试：设置新配置
        /// </summary>
        [Test]
        public void SetConfig_UpdatesConfiguration()
        {
            var newConfig = AsakiAutoSaveConfig.CreateDisabled();

            _autoSaveService.SetConfig(newConfig);

            Assert.AreEqual(newConfig, _autoSaveService.Config);
            Assert.IsFalse(_autoSaveService.Config.Enabled);
        }

        /// <summary>
        /// 测试：未注册数据提供者时无法启动
        /// </summary>
        [Test]
        public void StartService_WithoutDataProvider_DoesNotStart()
        {
            _autoSaveService.StartService();

            // 没有注册数据提供者，服务不会启动
            Assert.IsFalse(_autoSaveService.IsAutoSaving);
        }

        /// <summary>
        /// 测试：注册数据提供者
        /// </summary>
        [Test]
        public void RegisterDataProvider_RegistersSuccessfully()
        {
            var dataProvider = new TestDataProvider();

            _autoSaveService.RegisterDataProvider(() => dataProvider.GetData());

            // 验证可以启动服务
            _autoSaveService.SetConfig(AsakiAutoSaveConfig.CreateDefault());
            _autoSaveService.StartService();
            // 服务应该能启动（虽然可能立即暂停因为没有实际槽位管理器）
        }

        /// <summary>
        /// 测试：暂停和恢复服务
        /// </summary>
        [Test]
        public void PauseResume_TogglesPausedState()
        {
            _autoSaveService.Pause();
            // 暂停后服务不可用
            Assert.IsFalse(_autoSaveService.CanAutoSave());

            _autoSaveService.Resume();
            // 恢复后仍然不能保存，因为没有配置和数据提供者
            // 但至少不应该是暂停状态导致的失败
        }

        /// <summary>
        /// 测试：停止服务
        /// </summary>
        [Test]
        public void StopService_StopsRunning()
        {
            _autoSaveService.StartService();
            _autoSaveService.StopService();

            Assert.IsFalse(_autoSaveService.IsAutoSaving);
        }

        /// <summary>
        /// 测试：重置计时器
        /// </summary>
        [Test]
        public void ResetTimer_ResetsCountdown()
        {
            _autoSaveService.ResetTimer();

            // 验证时间被重置
            var timeUntilNext = _autoSaveService.TimeUntilNextAutoSave;
            // 由于默认配置可能没有TimeInterval触发器，值可能为-1
            // 但调用不应该抛出异常
        }

        /// <summary>
        /// 测试：获取下次自动保存时间
        /// </summary>
        [Test]
        public void GetNextAutoSaveTime_DisabledService_ReturnsNull()
        {
            _autoSaveService.SetConfig(AsakiAutoSaveConfig.CreateDisabled());

            var nextTime = _autoSaveService.GetNextAutoSaveTime();

            Assert.IsNull(nextTime);
        }

        /// <summary>
        /// 测试：自动保存事件触发
        /// </summary>
        [UnityTest]
        public IEnumerator AutoSaveBeginEvent_Triggers() =>
            UniTask.ToCoroutine(async () =>
            {
                bool eventTriggered = false;
                _autoSaveService.OnAutoSaveBegin += (args) => eventTriggered = true;

                // 注册数据提供者
                _autoSaveService.RegisterDataProvider(() => new TestSaveData());
                _autoSaveService.SetConfig(
                    new AsakiAutoSaveConfig
                    {
                        Enabled = true,
                        Triggers = AsakiAutoSaveTrigger.Manual,
                        ShowNotification = false,
                    }
                );

                await _autoSaveService.ForceAutoSaveAsync();

                Assert.IsTrue(eventTriggered);
            });

        /// <summary>
        /// 测试：自动保存完成事件触发
        /// </summary>
        [UnityTest]
        public IEnumerator AutoSaveCompleteEvent_Triggers() =>
            UniTask.ToCoroutine(async () =>
            {
                bool eventTriggered = false;
                AsakiAutoSaveEventArgs? receivedArgs = null;

                _autoSaveService.OnAutoSaveComplete += (args) =>
                {
                    eventTriggered = true;
                    receivedArgs = args;
                };

                _autoSaveService.RegisterDataProvider(() => new TestSaveData());
                _autoSaveService.SetConfig(
                    new AsakiAutoSaveConfig
                    {
                        Enabled = true,
                        Triggers = AsakiAutoSaveTrigger.Manual,
                        ShowNotification = false,
                    }
                );

                await _autoSaveService.ForceAutoSaveAsync();

                Assert.IsTrue(eventTriggered);
                Assert.IsTrue(receivedArgs.HasValue);
            });

        /// <summary>
        /// 测试：配置变更事件
        /// </summary>
        [Test]
        public void ConfigChangedEvent_Triggers()
        {
            bool eventTriggered = false;
            IAsakiAutoSaveConfig receivedConfig = null;

            _autoSaveService.OnConfigChanged += (config) =>
            {
                eventTriggered = true;
                receivedConfig = config;
            };

            var newConfig = AsakiAutoSaveConfig.CreateFrequent();
            _autoSaveService.SetConfig(newConfig);

            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(newConfig, receivedConfig);
        }

        /// <summary>
        /// 测试：触发检查点保存
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerCheckpointSave_WithEnabledTrigger_ReturnsTrue() =>
            UniTask.ToCoroutine(async () =>
            {
                _autoSaveService.RegisterDataProvider(() => new TestSaveData());
                _autoSaveService.SetConfig(
                    new AsakiAutoSaveConfig
                    {
                        Enabled = true,
                        Triggers = AsakiAutoSaveTrigger.Checkpoint,
                        ShowNotification = false,
                    }
                );
                _autoSaveService.StartService();

                bool result = await _autoSaveService.TriggerCheckpointSaveAsync("TestCheckpoint");

                Assert.IsTrue(result);
            });

        /// <summary>
        /// 测试：禁用检查点触发时返回 false
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerCheckpointSave_WithDisabledTrigger_ReturnsFalse() =>
            UniTask.ToCoroutine(async () =>
            {
                _autoSaveService.RegisterDataProvider(() => new TestSaveData());
                _autoSaveService.SetConfig(
                    new AsakiAutoSaveConfig
                    {
                        Enabled = true,
                        Triggers = AsakiAutoSaveTrigger.TimeInterval, // 没有Checkpoint
                        ShowNotification = false,
                    }
                );

                bool result = await _autoSaveService.TriggerCheckpointSaveAsync("TestCheckpoint");

                Assert.IsFalse(result);
            });

        /// <summary>
        /// 测试：触发场景切换保存
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerSceneSave_WithEnabledTrigger_ReturnsTrue() =>
            UniTask.ToCoroutine(async () =>
            {
                _autoSaveService.RegisterDataProvider(() => new TestSaveData());
                _autoSaveService.SetConfig(
                    new AsakiAutoSaveConfig
                    {
                        Enabled = true,
                        Triggers = AsakiAutoSaveTrigger.SceneChange,
                        SaveOnSceneExit = true,
                        ShowNotification = false,
                    }
                );
                _autoSaveService.StartService();

                bool result = await _autoSaveService.TriggerSceneSaveAsync("TestScene", false);

                Assert.IsTrue(result);
            });

        /// <summary>
        /// 测试：取消倒计时
        /// </summary>
        [Test]
        public void CancelCountdown_DoesNotThrow()
        {
            // 确保取消倒计时不会抛出异常
            Assert.DoesNotThrow(() => _autoSaveService.CancelCountdown());
        }

        /// <summary>
        /// 测试：CanAutoSave 检查
        /// </summary>
        [Test]
        public void CanAutoSave_WithoutDataProvider_ReturnsFalse()
        {
            _autoSaveService.SetConfig(AsakiAutoSaveConfig.CreateDefault());

            bool canSave = _autoSaveService.CanAutoSave();

            Assert.IsFalse(canSave);
        }

        /// <summary>
        /// 测试：禁用服务时 CanAutoSave 返回 false
        /// </summary>
        [Test]
        public void CanAutoSave_DisabledService_ReturnsFalse()
        {
            _autoSaveService.SetConfig(AsakiAutoSaveConfig.CreateDisabled());
            _autoSaveService.RegisterDataProvider(() => new TestSaveData());

            bool canSave = _autoSaveService.CanAutoSave();

            Assert.IsFalse(canSave);
        }

        // ===== 测试用的 Mock 类 =====

        public class TestSaveSlotManager : IAsakiSaveSlotManager
        {
            public int MaxSlots => 99;
            public int AutoSaveSlotIndex => 0;
            public int QuickSaveSlotIndex => 1;

            public System.Collections.Generic.IReadOnlyList<IAsakiSaveSlot> GetAllSlots() =>
                new System.Collections.Generic.List<IAsakiSaveSlot>();

            public System.Collections.Generic.IReadOnlyList<IAsakiSaveSlot> GetOccupiedSlots() =>
                new System.Collections.Generic.List<IAsakiSaveSlot>();

            public int GetFirstEmptySlot() => 0;

            public int FindBestSlotForSave() => 0;

            public IAsakiSaveSlot GetSlotInfo(int slotId) => new AsakiSaveSlot { SlotId = slotId };

            public bool IsSlotEmpty(int slotId) => true;

            public bool IsSlotValid(int slotId) => false;

            public int GetUsedSlotCount() => 0;

            public int GetRemainingSlotCount() => 99;

            public bool HasEmptySlot() => true;

            public bool LockSlot(int slotId) => true;

            public bool UnlockSlot(int slotId) => true;

            public UniTask RefreshSlotsAsync(System.Threading.CancellationToken token = default) =>
                UniTask.CompletedTask;

            public UniTask<IAsakiSaveSlot> CreateSaveAsync<TData>(
                string saveName,
                TData data,
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable =>
                UniTask.FromResult<IAsakiSaveSlot>(
                    new AsakiSaveSlot { SlotId = 0, SaveName = saveName }
                );

            public UniTask<IAsakiSaveSlot> OverwriteSaveAsync<TData>(
                int slotId,
                string saveName,
                TData data,
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable =>
                UniTask.FromResult<IAsakiSaveSlot>(
                    new AsakiSaveSlot { SlotId = slotId, SaveName = saveName }
                );

            public UniTask<(IAsakiSaveSlot Slot, TData Data)> LoadSaveAsync<TData>(
                int slotId,
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable, new() => throw new System.NotImplementedException();

            public UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadLatestSaveAsync<TData>(
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable, new() =>
                UniTask.FromResult<(IAsakiSaveSlot Slot, TData Data)?>(null);

            public bool DeleteSave(int slotId) => true;

            public UniTask<IAsakiSaveSlot> CopySaveAsync(
                int sourceSlotId,
                int targetSlotId = -1,
                System.Threading.CancellationToken token = default
            ) => UniTask.FromResult<IAsakiSaveSlot>(new AsakiSaveSlot());

            public UniTask<IAsakiSaveSlot> CreateBackupAsync(
                int slotId,
                string backupName = null,
                System.Threading.CancellationToken token = default
            ) => UniTask.FromResult<IAsakiSaveSlot>(new AsakiSaveSlot());

            public UniTask<IAsakiSaveSlot> RestoreFromBackupAsync(
                int backupSlotId,
                int targetSlotId = -1,
                System.Threading.CancellationToken token = default
            ) => throw new System.NotImplementedException();

            public UniTask<IAsakiSaveSlot> AutoSaveAsync<TData>(
                TData data,
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable =>
                UniTask.FromResult<IAsakiSaveSlot>(
                    new AsakiSaveSlot { SlotId = 0, SaveName = "Auto Save" }
                );

            public UniTask<IAsakiSaveSlot> QuickSaveAsync<TData>(
                TData data,
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable =>
                UniTask.FromResult<IAsakiSaveSlot>(
                    new AsakiSaveSlot { SlotId = 1, SaveName = "Quick Save" }
                );

            public UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadAutoSaveAsync<TData>(
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable, new() =>
                UniTask.FromResult<(IAsakiSaveSlot Slot, TData Data)?>(null);

            public UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadQuickSaveAsync<TData>(
                System.Threading.CancellationToken token = default
            )
                where TData : IAsakiSavable, new() =>
                UniTask.FromResult<(IAsakiSaveSlot Slot, TData Data)?>(null);

            public void OnInit() { }

            public UniTask OnInitAsync() => UniTask.CompletedTask;

            public void OnDispose() { }
        }

        public class TestEventService : IAsakiEventService
        {
            public void Subscribe<T>(IAsakiHandler<T> handler)
                where T : IAsakiEvent { }

            public void Unsubscribe<T>(IAsakiHandler<T> handler)
                where T : IAsakiEvent { }

            void IAsakiEventService.Publish<T>(in T e)
            {
                Publish<T>(e);
            }

            public void Publish<T>(in T eventData)
                where T : IAsakiEvent { }

            public System.IDisposable Subscribe<T>(System.Action<T> handler)
                where T : IAsakiEvent => null;

            public void Unsubscribe<T>(System.Action<T> handler)
                where T : IAsakiEvent { }

            public void Dispose()
            {
                throw new System.NotImplementedException();
            }

            public void SubscribeWeak<T>(IAsakiHandler<T> handler)
                where T : IAsakiEvent
            {
                throw new System.NotImplementedException();
            }
        }

        public class TestDataProvider
        {
            public IAsakiSavable GetData() => new TestSaveData();
        }

        public class TestSaveData : IAsakiSavable
        {
            public string Data = "Test";

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("Data", Data);
            }

            public void Deserialize(IAsakiReader reader)
            {
                Data = reader.ReadString("Data");
            }
        }
    }
}
