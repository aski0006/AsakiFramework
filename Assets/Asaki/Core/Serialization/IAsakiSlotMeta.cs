namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 定义存档槽位元数据的接口，包含存档的基本信息。
    /// </summary>
    /// <remarks>
    /// 此接口继承自IAsakiSavable，因此可以被序列化和反序列化。
    /// 它定义了存档槽位的基本属性，如ID、最后保存时间和存档名称。
    /// </remarks>
    public interface IAsakiSlotMeta : IAsakiSavable
    {
        /// <summary>
        /// 获取或设置存档槽位的ID。
        /// </summary>
        /// <value>存档槽位的唯一标识符。</value>
        int SlotId { get; set; }

        /// <summary>
        /// 获取或设置存档的最后保存时间。
        /// </summary>
        /// <value>最后保存时间的Unix时间戳（秒）。</value>
        long LastSaveTime { get; set; }

        /// <summary>
        /// 获取或设置存档的名称。
        /// </summary>
        /// <value>存档的用户可读名称。</value>
        string SaveName { get; set; }
    }
}
