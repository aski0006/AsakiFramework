namespace Asaki.Core.Serialization.Migration
{
    /// <summary>
    /// 定义数据迁移的核心接口。
    /// </summary>
    /// <remarks>
    /// 实现此接口来定义从一个版本到另一个版本的数据迁移逻辑。
    /// 迁移系统会在反序列化时自动检测版本不匹配并应用相应的迁移。
    /// </remarks>
    public interface IAsakiMigration
    {
        /// <summary>
        /// 获取此迁移的源版本（迁移前的版本）。
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// 获取此迁移的目标版本（迁移后的版本）。
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        /// 获取此迁移适用的数据类型的完整名称。
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// 执行数据迁移。
        /// </summary>
        /// <param name="reader">包含旧版本数据的读取器。</param>
        /// <param name="writer">用于写入新版本数据的写入器。</param>
        /// <remarks>
        /// 实现此方法时，应从reader读取旧版本的数据，
        /// 然后将转换后的数据写入writer。
        /// </remarks>
        void Migrate(IAsakiReader reader, IAsakiWriter writer);
    }

    /// <summary>
    /// 强类型数据迁移接口。
    /// </summary>
    /// <typeparam name="TData">要迁移的数据类型，必须实现IAsakiSavable接口。</typeparam>
    /// <remarks>
    /// 这是一个更高级的接口，允许直接操作强类型的数据对象。
    /// 相比基础接口，此接口提供了更好的类型安全性和更简洁的API。
    /// </remarks>
    public interface IAsakiMigration<TData> : IAsakiMigration
        where TData : IAsakiSavable
    {
        /// <summary>
        /// 执行强类型数据迁移。
        /// </summary>
        /// <param name="data">要迁移的数据对象。</param>
        /// <remarks>
        /// 实现此方法以直接修改数据对象，无需手动读写序列化器。
        /// 此方法应将数据从FromVersion转换为ToVersion。
        /// </remarks>
        void Migrate(TData data);
    }
}
