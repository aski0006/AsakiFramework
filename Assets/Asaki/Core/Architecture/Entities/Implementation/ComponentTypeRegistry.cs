using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 组件类型注册表 - 管理组件类型到 ID 的映射
    /// 线程安全，带 TypeId 上限保护
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static readonly ConcurrentDictionary<Type, int> _typeIds = new();
        private static readonly ConcurrentDictionary<int, Type> _idToTypes = new();
        private static int _nextTypeId = 0;

        /// <summary>
        /// 获取组件类型的 ID
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>类型 ID</returns>
        /// <exception cref="InvalidOperationException">当 TypeId 超过最大限制时抛出</exception>
        public static int GetTypeId<T>()
            where T : class, IEntityComponent
        {
            return GetTypeId(typeof(T));
        }

        /// <summary>
        /// 获取组件类型的 ID
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns>类型 ID</returns>
        /// <exception cref="InvalidOperationException">当 TypeId 超过最大限制时抛出</exception>
        public static int GetTypeId(Type type)
        {
            if (_typeIds.TryGetValue(type, out int id))
            {
                return id;
            }

            // 在分配新 ID 之前检查是否超过上限
            int currentId = Volatile.Read(ref _nextTypeId);
            if (currentId >= AsakiArchitectureConstants.MaxComponentTypeId)
            {
                throw new InvalidOperationException(
                    $"Component TypeId has reached maximum limit ({AsakiArchitectureConstants.MaxComponentTypeId}). "
                        + $"Cannot register more than {AsakiArchitectureConstants.MaxComponentTypeId} component types."
                );
            }

            int newId = Interlocked.Increment(ref _nextTypeId) - 1;

            // 双重检查：防止并发情况下超过上限
            if (newId >= AsakiArchitectureConstants.MaxComponentTypeId)
            {
                throw new InvalidOperationException(
                    $"Component TypeId {newId} exceeds maximum limit ({AsakiArchitectureConstants.MaxComponentTypeId}). "
                        + "Consider reducing the number of component types."
                );
            }

            if (_typeIds.TryAdd(type, newId))
            {
                _idToTypes[newId] = type;
                return newId;
            }

            return _typeIds[type];
        }

        /// <summary>
        /// 通过 ID 获取类型
        /// </summary>
        /// <param name="typeId">类型 ID</param>
        /// <returns>类型，不存在则返回 null</returns>
        public static Type GetTypeById(int typeId)
        {
            if (typeId < 0 || typeId >= AsakiArchitectureConstants.MaxComponentTypeId)
                return null;

            _idToTypes.TryGetValue(typeId, out var type);
            return type;
        }

        /// <summary>
        /// 获取已注册的类型数量
        /// </summary>
        public static int RegisteredTypeCount => _typeIds.Count;

        /// <summary>
        /// 获取下一个将要分配的 TypeId（用于测试和调试）
        /// </summary>
        public static int GetNextTypeId() => Volatile.Read(ref _nextTypeId);

        /// <summary>
        /// 清除所有注册的类型（主要用于测试）
        /// </summary>
        internal static void Clear()
        {
            _typeIds.Clear();
            _idToTypes.Clear();
            Volatile.Write(ref _nextTypeId, 0);
        }
    }
}
