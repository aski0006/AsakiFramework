using System;
using System.IO;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;
using Asaki.Core.Serialization.Migration;

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// 扩展的二进制读取器，支持版本迁移功能。
    /// </summary>
    /// <remarks>
    /// 此类包装了AsakiBinaryReader，在反序列化时自动检测版本不匹配并应用迁移。
    /// 实现了IDisposable接口，确保资源正确释放。
    /// </remarks>
    public class AsakiMigrationBinaryReader : IAsakiReader, IDisposable
    {
        private readonly AsakiBinaryReader _innerReader;
        private readonly IAsakiMigrationRegistry _migrationRegistry;
        private readonly Stream _baseStream;
        private int _readVersion;
        private bool _versionRead;
        private bool _disposed;

        public AsakiMigrationBinaryReader(
            Stream stream,
            IAsakiMigrationRegistry migrationRegistry = null
        )
        {
            _baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _innerReader = new AsakiBinaryReader(stream);
            _migrationRegistry = migrationRegistry;
            _versionRead = false;
            _disposed = false;
        }

        public int ReadVersion()
        {
            _readVersion = _innerReader.ReadVersion();
            _versionRead = true;
            return _readVersion;
        }

        /// <summary>
        /// 读取对象，支持自动版本迁移。
        /// </summary>
        public T ReadObject<T>(string key, T existingObj = default(T))
            where T : IAsakiSavable, new()
        {
            // 如果数据实现了版本控制接口，检查版本
            if (typeof(IAsakiVersionedSavable).IsAssignableFrom(typeof(T)))
            {
                // 创建临时对象以获取目标版本
                var tempObj = new T();
                int targetVersion = (tempObj as IAsakiVersionedSavable)?.GetDataVersion() ?? 1;

                // 如果版本不匹配且存在迁移注册表，尝试迁移
                if (_versionRead && _readVersion != targetVersion && _migrationRegistry != null)
                {
                    return ReadObjectWithMigration<T>(
                        key,
                        _readVersion,
                        targetVersion,
                        existingObj
                    );
                }
            }

            // 正常读取
            return _innerReader.ReadObject(key, existingObj);
        }

        /// <summary>
        /// 读取对象并应用迁移。
        /// </summary>
        private T ReadObjectWithMigration<T>(
            string key,
            int dataVersion,
            int targetVersion,
            T existingObj
        )
            where T : IAsakiSavable, new()
        {
            string typeName = typeof(T).FullName;

            // 查找迁移路径
            var migrationPath = _migrationRegistry.FindMigrationPath(
                typeName,
                dataVersion,
                targetVersion
            );

            if (migrationPath == null || migrationPath.Count == 0)
            {
                ALog.Warn(
                    $"[AsakiMigration] No migration path found for {typeName} from v{dataVersion} to v{targetVersion}. "
                        + "Attempting to deserialize directly (may fail or produce incorrect data)."
                );
                return _innerReader.ReadObject(key, existingObj);
            }

            ALog.Info(
                $"[AsakiMigration] Applying {migrationPath.Count} migration(s) for {typeName}..."
            );

            try
            {
                // 读取原始数据
                var data = _innerReader.ReadObject<T>(key, existingObj);

                // 应用每个迁移
                foreach (var migration in migrationPath)
                {
                    ALog.Info(
                        $"[AsakiMigration] Applying migration: {typeName} v{migration.FromVersion} -> v{migration.ToVersion}"
                    );

                    // 如果是强类型迁移，直接在已读取的数据上应用
                    if (migration is IAsakiMigration<T> typedMigration)
                    {
                        typedMigration.Migrate(data);
                    }
                    else
                    {
                        // 低级迁移：需要序列化当前数据 -> 迁移 -> 反序列化
                        // 注意：这里使用已读取的数据，而不是重新从流中读取
                        using (var tempReadStream = new MemoryStream())
                        using (var tempWriteStream = new MemoryStream())
                        {
                            // 1. 将当前数据序列化到临时流（模拟旧版本数据）
                            var tempWriter = new AsakiBinaryWriter(tempReadStream, true);
                            data.Serialize(tempWriter);
                            tempWriter.Dispose();

                            // 2. 准备读取
                            tempReadStream.Position = 0;
                            var migrationReader = new AsakiBinaryReader(tempReadStream, true);
                            var migrationWriter = new AsakiBinaryWriter(tempWriteStream, true);

                            // 3. 执行迁移
                            migration.Migrate(migrationReader, migrationWriter);

                            // 4. 从迁移后的数据反序列化
                            tempWriteStream.Position = 0;
                            var resultReader = new AsakiBinaryReader(tempWriteStream, true);
                            data.Deserialize(resultReader);

                            migrationReader.Dispose();
                            migrationWriter.Dispose();
                            resultReader.Dispose();
                        }
                    }
                }

                ALog.Info($"[AsakiMigration] Successfully migrated {typeName} to v{targetVersion}");
                return data;
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiMigration] Migration failed for {typeName}: {ex.Message}", ex);
                throw;
            }
        }

        // 委托所有其他方法到内部reader
        public object ReadObject(string key, Type type) => _innerReader.ReadObject(key, type);

        public byte ReadByte(string key) => _innerReader.ReadByte(key);

        public int ReadInt(string key) => _innerReader.ReadInt(key);

        public long ReadLong(string key) => _innerReader.ReadLong(key);

        public float ReadFloat(string key) => _innerReader.ReadFloat(key);

        public double ReadDouble(string key) => _innerReader.ReadDouble(key);

        public string ReadString(string key) => _innerReader.ReadString(key);

        public bool ReadBool(string key) => _innerReader.ReadBool(key);

        public uint ReadUInt(string key) => _innerReader.ReadUInt(key);

        public ulong ReadULong(string key) => _innerReader.ReadULong(key);

        public UnityEngine.Vector2Int ReadVector2Int(string key) =>
            _innerReader.ReadVector2Int(key);

        public UnityEngine.Vector3Int ReadVector3Int(string key) =>
            _innerReader.ReadVector3Int(key);

        public UnityEngine.Vector2 ReadVector2(string key) => _innerReader.ReadVector2(key);

        public UnityEngine.Vector3 ReadVector3(string key) => _innerReader.ReadVector3(key);

        public UnityEngine.Vector4 ReadVector4(string key) => _innerReader.ReadVector4(key);

        public UnityEngine.Bounds ReadBounds(string key) => _innerReader.ReadBounds(key);

        public UnityEngine.Quaternion ReadQuaternion(string key) =>
            _innerReader.ReadQuaternion(key);

        public int BeginList(string key) => _innerReader.BeginList(key);

        public void EndList() => _innerReader.EndList();

        /// <summary>
        /// 释放读取器使用的资源。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _innerReader?.Dispose();
            _disposed = true;
        }
    }
}
