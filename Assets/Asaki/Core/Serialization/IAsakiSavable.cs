namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 定义可序列化对象的核心接口，所有需要被Asaki序列化系统处理的类都必须实现此接口。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类可以被序列化为不同格式（如二进制、JSON等），并支持从序列化数据中反序列化。
    /// 该接口定义了序列化和反序列化的核心方法，是Asaki序列化系统的基础。
    /// </remarks>
    public interface IAsakiSavable
    {
        /// <summary>
        /// 将对象的数据序列化到指定的写入器中。
        /// </summary>
        /// <param name="writer">用于写入序列化数据的IAsakiWriter实例。</param>
        /// <remarks>
        /// 在实现此方法时，应按照特定顺序写入对象的所有需要持久化的字段和属性。
        /// 写入顺序必须与Deserialize方法中的读取顺序完全一致。
        /// </remarks>
        void Serialize(IAsakiWriter writer);

        /// <summary>
        /// 从指定的读取器中反序列化数据并恢复对象状态。
        /// </summary>
        /// <param name="reader">用于读取序列化数据的IAsakiReader实例。</param>
        /// <remarks>
        /// 在实现此方法时，应按照与Serialize方法完全相同的顺序读取数据。
        /// 反序列化顺序必须与序列化顺序保持一致，否则会导致数据错误。
        /// </remarks>
        void Deserialize(IAsakiReader reader);
    }
}
