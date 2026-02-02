using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记一个类为数据迁移类。
    /// </summary>
    /// <remarks>
    /// 使用此特性标记的类会被自动注册到迁移系统中。
    /// 被标记的类必须实现IAsakiMigration或IAsakiMigration&lt;T&gt;接口。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AsakiMigrationAttribute : Attribute
    {
        /// <summary>
        /// 迁移适用的数据类型。
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// 源版本号。
        /// </summary>
        public int FromVersion { get; }

        /// <summary>
        /// 目标版本号。
        /// </summary>
        public int ToVersion { get; }

        /// <summary>
        /// 创建一个新的迁移特性实例。
        /// </summary>
        /// <param name="dataType">要迁移的数据类型。</param>
        /// <param name="fromVersion">源版本号。</param>
        /// <param name="toVersion">目标版本号。</param>
        public AsakiMigrationAttribute(Type dataType, int fromVersion, int toVersion)
        {
            DataType = dataType;
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }
    }
}
