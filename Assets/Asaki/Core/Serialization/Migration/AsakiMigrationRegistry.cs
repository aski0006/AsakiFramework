using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Logging;

namespace Asaki.Core.Serialization.Migration
{
    /// <summary>
    /// 迁移注册表的默认实现。
    /// </summary>
    /// <remarks>
    /// 此实现使用图搜索算法（BFS）来查找最短迁移路径。
    /// 支持自动链式迁移，例如 v1->v2->v3->v4。
    /// </remarks>
    public class AsakiMigrationRegistry : IAsakiMigrationRegistry
    {
        // 存储结构: TypeName -> (FromVersion -> List<Migration>)
        private readonly Dictionary<string, Dictionary<int, List<IAsakiMigration>>> _migrations;

        public AsakiMigrationRegistry()
        {
            _migrations = new Dictionary<string, Dictionary<int, List<IAsakiMigration>>>();
        }

        /// <summary>
        /// 注册一个迁移。
        /// </summary>
        public void RegisterMigration(IAsakiMigration migration)
        {
            if (migration == null)
                throw new ArgumentNullException(nameof(migration));

            string typeName = migration.TypeName;

            if (!_migrations.ContainsKey(typeName))
            {
                _migrations[typeName] = new Dictionary<int, List<IAsakiMigration>>();
            }

            var typeDict = _migrations[typeName];
            int fromVersion = migration.FromVersion;

            if (!typeDict.ContainsKey(fromVersion))
            {
                typeDict[fromVersion] = new List<IAsakiMigration>();
            }

            typeDict[fromVersion].Add(migration);

            ALog.Info(
                $"[AsakiMigration] Registered migration: {typeName} v{migration.FromVersion} -> v{migration.ToVersion}"
            );
        }

        /// <summary>
        /// 查找从指定版本到目标版本的迁移路径（使用BFS算法）。
        /// </summary>
        public List<IAsakiMigration> FindMigrationPath(
            string typeName,
            int fromVersion,
            int toVersion
        )
        {
            if (fromVersion == toVersion)
                return new List<IAsakiMigration>(); // 版本相同，无需迁移

            if (!_migrations.ContainsKey(typeName))
                return null; // 没有该类型的迁移

            // BFS 查找最短路径
            var queue = new Queue<(int version, List<IAsakiMigration> path)>();
            var visited = new HashSet<int>();

            queue.Enqueue((fromVersion, new List<IAsakiMigration>()));
            visited.Add(fromVersion);

            while (queue.Count > 0)
            {
                var (currentVersion, currentPath) = queue.Dequeue();

                // 检查从当前版本出发的所有迁移
                if (_migrations[typeName].TryGetValue(currentVersion, out var migrations))
                {
                    foreach (var migration in migrations)
                    {
                        int nextVersion = migration.ToVersion;

                        // 找到目标版本
                        if (nextVersion == toVersion)
                        {
                            var result = new List<IAsakiMigration>(currentPath) { migration };
                            ALog.Info(
                                $"[AsakiMigration] Found migration path for {typeName}: "
                                    + string.Join(
                                        " -> ",
                                        result.Select(m => $"v{m.FromVersion}->v{m.ToVersion}")
                                    )
                            );
                            return result;
                        }

                        // 继续搜索
                        if (!visited.Contains(nextVersion))
                        {
                            visited.Add(nextVersion);
                            var newPath = new List<IAsakiMigration>(currentPath) { migration };
                            queue.Enqueue((nextVersion, newPath));
                        }
                    }
                }
            }

            ALog.Warn(
                $"[AsakiMigration] No migration path found for {typeName} from v{fromVersion} to v{toVersion}"
            );
            return null; // 找不到路径
        }

        /// <summary>
        /// 检查是否存在迁移路径。
        /// </summary>
        public bool HasMigrationPath(string typeName, int fromVersion, int toVersion)
        {
            return FindMigrationPath(typeName, fromVersion, toVersion) != null;
        }

        /// <summary>
        /// 获取指定类型的所有已注册迁移。
        /// </summary>
        public List<IAsakiMigration> GetMigrations(string typeName)
        {
            if (!_migrations.ContainsKey(typeName))
                return new List<IAsakiMigration>();

            return _migrations[typeName].Values.SelectMany(list => list).ToList();
        }
    }
}
