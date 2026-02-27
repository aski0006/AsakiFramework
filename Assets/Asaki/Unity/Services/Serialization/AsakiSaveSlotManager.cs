using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Asaki.Core.Broker;
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

        private IAsakiSaveService _saveService;
        private string _rootPath;
        private string _backupPath;
        private Dictionary<int, AsakiSaveSlot> _slotCache;

        /// <inheritdoc />
        public int MaxSlots { get; private set; }

        /// <inheritdoc />
        public int AutoSaveSlotIndex { get; private set; }

        /// <inheritdoc />
        public int QuickSaveSlotIndex { get; private set; }

        /// <summary>
        /// 构造函数，从 SaveService.Config 获取配置
        /// </summary>
        /// <param name="saveService">保存服务接口</param>
        /// <param name="eventService">事件服务接口（保留用于兼容性，事件由 SaveService 发布）</param>
        public AsakiSaveSlotManager(IAsakiSaveService saveService, IAsakiEventService eventService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _slotCache = new Dictionary<int, AsakiSaveSlot>();

            // 从 SaveService 获取配置
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

        /// <summary>
        /// 初始化方法，用于框架兼容性
        /// 如果构造函数已设置依赖，则跳过初始化
        /// </summary>
        /// <param name="saveService">保存服务接口</param>
        /// <param name="eventService">事件服务接口（保留用于兼容性）</param>
        public void Init(IAsakiSaveService saveService, IAsakiEventService eventService)
        {
            // 配置已在构造函数中初始化，此方法保留用于兼容性
            // 如果 _saveService 已设置，则跳过
            if (_saveService != null)
                return;

            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));

            // 从 SaveService 获取配置
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

        /// <summary>
        /// 初始化回调方法，设置路径并刷新槽位缓存
        /// </summary>
        public void OnInit()
        {
            // 使用 SaveService 的路径
            _rootPath = _saveService.SaveDirectoryPath;
            _backupPath = Path.Combine(_rootPath, BACKUP_DIR_NAME);

            // 确保备份目录存在
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
                ALog.Error(
                    $"[AsakiSaveSlotManager] Failed to lock slot {slotId}: {ex.Message}",
                    ex
                );
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
                ALog.Error(
                    $"[AsakiSaveSlotManager] Failed to unlock slot {slotId}: {ex.Message}",
                    ex
                );
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

            // 保存元数据和游戏数据（事件由 SaveService 发布）
            await SaveSlotDataAsync(slotId, slot, data, token);

            // 更新缓存
            _slotCache[slotId] = slot;

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
                ALog.Error(
                    $"[AsakiSaveSlotManager] Failed to delete slot {slotId}: {ex.Message}",
                    ex
                );
                return false;
            }
        }

        /// <summary>
        /// 复制存档到另一个槽位
        /// </summary>
        /// <param name="sourceSlotId">源槽位ID</param>
        /// <param name="targetSlotId">目标槽位ID，如果为-1则自动选择第一个空槽位</param>
        /// <param name="token">取消令牌</param>
        /// <returns>目标槽位信息</returns>
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

            // 使用 SaveService 复制
            bool success = await _saveService.CopySlotAsync(sourceSlotId, targetSlotId, token);
            if (!success)
                throw new IOException($"Failed to copy slot {sourceSlotId} to {targetSlotId}");

            // 刷新目标槽位信息
            await RefreshSlotAsync(targetSlotId, token);

            // 更新元数据，添加 "(复制)" 后缀
            if (_slotCache.TryGetValue(targetSlotId, out var slot))
            {
                slot.SaveName = $"{slot.SaveName} (复制)";
                slot.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await SaveSlotMetaAsync(targetSlotId, slot, token);
            }

            ALog.Info($"[AsakiSaveSlotManager] Copied slot {sourceSlotId} to {targetSlotId}");

            return GetSlotInfo(targetSlotId);
        }

        /// <summary>
        /// 创建存档备份
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <param name="backupName">备份名称，如果为null则自动生成</param>
        /// <param name="token">取消令牌</param>
        /// <returns>备份信息</returns>
        public async UniTask<IAsakiSaveSlot> CreateBackupAsync(
            int slotId,
            string backupName = null,
            CancellationToken token = default
        )
        {
            if (!IsSlotValid(slotId))
                throw new FileNotFoundException($"Slot {slotId} not found");

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var backupDir = Path.Combine(_backupPath, $"Slot_{slotId}_Backup_{timestamp}");

            // 使用 SaveService 导出
            bool success = await _saveService.ExportSlotAsync(slotId, backupDir, token);
            if (!success)
                throw new IOException($"Failed to create backup for slot {slotId}");

            // 创建备份信息
            var backupInfo = new AsakiSaveSlot
            {
                SlotId = slotId,
                SaveName = backupName ?? $"备份 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                LastSaveTime = timestamp,
                Status = AsakiSaveSlotStatus.Occupied,
            };

            ALog.Info($"[AsakiSaveSlotManager] Created backup for slot {slotId} at {backupDir}");

            return backupInfo;
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

        /// <summary>
        /// 获取槽位目录路径
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>槽位目录路径</returns>
        private string GetSlotDir(int slotId)
        {
            return _saveService.GetSlotDirectory(slotId);
        }

        /// <summary>
        /// 获取槽位元数据文件路径
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>元数据文件路径</returns>
        private string GetMetaPath(int slotId)
        {
            return _saveService.GetSlotMetaPath(slotId);
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

        /// <summary>
        /// 刷新单个槽位的信息
        /// 注意：元数据文件读取是 SlotManager 缓存刷新的特例，
        /// 不通过 SaveService 进行，因为缓存刷新只需要元数据而不需要游戏数据。
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <param name="token">取消令牌</param>
        private async UniTask RefreshSlotAsync(int slotId, CancellationToken token = default)
        {
            var slotDir = _saveService.GetSlotDirectory(slotId);
            var metaPath = _saveService.GetSlotMetaPath(slotId);
            var lockFile = Path.Combine(slotDir, ".locked");

            // 使用 SaveService 检查槽位存在性
            if (!_saveService.SlotExists(slotId))
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
                    slot.FileSize = _saveService.GetSlotFileSize(slotId);

                    _slotCache[slotId] = slot;
                    return;
                }
                catch (Exception ex)
                {
                    ALog.Warn(
                        $"[AsakiSaveSlotManager] Failed to read meta for slot {slotId}: {ex.Message}",
                        ex
                    );
                }
            }

            // 尝试从二进制数据恢复基本信息
            try
            {
                var fileSize = _saveService.GetSlotFileSize(slotId);
                var lastModified = _saveService.GetSlotLastModifiedTime(slotId);
                _slotCache[slotId] = new AsakiSaveSlot
                {
                    SlotId = slotId,
                    Status = AsakiSaveSlotStatus.Occupied,
                    LastSaveTime = lastModified,
                    LastModifyTime = lastModified,
                    FileSize = fileSize,
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

        /// <summary>
        /// 保存槽位数据，委托给 SaveService 处理
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <param name="slotId">槽位ID</param>
        /// <param name="slot">槽位元数据</param>
        /// <param name="data">游戏数据</param>
        /// <param name="token">取消令牌</param>
        private async UniTask SaveSlotDataAsync<TData>(
            int slotId,
            AsakiSaveSlot slot,
            TData data,
            CancellationToken token
        )
            where TData : IAsakiSavable
        {
            await _saveService.SaveSlotAsync(slotId, slot, data, token);
            slot.FileSize = _saveService.GetSlotFileSize(slotId);
        }

        /// <summary>
        /// 保存槽位元数据
        /// 注意：此方法用于 SlotManager 特有的元数据更新操作（如复制后添加后缀），
        /// 主要的保存操作应通过 SaveService.SaveSlotAsync 进行。
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <param name="slot">槽位元数据</param>
        /// <param name="token">取消令牌</param>
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

        /// <summary>
        /// 加载槽位数据，委托给 SaveService 处理
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <param name="slotId">槽位ID</param>
        /// <param name="token">取消令牌</param>
        /// <returns>槽位元数据和游戏数据的元组</returns>
        private async UniTask<(AsakiSaveSlot Slot, TData Data)> LoadSlotDataAsync<TData>(
            int slotId,
            CancellationToken token
        )
            where TData : IAsakiSavable, new()
        {
            var (slot, data) = await _saveService.LoadSlotAsync<AsakiSaveSlot, TData>(
                slotId,
                token
            );
            return (slot, data);
        }
    }
}
