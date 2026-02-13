using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Collections;
using Asaki.Core.Logging;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体世界实现 - 高性能缓存查询优化版
    /// </summary>
    public class EntityWorld : IEntityWorld
    {
        // 实体存储：使用 MagicContainer 保证内存连续和 O(1) 访问
        private readonly MagicContainer<Entity> _entities = new();

        // 代际管理：防止实体销毁后句柄被重用导致的 ABA 问题
        private readonly List<int> _generations = new();

        // 核心优化：组件组缓存 (Component System Groups)
        // 映射：组件类型ID -> 拥有该组件的所有实体集合
        // 这将 Query 的复杂度从 O(TotalEntities) 降低为 O(EntitiesWithComponent)
        private readonly Dictionary<int, HashSet<Entity>> _componentGroups = new Dictionary<
            int,
            HashSet<Entity>
        >(64);

        public int EntityCount => _entities.Count;

        /// <summary>
        /// 创建实体
        /// </summary>
        public IEntity CreateEntity()
        {
            var entity = new Entity(this);
            int handle = _entities.Add(entity);

            // 代际更新
            int generation;
            if (handle < _generations.Count)
            {
                generation = ++_generations[handle];
            }
            else
            {
                generation = 0;
                while (_generations.Count <= handle)
                    _generations.Add(0);
            }

            entity.Initialize(new EntityId(handle, generation));

            // 仅通过 Broker 发布事件，不再提供 C# event 以防泄漏
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

            // 1. 获取实体
            var entity = _entities.Get(id.Handle);
            if (entity == null)
                return;

            // 2. 清理组缓存 (必须在 Dispose 之前做，因为 Dispose 会清空组件信息)
            // 优化：Entity.Dispose 会逐个移除组件，进而触发 OnComponentRemoved 更新缓存
            // 所以这里只需要调用 Dispose
            entity.Dispose();

            // 3. 发布事件
            AsakiBroker.Publish(new EntityDestroyedEvent { EntityId = id, World = this });

            // 4. 从容器移除
            _entities.Remove(id.Handle);
        }

        public IEntity GetEntity(EntityId id)
        {
            if (!IsValidId(id))
                return null;
            return _entities.Get(id.Handle);
        }

        public bool TryGetEntity(EntityId id, out IEntity entity)
        {
            entity = GetEntity(id);
            return entity != null;
        }

        public IEnumerable<IEntity> GetAllEntities()
        {
            return _entities; // MagicContainer 实现了 IEnumerable
        }

        #region Query Optimization (O(1) Lookup)

        /// <summary>
        /// 内部回调：当实体添加组件时更新缓存
        /// </summary>
        internal void OnComponentAdded(Entity entity, int typeId)
        {
            if (!_componentGroups.TryGetValue(typeId, out var group))
            {
                group = new HashSet<Entity>();
                _componentGroups[typeId] = group;
            }
            group.Add(entity);
        }

        /// <summary>
        /// 内部回调：当实体移除组件时更新缓存
        /// </summary>
        internal void OnComponentRemoved(Entity entity, int typeId)
        {
            if (_componentGroups.TryGetValue(typeId, out var group))
            {
                group.Remove(entity);
                // 此时不删除空 HashSet 以减少 GC 抖动
            }
        }

        public IEnumerable<IEntity> Query<T1>()
            where T1 : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T1>();
            if (_componentGroups.TryGetValue(typeId, out var group))
            {
                // 直接返回 HashSet 的枚举器，无 GC，极快
                foreach (var entity in group)
                    yield return entity;
            }
        }

        public IEnumerable<IEntity> Query<T1, T2>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
        {
            int t1 = ComponentTypeRegistry.GetTypeId<T1>();
            int t2 = ComponentTypeRegistry.GetTypeId<T2>();

            // 优化：总是遍历数量较小的那个组
            if (!_componentGroups.TryGetValue(t1, out var g1) || g1.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t2, out var g2) || g2.Count == 0)
                yield break;

            var smallerGroup = g1.Count < g2.Count ? g1 : g2;
            var otherTypeId = g1.Count < g2.Count ? t2 : t1;

            foreach (var entity in smallerGroup)
            {
                if (entity.HasComponent(otherTypeId))
                    yield return entity;
            }
        }

        public IEnumerable<IEntity> Query<T1, T2, T3>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
        {
            int t1 = ComponentTypeRegistry.GetTypeId<T1>();
            int t2 = ComponentTypeRegistry.GetTypeId<T2>();
            int t3 = ComponentTypeRegistry.GetTypeId<T3>();

            if (!_componentGroups.TryGetValue(t1, out var g1) || g1.Count == 0)
                yield break;

            // 简单策略：遍历 g1，检查 t2 和 t3
            foreach (var entity in g1)
            {
                if (entity.HasComponent(t2) && entity.HasComponent(t3))
                    yield return entity;
            }
        }

        #endregion

        public void Dispose()
        {
            _entities.ForEach(e =>
            {
                try
                {
                    e.Dispose();
                }
                catch { }
            });
            _entities.Clear();
            _generations.Clear();
            _componentGroups.Clear();
        }

        public IEntity GetEntityAt(int index) => _entities.GetAt(index);

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
