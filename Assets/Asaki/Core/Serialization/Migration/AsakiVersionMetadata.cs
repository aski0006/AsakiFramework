namespace Asaki.Core.Serialization.Migration
{
    /// <summary>
    /// 表示序列化数据的版本元数据。
    /// </summary>
    /// <remarks>
    /// 此类封装了数据的版本信息和类型信息，
    /// 用于在反序列化时进行版本检查和迁移。
    /// </remarks>
    public class AsakiVersionMetadata
    {
        /// <summary>
        /// 数据的版本号。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 数据类型的完整名称（包括命名空间）。
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 创建一个新的版本元数据实例。
        /// </summary>
        /// <param name="typeName">数据类型的完整名称。</param>
        /// <param name="version">版本号。</param>
        public AsakiVersionMetadata(string typeName, int version)
        {
            TypeName = typeName;
            Version = version;
        }

        /// <summary>
        /// 检查当前版本是否与目标版本匹配。
        /// </summary>
        /// <param name="targetVersion">目标版本号。</param>
        /// <returns>如果版本匹配返回true，否则返回false。</returns>
        public bool IsVersionMatch(int targetVersion)
        {
            return Version == targetVersion;
        }

        /// <summary>
        /// 检查是否需要迁移到目标版本。
        /// </summary>
        /// <param name="targetVersion">目标版本号。</param>
        /// <returns>如果需要迁移返回true，否则返回false。</returns>
        public bool RequiresMigration(int targetVersion)
        {
            return Version != targetVersion;
        }

        public override string ToString()
        {
            return $"{TypeName} v{Version}";
        }
    }
}
