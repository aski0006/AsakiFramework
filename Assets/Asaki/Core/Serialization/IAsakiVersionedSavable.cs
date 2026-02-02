namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 扩展接口，为可序列化对象添加版本控制支持。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类型将支持数据版本控制和自动迁移。
    /// 版本信息会与数据一起序列化，用于反序列化时的版本检查。
    /// </remarks>
    public interface IAsakiVersionedSavable : IAsakiSavable
    {
        /// <summary>
        /// 获取当前数据的版本号。
        /// </summary>
        /// <returns>数据版本号，必须与[AsakiSave(Version = x)]中定义的版本一致。</returns>
        int GetDataVersion();
    }
}
