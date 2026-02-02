using System;
using System.IO;

namespace Asaki.Core.Serialization.Migration
{
    /// <summary>
    /// 抽象基类，简化强类型迁移的实现。
    /// </summary>
    /// <typeparam name="TData">要迁移的数据类型。</typeparam>
    /// <remarks>
    /// 继承此类可以避免手动实现低级的reader/writer迁移逻辑。
    /// 只需实现Migrate(TData)方法即可完成迁移。
    /// </remarks>
    public abstract class AsakiMigrationBase<TData> : IAsakiMigration<TData>
        where TData : IAsakiSavable, new()
    {
        /// <summary>
        /// 源版本号。
        /// </summary>
        public abstract int FromVersion { get; }

        /// <summary>
        /// 目标版本号。
        /// </summary>
        public abstract int ToVersion { get; }

        /// <summary>
        /// 数据类型的完整名称。
        /// </summary>
        public virtual string TypeName => typeof(TData).FullName;

        /// <summary>
        /// 执行强类型迁移。
        /// </summary>
        /// <param name="data">要迁移的数据对象。</param>
        public abstract void Migrate(TData data);

        /// <summary>
        /// 执行低级迁移（自动实现）。
        /// </summary>
        /// <remarks>
        /// 此方法会自动将reader中的数据反序列化为对象，
        /// 调用强类型Migrate方法，然后将结果序列化到writer。
        /// </remarks>
        public virtual void Migrate(IAsakiReader reader, IAsakiWriter writer)
        {
            // 1. 从reader反序列化旧版本数据
            var data = new TData();
            data.Deserialize(reader);

            // 2. 执行强类型迁移
            Migrate(data);

            // 3. 将迁移后的数据写入writer
            data.Serialize(writer);
        }
    }
}
