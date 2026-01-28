using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Serialization
{
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
            CancellationToken token = default(CancellationToken)
        )
            where TMeta : IAsakiSlotMeta
            where TData : IAsakiSavable;

        /// <summary>
        /// 从指定的存档槽位异步加载数据。
        /// </summary>
        /// <typeparam name="TMeta">存档元数据类型，必须实现IAsakiSlotMeta接口。</typeparam>
        /// <typeparam name="TData">存档数据类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="slotId">存档槽位的ID。</param>
        /// <param name="token">用于取消加载操作的取消令牌。</param>
        /// <returns>包含加载的元数据和游戏数据的UniTask对象。</returns>
        /// <exception cref="FileNotFoundException">当指定的存档槽位不存在时抛出。</exception>
        /// <remarks>
        /// 此方法会异步读取存档文件，并将数据反序列化为指定的类型。
        /// 加载过程中会并行读取元数据和游戏数据，以提高性能。
        /// </remarks>
        UniTask<(TMeta Meta, TData Data)> LoadSlotAsync<TMeta, TData>(
            int slotId,
            CancellationToken token = default(CancellationToken)
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
        /// 删除指定的存档槽位。
        /// </summary>
        /// <param name="slotId">要删除的存档槽位ID。</param>
        /// <returns>如果删除成功返回true，否则返回false。</returns>
        /// <remarks>
        /// 此方法会删除指定槽位的所有存档文件和目录。
        /// </remarks>
        bool DeleteSlot(int slotId);

        /// <summary>
        /// 检查指定的存档槽位是否存在。
        /// </summary>
        /// <param name="slotId">要检查的存档槽位ID。</param>
        /// <returns>如果存档槽位存在返回true，否则返回false。</returns>
        /// <remarks>
        /// 此方法通过检查存档数据文件是否存在来判断槽位是否存在。
        /// </remarks>
        bool SlotExists(int slotId);
    }
}
