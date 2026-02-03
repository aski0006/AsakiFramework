using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 组件类型注册表 - 管理组件类型到ID的映射
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<Type, int> _typeIds = new();
        private static readonly Dictionary<int, Type> _idToTypes = new();
        private static int _nextTypeId = 0;
        private static readonly object _lock = new();

        /// <summary>
        /// 获取组件类型的ID
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>类型ID</returns>
        public static int GetTypeId<T>()
            where T : class, IEntityComponent
        {
            return GetTypeId(typeof(T));
        }

        /// <summary>
        /// 获取组件类型的ID
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns>类型ID</returns>
        public static int GetTypeId(Type type)
        {
            if (!_typeIds.TryGetValue(type, out int id))
            {
                lock (_lock)
                {
                    if (!_typeIds.TryGetValue(type, out id))
                    {
                        id = _nextTypeId++;
                        _typeIds[type] = id;
                        _idToTypes[id] = type;
                    }
                }
            }
            return id;
        }

        /// <summary>
        /// 通过ID获取类型
        /// </summary>
        /// <param name="typeId">类型ID</param>
        /// <returns>类型，不存在则返回null</returns>
        public static Type GetTypeById(int typeId)
        {
            _idToTypes.TryGetValue(typeId, out var type);
            return type;
        }

        /// <summary>
        /// 获取已注册的类型数量
        /// </summary>
        public static int RegisteredTypeCount => _typeIds.Count;

        /// <summary>
        /// 清除所有注册的类型（主要用于测试）
        /// </summary>
        internal static void Clear()
        {
            lock (_lock)
            {
                _typeIds.Clear();
                _idToTypes.Clear();
                _nextTypeId = 0;
            }
        }
    }
}
