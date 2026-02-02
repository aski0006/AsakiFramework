using System.Collections.Generic;

namespace Asaki.Core.Serialization.Migration
{
    /// <summary>
    /// 定义迁移注册表接口，用于管理所有数据迁移。
    /// </summary>
    /// <remarks>
    /// 迁移注册表维护了所有类型的所有版本迁移路径。
    /// 它负责查找和执行从一个版本到另一个版本所需的迁移链。
    /// </remarks>
    public interface IAsakiMigrationRegistry
    {
        /// <summary>
        /// 注册一个迁移。
        /// </summary>
        /// <param name="migration">要注册的迁移实例。</param>
        /// <remarks>
        /// 注册后，迁移系统将能够自动应用此迁移。
        /// 同一类型的同一版本对可以注册多个迁移，它们将按注册顺序执行。
        /// </remarks>
        void RegisterMigration(IAsakiMigration migration);

        /// <summary>
        /// 查找从指定版本迁移到目标版本的迁移路径。
        /// </summary>
        /// <param name="typeName">数据类型的完整名称。</param>
        /// <param name="fromVersion">源版本号。</param>
        /// <param name="toVersion">目标版本号。</param>
        /// <returns>
        /// 迁移路径（按顺序执行的迁移列表），如果找不到有效路径则返回null。
        /// </returns>
        /// <remarks>
        /// 此方法会自动查找从fromVersion到toVersion的最短迁移路径。
        /// 例如，如果存在 1->2, 2->3, 1->3 的迁移，查找1到3时会优先选择直接路径1->3。
        /// </remarks>
        List<IAsakiMigration> FindMigrationPath(string typeName, int fromVersion, int toVersion);

        /// <summary>
        /// 检查是否存在从指定版本到目标版本的迁移路径。
        /// </summary>
        /// <param name="typeName">数据类型的完整名称。</param>
        /// <param name="fromVersion">源版本号。</param>
        /// <param name="toVersion">目标版本号。</param>
        /// <returns>如果存在有效的迁移路径返回true，否则返回false。</returns>
        bool HasMigrationPath(string typeName, int fromVersion, int toVersion);

        /// <summary>
        /// 获取指定类型的所有已注册迁移。
        /// </summary>
        /// <param name="typeName">数据类型的完整名称。</param>
        /// <returns>该类型的所有迁移列表，如果没有则返回空列表。</returns>
        List<IAsakiMigration> GetMigrations(string typeName);
    }
}
