using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Collections;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体世界实现 - 基于魔法容器的高性能存储
    /// </summary>
    public class EntityWorld : IEntityWorld
    {
        // 使用魔法容器存储实体 - 内存连续 + O(1)增删
        private readonly MagicContainer<Entity> _entities = new();

        // 代际管理 - 防止 ABA 问题
        private readonly List<int> _generations = new();

        // 组件类型注册
        private readonly Dictionary<Type, int> _componentTypeIds = new();
        private int _nextComponentTypeId = 0;

        /// <summary>
        /// 实体数量
        /// </summary>
        public int EntityCount => _entities.Count;

        /// <summary>
        /// 实体被创建时的事件
        /// </summary>
        public event Action<IEntity> OnEntityCreated;

        /// <summary>
        /// 实体被销毁时的事件
        /// </summary>
        public event Action<IEntity> OnEntityDestroyed;

        /// <summary>
        /// 创建实体
        /// </summary>
        public IEntity CreateEntity()
        {
            var entity = new Entity(this);
            int handle = _entities.Add(entity);

            // 分配代际
            int generation;
            if (handle < _generations.Count)
            {
                generation = ++_generations[handle];
            }
            else
            {
                generation = 0;
                while (_generations.Count <= handle)
                {
                    _generations.Add(0);
                }
            }

            // 设置实体ID
            entity.Initialize(new EntityId(handle, generation));

            OnEntityCreated?.Invoke(entity);

            // 发布 Broker 事件
            AsakiBroker.Publish(new EntityCreatedEvent { EntityId = entity.Id, World = this });

            return entity;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void DestroyEntity(EntityId id)
        {
            if (!IsValidId(id))
                return;

            var entity = _entities.Get(id.Handle);
            if (entity == null)
                return;

            OnEntityDestroyed?.Invoke(entity);

            // 发布 Broker 事件
            AsakiBroker.Publish(new EntityDestroyedEvent { EntityId = id, World = this });

            // 清理实体
            entity.Dispose();

            // 魔法容器 O(1) 删除（Swap到末尾）
            _entities.Remove(id.Handle);
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public IEntity GetEntity(EntityId id)
        {
            if (!IsValidId(id))
                return null;
            return _entities.Get(id.Handle);
        }

        /// <summary>
        /// 尝试获取实体
        /// </summary>
        public bool TryGetEntity(EntityId id, out IEntity entity)
        {
            entity = GetEntity(id);
            return entity != null;
        }

        /// <summary>
        /// 获取所有实体（连续内存遍历，高性能）
        /// </summary>
        public IEnumerable<IEntity> GetAllEntities()
        {
            // 直接遍历底层数组，缓存友好
            for (int i = 0; i < _entities.Capacity; i++)
            {
                yield return _entities.GetAt(i);
            }
        }

        /// <summary>
        /// 查询具有指定组件的实体
        /// </summary>
        public IEnumerable<IEntity> Query<T1>()
            where T1 : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T1>();

            for (int i = 0; i < _entities.Capacity; i++)
            {
                var entity = _entities.GetAt(i);
                if (entity.HasComponent(typeId))
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        public IEnumerable<IEntity> Query<T1, T2>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
        {
            int typeId1 = ComponentTypeRegistry.GetTypeId<T1>();
            int typeId2 = ComponentTypeRegistry.GetTypeId<T2>();

            for (int i = 0; i < _entities.Capacity; i++)
            {
                var entity = _entities.GetAt(i);
                if (entity.HasComponent(typeId1) && entity.HasComponent(typeId2))
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        public IEnumerable<IEntity> Query<T1, T2, T3>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
        {
            int typeId1 = ComponentTypeRegistry.GetTypeId<T1>();
            int typeId2 = ComponentTypeRegistry.GetTypeId<T2>();
            int typeId3 = ComponentTypeRegistry.GetTypeId<T3>();

            for (int i = 0; i < _entities.Capacity; i++)
            {
                var entity = _entities.GetAt(i);
                if (
                    entity.HasComponent(typeId1)
                    && entity.HasComponent(typeId2)
                    && entity.HasComponent(typeId3)
                )
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 批量处理所有实体 - 最高性能遍历
        /// </summary>
        public void ForEach(Action<IEntity> action)
        {
            if (action == null)
                return;

            for (int i = 0; i < _entities.Capacity; i++)
            {
                action(_entities.GetAt(i));
            }
        }

        /// <summary>
        /// 批量处理所有实体（带索引）
        /// </summary>
        public void ForEach(Action<int, IEntity> action)
        {
            if (action == null)
                return;

            for (int i = 0; i < _entities.Capacity; i++)
            {
                action(i, _entities.GetAt(i));
            }
        }

        /// <summary>
        /// 释放世界（销毁所有实体）
        /// </summary>
        public void Dispose()
        {
            // 批量销毁所有实体
            for (int i = 0; i < _entities.Capacity; i++)
            {
                try
                {
                    _entities.GetAt(i)?.Dispose();
                }
                catch (Exception ex)
                {
                    Core.Logging.ALog.Error($"[EntityWorld] Error disposing entity: {ex.Message}");
                }
            }

            // 清理事件订阅
            OnEntityCreated = null;
            OnEntityDestroyed = null;

            // 清理容器
            _entities.Clear();
            _generations.Clear();
        }

        /// <summary>
        /// 检查实体ID是否有效
        /// </summary>
        private bool IsValidId(EntityId id)
        {
            if (!id.IsValid)
                return false;
            if (id.Handle >= _generations.Count)
                return false;
            return _generations[id.Handle] == id.Generation;
        }
    }
}
