using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Asaki.Core.Broker;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// 保存槽位管理器实现
    /// </summary>
    public class AsakiSaveSlotManager : IAsakiSaveSlotManager
    {
        private const int DEFAULT_MAX_SLOTS = 99;
        private const string BACKUP_DIR_NAME = "Backups";
        private const string META_FILE_NAME = "slot.meta";
        private const string DATA_FILE_NAME = "data.bin";

        private IAsakiSaveService _saveService;
        private IAsakiEventService _eventService;
        private string _rootPath;
        private string _backupPath;
        private Dictionary<int, AsakiSaveSlot> _slotCache;

        /// <inheritdoc />
        public int MaxSlots { get; private set; }

        /// <inheritdoc />
        public int AutoSaveSlotIndex { get; private set; }

        /// <inheritdoc />
        public int QuickSaveSlotIndex { get; private set; }

        public AsakiSaveSlotManager(
            IAsakiSaveService saveService,
            IAsakiEventService eventService,
            int maxSlots = DEFAULT_MAX_SLOTS,
            int autoSaveSlotIndex = 0,
            int quickSaveSlotIndex = 1
        )
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            MaxSlots = Mathf.Clamp(maxSlots, 1, 999);
            AutoSaveSlotIndex = Mathf.Clamp(autoSaveSlotIndex, 0, MaxSlots - 1);
            QuickSaveSlotIndex = Mathf.Clamp(quickSaveSlotIndex, 0, MaxSlots - 1);
            _slotCache = new Dictionary<int, AsakiSaveSlot>();
        }

        // 无参构造函数供框架使用（配合Init方法）
        public AsakiSaveSlotManager()
        {
            _slotCache = new Dictionary<int, AsakiSaveSlot>();
        }

        public void Init(IAsakiSaveService saveService, IAsakiEventService eventService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            // 尝试从 SaveService 获取配置
            if (saveService is AsakiSaveService asakiSaveService && asakiSaveService.Config != null)
            {
                var config = asakiSaveService.Config;
                MaxSlots = config.MaxSlots;
                AutoSaveSlotIndex = config.AutoSaveSlotIndex;
                QuickSaveSlotIndex = config.QuickSaveSlotIndex;
            }
            else
            {
                MaxSlots = DEFAULT_MAX_SLOTS;
                AutoSaveSlotIndex = 0;
                QuickSaveSlotIndex = 1;
            }
        }

        public void OnInit()
        {
            // 使用 SaveService 的路径，确保与保存服务一致
            _rootPath =
                _saveService?.SaveDirectoryPath
                ?? Path.Combine(Application.persistentDataPath, "Saves");
            _backupPath = Path.Combine(_rootPath, BACKUP_DIR_NAME);

            // 确保目录存在
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
            if (!Directory.Exists(_backupPath))
                Directory.CreateDirectory(_backupPath);

            // 初始化槽位缓存
            RefreshSlotsAsync().Forget();
            ALog.Info(
                $"[AsakiSaveSlotManager] Initialized with {MaxSlots} max slots, AutoSave: {AutoSaveSlotIndex}, QuickSave: {QuickSaveSlotIndex}, Path: {_rootPath}"
            );
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            _slotCache.Clear();
        }

        /// <inheritdoc />
        public IReadOnlyList<IAsakiSaveSlot> GetAllSlots()
        {
            var result = new List<IAsakiSaveSlot>(MaxSlots);
            for (int i = 0; i < MaxSlots; i++)
            {
                result.Add(GetOrCreateSlotInfo(i));
            }
            return result;
        }

        /// <inheritdoc />
        public IReadOnlyList<IAsakiSaveSlot> GetOccupiedSlots()
        {
            return _slotCache
                .Values.Where(s => s.Status == AsakiSaveSlotStatus.Occupied)
                .OrderByDescending(s => s.LastSaveTime)
                .Cast<IAsakiSaveSlot>()
                .ToList();
        }

        /// <inheritdoc />
        public int GetFirstEmptySlot()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (IsSlotEmpty(i))
                    return i;
            }
            return -1;
        }

        /// <inheritdoc />
        public int FindBestSlotForSave()
        {
            // 优先找空槽位
            var emptySlot = GetFirstEmptySlot();
            if (emptySlot >= 0)
                return emptySlot;

            // 其次找最早的存档（排除锁定和特殊槽位）
            var oldestSlot = _slotCache
                .Values.Where(s => s.Status == AsakiSaveSlotStatus.Occupied)
                .Where(s => s.SlotId != AutoSaveSlotIndex && s.SlotId != QuickSaveSlotIndex)
                .Where(s => s.Status != AsakiSaveSlotStatus.Locked)
                .OrderBy(s => s.LastSaveTime)
                .FirstOrDefault();

            return oldestSlot?.SlotId ?? -1;
        }

        /// <inheritdoc />
        public IAsakiSaveSlot GetSlotInfo(int slotId)
        {
            if (slotId < 0 || slotId >= MaxSlots)
                return null;

            return GetOrCreateSlotInfo(slotId);
        }

        /// <inheritdoc />
        public bool IsSlotEmpty(int slotId)
        {
            var slot = GetSlotInfo(slotId);
            return slot?.IsEmpty ?? true;
        }

        /// <inheritdoc />
        public bool IsSlotValid(int slotId)
        {
            var slot = GetSlotInfo(slotId);
            return slot?.IsValid ?? false;
        }

        /// <inheritdoc />
        public int GetUsedSlotCount()
        {
            return _slotCache.Values.Count(s => s.Status == AsakiSaveSlotStatus.Occupied);
        }

        /// <inheritdoc />
        public int GetRemainingSlotCount()
        {
            return MaxSlots - GetUsedSlotCount();
        }

        /// <inheritdoc />
        public bool HasEmptySlot()
        {
            return GetRemainingSlotCount() > 0;
        }

        /// <inheritdoc />
        public bool LockSlot(int slotId)
        {
            if (slotId < 0 || slotId >= MaxSlots)
                return false;

            var slotDir = GetSlotDir(slotId);
            var lockFile = Path.Combine(slotDir, ".locked");

            try
            {
                File.WriteAllText(lockFile, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                if (_slotCache.TryGetValue(slotId, out var slot))
                {
                    slot.Status = AsakiSaveSlotStatus.Locked;
                }
                return true;
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiSaveSlotManager] Failed to lock slot {slotId}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool UnlockSlot(int slotId)
        {
            if (slotId < 0 || slotId >= MaxSlots)
                return false;

            var slotDir = GetSlotDir(slotId);
            var lockFile = Path.Combine(slotDir, ".locked");

            try
            {
                if (File.Exists(lockFile))
                    File.Delete(lockFile);

                // 刷新槽位状态
                RefreshSlotAsync(slotId).Forget();
                return true;
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiSaveSlotManager] Failed to unlock slot {slotId}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async UniTask RefreshSlotsAsync(CancellationToken token = default)
        {
            _slotCache.Clear();

            for (int i = 0; i < MaxSlots; i++)
            {
                await RefreshSlotAsync(i, token);
            }

            ALog.Info($"[AsakiSaveSlotManager] Refreshed {_slotCache.Count} slots");
        }

        /// <inheritdoc />
        public async UniTask<IAsakiSaveSlot> CreateSaveAsync<TData>(
            string saveName,
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable
        {
            var slotId = FindBestSlotForSave();
            if (slotId < 0)
            {
                throw new InvalidOperationException("No available save slots");
            }

            return await OverwriteSaveAsync(slotId, saveName, data, token);
        }

        /// <inheritdoc />
        public async UniTask<IAsakiSaveSlot> OverwriteSaveAsync<TData>(
            int slotId,
            string saveName,
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable
        {
            if (slotId < 0 || slotId >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotId));

            // 检查槽位是否被锁定
            if (IsSlotLocked(slotId))
            {
                throw new InvalidOperationException(
                    $"Slot {slotId} is locked and cannot be overwritten"
                );
            }

            var slot = GetOrCreateSlotInfo(slotId);
            slot.SlotId = slotId;
            slot.SaveName = saveName ?? $"存档 {slotId + 1}";
            slot.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            slot.LastModifyTime = slot.LastSaveTime;
            slot.Status = AsakiSaveSlotStatus.Occupied;
            slot.GameVersion = Application.version;

            // 保存元数据和游戏数据
            await SaveSlotDataAsync(slotId, slot, data, token);

            // 更新缓存
            _slotCache[slotId] = slot;

            _eventService.Publish(new AsakiSaveSuccessEvent { Filename = $"Slot_{slotId}" });

            ALog.Info($"[AsakiSaveSlotManager] Saved to slot {slotId}: {saveName}");

            return slot;
        }

        /// <inheritdoc />
        public async UniTask<(IAsakiSaveSlot Slot, TData Data)> LoadSaveAsync<TData>(
            int slotId,
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new()
        {
            if (!IsSlotValid(slotId))
            {
                throw new FileNotFoundException($"Slot {slotId} not found or invalid");
            }

            var (slot, data) = await LoadSlotDataAsync<TData>(slotId, token);

            ALog.Info($"[AsakiSaveSlotManager] Loaded from slot {slotId}: {slot.SaveName}");

            return (slot, data);
        }

        /// <inheritdoc />
        public async UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadLatestSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new()
        {
            var latestSlot = GetOccupiedSlots().FirstOrDefault();
            if (latestSlot == null)
                return null;

            var result = await LoadSaveAsync<TData>(latestSlot.SlotId, token);
            return result;
        }

        /// <inheritdoc />
        public bool DeleteSave(int slotId)
        {
            if (slotId < 0 || slotId >= MaxSlots)
                return false;

            var slotDir = GetSlotDir(slotId);
            if (!Directory.Exists(slotDir))
                return false;

            try
            {
                Directory.Delete(slotDir, true);
                _slotCache.Remove(slotId);

                ALog.Info($"[AsakiSaveSlotManager] Deleted slot {slotId}");
                return true;
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiSaveSlotManager] Failed to delete slot {slotId}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async UniTask<IAsakiSaveSlot> CopySaveAsync(
            int sourceSlotId,
            int targetSlotId = -1,
            CancellationToken token = default
        )
        {
            if (!IsSlotValid(sourceSlotId))
                throw new FileNotFoundException($"Source slot {sourceSlotId} not found");

            if (targetSlotId < 0)
            {
                targetSlotId = GetFirstEmptySlot();
                if (targetSlotId < 0)
                    throw new InvalidOperationException("No empty slot available for copy");
            }

            if (targetSlotId < 0 || targetSlotId >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(targetSlotId));

            var sourceDir = GetSlotDir(sourceSlotId);
            var targetDir = GetSlotDir(targetSlotId);

            // 确保目标目录存在
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            // 复制所有文件
            var files = Directory.GetFiles(sourceDir);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // 更新元数据
            await RefreshSlotAsync(targetSlotId, token);
            if (_slotCache.TryGetValue(targetSlotId, out var slot))
            {
                slot.SaveName = $"{slot.SaveName} (复制)";
                slot.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await SaveSlotMetaAsync(targetSlotId, slot, token);
            }

            ALog.Info($"[AsakiSaveSlotManager] Copied slot {sourceSlotId} to {targetSlotId}");

            return GetSlotInfo(targetSlotId);
        }

        /// <inheritdoc />
        public UniTask<IAsakiSaveSlot> CreateBackupAsync(
            int slotId,
            string backupName = null,
            CancellationToken token = default
        )
        {
            if (!IsSlotValid(slotId))
                throw new FileNotFoundException($"Slot {slotId} not found");

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var backupDir = Path.Combine(_backupPath, $"Slot_{slotId}_Backup_{timestamp}");

            Directory.CreateDirectory(backupDir);

            var sourceDir = GetSlotDir(slotId);
            var files = Directory.GetFiles(sourceDir);
            foreach (var file in files)
            {
                var destFile = Path.Combine(backupDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // 创建备份信息文件
            var backupInfo = new AsakiSaveSlot
            {
                SlotId = slotId,
                SaveName = backupName ?? $"备份 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                LastSaveTime = timestamp,
                Status = AsakiSaveSlotStatus.Occupied,
            };

            ALog.Info($"[AsakiSaveSlotManager] Created backup for slot {slotId} at {backupDir}");

            return UniTask.FromResult<IAsakiSaveSlot>(backupInfo);
        }

        /// <inheritdoc />
        public UniTask<IAsakiSaveSlot> RestoreFromBackupAsync(
            int backupSlotId,
            int targetSlotId = -1,
            CancellationToken token = default
        )
        {
            // 简化实现：targetSlotId 作为备份索引处理
            // 实际实现可能需要更复杂的备份管理
            throw new NotImplementedException(
                "Restore from backup requires backup indexing system"
            );
        }

        /// <inheritdoc />
        public async UniTask<IAsakiSaveSlot> AutoSaveAsync<TData>(
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable
        {
            var slot = GetOrCreateSlotInfo(AutoSaveSlotIndex);
            slot.SaveName = $"自动保存 - {DateTime.Now:HH:mm:ss}";
            slot.ProgressPercent = 0; // 由调用方更新

            return await OverwriteSaveAsync(AutoSaveSlotIndex, slot.SaveName, data, token);
        }

        /// <inheritdoc />
        public async UniTask<IAsakiSaveSlot> QuickSaveAsync<TData>(
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable
        {
            var slot = GetOrCreateSlotInfo(QuickSaveSlotIndex);
            slot.SaveName = $"快速保存 - {DateTime.Now:HH:mm:ss}";

            return await OverwriteSaveAsync(QuickSaveSlotIndex, slot.SaveName, data, token);
        }

        /// <inheritdoc />
        public async UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadAutoSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new()
        {
            if (!IsSlotValid(AutoSaveSlotIndex))
                return null;

            return await LoadSaveAsync<TData>(AutoSaveSlotIndex, token);
        }

        /// <inheritdoc />
        public async UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadQuickSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new()
        {
            if (!IsSlotValid(QuickSaveSlotIndex))
                return null;

            return await LoadSaveAsync<TData>(QuickSaveSlotIndex, token);
        }

        // =========================================================
        // 私有方法
        // =========================================================

        private string GetSlotDir(int slotId)
        {
            return Path.Combine(_rootPath, $"Slot_{slotId}");
        }

        private string GetMetaPath(int slotId)
        {
            return Path.Combine(GetSlotDir(slotId), META_FILE_NAME);
        }

        private string GetDataPath(int slotId)
        {
            return Path.Combine(GetSlotDir(slotId), DATA_FILE_NAME);
        }

        private AsakiSaveSlot GetOrCreateSlotInfo(int slotId)
        {
            if (_slotCache.TryGetValue(slotId, out var cached))
                return cached;

            var slot = new AsakiSaveSlot
            {
                SlotId = slotId,
                Status = AsakiSaveSlotStatus.Empty,
                SaveName = $"存档 {slotId + 1}",
            };

            return slot;
        }

        private async UniTask RefreshSlotAsync(int slotId, CancellationToken token = default)
        {
            var slotDir = GetSlotDir(slotId);
            var metaPath = GetMetaPath(slotId);
            var dataPath = GetDataPath(slotId);
            var lockFile = Path.Combine(slotDir, ".locked");

            if (!Directory.Exists(slotDir) || !File.Exists(dataPath))
            {
                _slotCache[slotId] = new AsakiSaveSlot
                {
                    SlotId = slotId,
                    Status = AsakiSaveSlotStatus.Empty,
                };
                return;
            }

            // 检查锁定状态
            if (File.Exists(lockFile))
            {
                _slotCache[slotId] = new AsakiSaveSlot
                {
                    SlotId = slotId,
                    Status = AsakiSaveSlotStatus.Locked,
                };
                return;
            }

            // 尝试读取元数据
            if (File.Exists(metaPath))
            {
                try
                {
                    var metaText = await File.ReadAllTextAsync(metaPath, token);
                    var slot = new AsakiSaveSlot();
                    var reader = AsakiJsonReader.FromJson(metaText);
                    slot.Deserialize(reader);
                    slot.SlotId = slotId;
                    slot.Status = AsakiSaveSlotStatus.Occupied;
                    slot.FileSize = new FileInfo(dataPath).Length;

                    _slotCache[slotId] = slot;
                    return;
                }
                catch (Exception ex)
                {
                    ALog.Warn(
                        $"[AsakiSaveSlotManager] Failed to read meta for slot {slotId}: {ex.Message}"
                    );
                }
            }

            // 尝试从二进制数据恢复基本信息
            try
            {
                var fileInfo = new FileInfo(dataPath);
                var unixMs = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeMilliseconds();
                _slotCache[slotId] = new AsakiSaveSlot
                {
                    SlotId = slotId,
                    Status = AsakiSaveSlotStatus.Occupied,
                    LastSaveTime = unixMs,
                    LastModifyTime = unixMs,
                    FileSize = fileInfo.Length,
                    SaveName = $"存档 {slotId + 1} (未知)",
                };
            }
            catch
            {
                _slotCache[slotId] = new AsakiSaveSlot
                {
                    SlotId = slotId,
                    Status = AsakiSaveSlotStatus.Corrupted,
                };
            }
        }

        private bool IsSlotLocked(int slotId)
        {
            var lockFile = Path.Combine(GetSlotDir(slotId), ".locked");
            return File.Exists(lockFile);
        }

        private async UniTask SaveSlotDataAsync<TData>(
            int slotId,
            AsakiSaveSlot slot,
            TData data,
            CancellationToken token
        )
            where TData : IAsakiSavable
        {
            var slotDir = GetSlotDir(slotId);
            if (!Directory.Exists(slotDir))
                Directory.CreateDirectory(slotDir);

            // 保存元数据
            await SaveSlotMetaAsync(slotId, slot, token);

            // 保存游戏数据（复用现有服务）
            // 注意：这里我们绕过 SaveSlotAsync，直接保存数据部分
            var dataPath = GetDataPath(slotId);
            byte[] dataBuffer;
            using (var ms = new MemoryStream())
            {
                var writer = new AsakiBinaryWriter(ms);
                data.Serialize(writer);
                dataBuffer = ms.ToArray();
            }

#if ASAKI_USE_UNITASK
            await UniTask.SwitchToThreadPool();
#endif
            await File.WriteAllBytesAsync(dataPath, dataBuffer, token);
#if ASAKI_USE_UNITASK
            await UniTask.SwitchToMainThread();
#endif

            // 更新文件大小
            slot.FileSize = dataBuffer.Length;
        }

        private async UniTask SaveSlotMetaAsync(
            int slotId,
            AsakiSaveSlot slot,
            CancellationToken token
        )
        {
            var metaPath = GetMetaPath(slotId);

            var sb = AsakiStringBuilderPool.Rent();
            try
            {
                var writer = new AsakiJsonWriter(sb);
                slot.Serialize(writer);
                await File.WriteAllTextAsync(metaPath, writer.GetResult(), token);
            }
            finally
            {
                AsakiStringBuilderPool.Return(sb);
            }
        }

        private async UniTask<(AsakiSaveSlot Slot, TData Data)> LoadSlotDataAsync<TData>(
            int slotId,
            CancellationToken token
        )
            where TData : IAsakiSavable, new()
        {
            var dataPath = GetDataPath(slotId);
            var metaPath = GetMetaPath(slotId);

            // 读取元数据
            AsakiSaveSlot slot = null;
            if (File.Exists(metaPath))
            {
                var metaText = await File.ReadAllTextAsync(metaPath, token);
                slot = new AsakiSaveSlot();
                var reader = AsakiJsonReader.FromJson(metaText);
                slot.Deserialize(reader);
                slot.SlotId = slotId;
            }

            // 读取游戏数据
            byte[] dataBytes = await File.ReadAllBytesAsync(dataPath, token);
            var data = new TData();

            await UniTask.SwitchToMainThread();

            using (var ms = new MemoryStream(dataBytes))
            {
                var reader = new AsakiBinaryReader(ms);
                data.Deserialize(reader);
            }

            return (
                slot ?? new AsakiSaveSlot { SlotId = slotId, SaveName = $"存档 {slotId + 1}" },
                data
            );
        }
    }
}
