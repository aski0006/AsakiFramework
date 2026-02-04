using System.Collections.Generic;
using System.IO;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 保存操作结果
    /// </summary>
    public struct AsakiSaveResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success;

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// 保存的槽位 ID
        /// </summary>
        public int SlotId;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize;

        /// <summary>
        /// 保存耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds;

        /// <summary>
        /// 成功的结果
        /// </summary>
        public static AsakiSaveResult Successful(int slotId, long fileSize = 0, long elapsedMs = 0) =>
            new AsakiSaveResult { Success = true, SlotId = slotId, FileSize = fileSize, ElapsedMilliseconds = elapsedMs };

        /// <summary>
        /// 失败的结果
        /// </summary>
        public static AsakiSaveResult Failed(string errorMessage, int slotId = -1) =>
            new AsakiSaveResult { Success = false, ErrorMessage = errorMessage, SlotId = slotId };
    }

    /// <summary>
    /// 加载操作结果
    /// </summary>
    public struct AsakiLoadResult<TMeta, TData>
        where TMeta : IAsakiSlotMeta, new()
        where TData : IAsakiSavable, new()
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success;

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// 加载的元数据
        /// </summary>
        public TMeta Meta;

        /// <summary>
        /// 加载的数据
        /// </summary>
        public TData Data;

        /// <summary>
        /// 加载耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds;

        /// <summary>
        /// 成功的结果
        /// </summary>
        public static AsakiLoadResult<TMeta, TData> Successful(TMeta meta, TData data, long elapsedMs = 0) =>
            new AsakiLoadResult<TMeta, TData> { Success = true, Meta = meta, Data = data, ElapsedMilliseconds = elapsedMs };

        /// <summary>
        /// 失败的结果
        /// </summary>
        public static AsakiLoadResult<TMeta, TData> Failed(string errorMessage) =>
            new AsakiLoadResult<TMeta, TData> { Success = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// 定义Asaki保存服务的核心接口，负责管理游戏存档的保存、加载和管理操作。
    /// </summary>
    /// <remarks>
    /// 此接口继承自IAsakiModule，是Asaki框架中的一个核心服务模块。
    /// 它提供了基于Slot的异步保存和加载功能，以及存档管理的工具方法。
    /// </remarks>
    public interface IAsakiSaveService : IAsakiModule
    {
        /// <summary>
        /// 获取默认存档目录路径
        /// </summary>
        string SaveDirectoryPath { get; }

        /// <summary>
        /// 获取支持的存档槽位数量上限
        /// </summary>
        int MaxSupportedSlots { get; }

        /// <summary>
        /// 异步保存数据到指定的存档槽位。
        /// </summary>
        /// <typeparam name="TMeta">存档元数据类型，必须实现IAsakiSlotMeta接口。</typeparam>
        /// <typeparam name="TData">存档数据类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="slotId">存档槽位的ID。</param>
        /// <param name="meta">存档的元数据，包含存档的基本信息。</param>
        /// <param name="data">要保存的游戏数据。</param>
        /// <param name="cancellationToken">用于取消保存操作的取消令牌。</param>
        /// <returns>表示异步操作的Task对象。</returns>
        /// <remarks>
        /// 此方法会自动处理存档目录的创建，并将元数据和游戏数据分别保存。
        /// 保存过程中会发布相应的事件（如保存开始、保存成功、保存失败）。
        /// </remarks>
        UniTask SaveSlotAsync<TMeta, TData>(
            int slotId,
            TMeta meta,
            TData data,
            CancellationToken cancellationToken = default
        )
            where TMeta : IAsakiSlotMeta
            where TData : IAsakiSavable;

        /// <summary>
        /// 异步保存数据并返回详细结果
        /// </summary>
        UniTask<AsakiSaveResult> SaveSlotWithResultAsync<TMeta, TData>(
            int slotId,
            TMeta meta,
            TData data,
            CancellationToken cancellationToken = default
        )
            where TMeta : IAsakiSlotMeta
            where TData : IAsakiSavable;

        /// <summary>
        /// 从指定的存档槽位异步加载数据。
        /// </summary>
        /// <typeparam name="TMeta">存档元数据类型，必须实现IAsakiSlotMeta接口。</typeparam>
        /// <typeparam name="TData">存档数据类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="slotId">存档槽位的ID。</param>
        /// <param name="cancellationToken">用于取消加载操作的取消令牌。</param>
        /// <returns>包含加载的元数据和游戏数据的UniTask对象。</returns>
        /// <exception cref="FileNotFoundException">当指定的存档槽位不存在时抛出。</exception>
        /// <remarks>
        /// 此方法会异步读取存档文件，并将数据反序列化为指定的类型。
        /// 加载过程中会并行读取元数据和游戏数据，以提高性能。
        /// </remarks>
        UniTask<(TMeta Meta, TData Data)> LoadSlotAsync<TMeta, TData>(
            int slotId,
            CancellationToken cancellationToken = default
        )
            where TMeta : IAsakiSlotMeta, new()
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 异步加载数据并返回详细结果
        /// </summary>
        UniTask<AsakiLoadResult<TMeta, TData>> LoadSlotWithResultAsync<TMeta, TData>(
            int slotId,
            CancellationToken cancellationToken = default
        )
            where TMeta : IAsakiSlotMeta, new()
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 尝试加载存档，如果不存在返回 null 而不抛出异常
        /// </summary>
        UniTask<(TMeta Meta, TData Data)?> TryLoadSlotAsync<TMeta, TData>(
            int slotId,
            CancellationToken cancellationToken = default
        )
            where TMeta : IAsakiSlotMeta, new()
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 获取所有已使用的存档槽位ID列表。
        /// </summary>
        /// <returns>已使用的存档槽位ID列表。</returns>
        /// <remarks>
        /// 此方法会扫描存档目录，返回所有存在的存档槽位ID。
        /// </remarks>
        List<int> GetUsedSlots();

        /// <summary>
        /// 获取所有存档槽位的完整信息
        /// </summary>
        IReadOnlyList<AsakiSaveSlotInfo> GetAllSlotInfos();

        /// <summary>
        /// 删除指定的存档槽位。
        /// </summary>
        /// <param name="slotId">要删除的存档槽位ID。</param>
        /// <returns>如果删除成功返回true，否则返回false。</returns>
        /// <remarks>
        /// 此方法会删除指定槽位的所有存档文件和目录。
        /// </remarks>
        bool DeleteSlot(int slotId);

        /// <summary>
        /// 批量删除多个存档槽位
        /// </summary>
        int DeleteSlots(IEnumerable<int> slotIds);

        /// <summary>
        /// 检查指定的存档槽位是否存在。
        /// </summary>
        /// <param name="slotId">要检查的存档槽位ID。</param>
        /// <returns>如果存档槽位存在返回true，否则返回false。</returns>
        /// <remarks>
        /// 此方法通过检查存档数据文件是否存在来判断槽位是否存在。
        /// </remarks>
        bool SlotExists(int slotId);

        /// <summary>
        /// 获取指定槽位的存档文件大小
        /// </summary>
        long GetSlotFileSize(int slotId);

        /// <summary>
        /// 获取指定槽位的最后修改时间
        /// </summary>
        long GetSlotLastModifiedTime(int slotId);

        /// <summary>
        /// 复制存档到另一个槽位
        /// </summary>
        UniTask<bool> CopySlotAsync(int sourceSlotId, int targetSlotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 导出存档到指定路径
        /// </summary>
        UniTask<bool> ExportSlotAsync(int slotId, string exportPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从指定路径导入存档
        /// </summary>
        UniTask<bool> ImportSlotAsync(string importPath, int targetSlotId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 存档槽位信息（简化版，用于快速查询）
    /// </summary>
    public struct AsakiSaveSlotInfo
    {
        /// <summary>
        /// 槽位 ID
        /// </summary>
        public int SlotId;

        /// <summary>
        /// 是否存在有效存档
        /// </summary>
        public bool Exists;

        /// <summary>
        /// 最后保存时间（Unix 时间戳）
        /// </summary>
        public long LastSaveTime;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize;

        /// <summary>
        /// 存档名称
        /// </summary>
        public string SaveName;
    }
}
