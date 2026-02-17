using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Broker;
using Asaki.Core.Collections;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体世界实现 - 高性能缓存查询优化版
    /// </summary>
    public class EntityWorld : IEntityWorld, IAsakiResolverProvider
    {
        // 实体存储：使用 MagicContainer 保证内存连续和 O(1) 访问
        private readonly MagicContainer<Entity> _entities = new();

        // 代际管理：防止实体销毁后句柄被重用导致的 ABA 问题
        // 使用数组替代 List 以获得更好的性能和线程安全性
        private int[] _generations = new int[64]; // 初始容量 64
        private int _generationCount; // 实际使用的代际数量
        private readonly object _generationLock = new object(); // 代际数组扩容锁

        // 核心优化：组件组缓存 (Component System Groups)
        // 映射：组件类型 ID -> 拥有该组件的所有实体集合
        // 这将 Query 的复杂度从 O(TotalEntities) 降低为 O(EntitiesWithComponent)
        private readonly Dictionary<int, HashSet<Entity>> _componentGroups = new Dictionary<
            int,
            HashSet<Entity>
        >(AsakiArchitectureConstants.DefaultComponentGroupsCapacity);

        public int EntityCount => _entities.Count;

        /// <summary>
        /// 解析器（用于组件依赖注入）
        /// </summary>
        public IAsakiResolver Resolver { get; private set; } = AsakiGlobalResolver.Instance;

        /// <summary>
        /// 创建实体
        /// </summary>
        public IEntity CreateEntity()
        {
            var entity = new Entity(this);
            int handle = _entities.Add(entity);

            // 代际更新 - 使用线程安全的方式
            int generation;

            lock (_generationLock)
            {
                if (handle < _generationCount)
                {
                    // 使用 Volatile.Write 确保写入的可见性
                    int newGeneration = Interlocked.Increment(ref _generations[handle]);
                    generation = newGeneration;
                }
                else
                {
                    // 需要扩容代际数组
                    EnsureGenerationCapacity(handle + 1);
                    _generationCount = Math.Max(_generationCount, handle + 1);
                    generation = 0;
                    _generations[handle] = 0;
                }
            }

            entity.Initialize(new EntityId(handle, generation));

            // 仅通过 Broker 发布事件，不再提供 C# event 以防泄漏
            AsakiBroker.Publish(new EntityCreatedEvent { EntityId = entity.Id, World = this });

            return entity;
        }

        /// <summary>
        /// 确保代际数组容量
        /// </summary>
        private void EnsureGenerationCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= _generations.Length)
                return;

            int newCapacity = Math.Max(requiredCapacity, _generations.Length * 2);
            Array.Resize(ref _generations, newCapacity);
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
                if (group.Count == 0)
                {
                    _componentGroups.Remove(typeId);
                }
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

            // 优化：选择最小的组进行遍历
            if (!_componentGroups.TryGetValue(t2, out var g2) || g2.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t3, out var g3) || g3.Count == 0)
                yield break;

            // 找到最小的组
            int minCount = Math.Min(g1.Count, Math.Min(g2.Count, g3.Count));
            HashSet<Entity> smallestGroup;
            int checkTypeId1,
                checkTypeId2;

            if (g1.Count == minCount)
            {
                smallestGroup = g1;
                checkTypeId1 = t2;
                checkTypeId2 = t3;
            }
            else if (g2.Count == minCount)
            {
                smallestGroup = g2;
                checkTypeId1 = t1;
                checkTypeId2 = t3;
            }
            else
            {
                smallestGroup = g3;
                checkTypeId1 = t1;
                checkTypeId2 = t2;
            }

            foreach (var entity in smallestGroup)
            {
                if (entity.HasComponent(checkTypeId1) && entity.HasComponent(checkTypeId2))
                    yield return entity;
            }
        }

        public IEnumerable<IEntity> Query<T1, T2, T3, T4>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent
        {
            int t1 = ComponentTypeRegistry.GetTypeId<T1>();
            int t2 = ComponentTypeRegistry.GetTypeId<T2>();
            int t3 = ComponentTypeRegistry.GetTypeId<T3>();
            int t4 = ComponentTypeRegistry.GetTypeId<T4>();

            if (!_componentGroups.TryGetValue(t1, out var g1) || g1.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t2, out var g2) || g2.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t3, out var g3) || g3.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t4, out var g4) || g4.Count == 0)
                yield break;

            int minCount = Math.Min(g1.Count, Math.Min(g2.Count, Math.Min(g3.Count, g4.Count)));
            HashSet<Entity> smallestGroup;
            int[] checkTypeIds;

            if (g1.Count == minCount)
            {
                smallestGroup = g1;
                checkTypeIds = new[] { t2, t3, t4 };
            }
            else if (g2.Count == minCount)
            {
                smallestGroup = g2;
                checkTypeIds = new[] { t1, t3, t4 };
            }
            else if (g3.Count == minCount)
            {
                smallestGroup = g3;
                checkTypeIds = new[] { t1, t2, t4 };
            }
            else
            {
                smallestGroup = g4;
                checkTypeIds = new[] { t1, t2, t3 };
            }

            foreach (var entity in smallestGroup)
            {
                bool hasAll = true;
                for (int i = 0; i < checkTypeIds.Length; i++)
                {
                    if (!entity.HasComponent(checkTypeIds[i]))
                    {
                        hasAll = false;
                        break;
                    }
                }
                if (hasAll)
                    yield return entity;
            }
        }

        public IEnumerable<IEntity> Query<T1, T2, T3, T4, T5>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent
            where T5 : class, IEntityComponent
        {
            int t1 = ComponentTypeRegistry.GetTypeId<T1>();
            int t2 = ComponentTypeRegistry.GetTypeId<T2>();
            int t3 = ComponentTypeRegistry.GetTypeId<T3>();
            int t4 = ComponentTypeRegistry.GetTypeId<T4>();
            int t5 = ComponentTypeRegistry.GetTypeId<T5>();

            if (!_componentGroups.TryGetValue(t1, out var g1) || g1.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t2, out var g2) || g2.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t3, out var g3) || g3.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t4, out var g4) || g4.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t5, out var g5) || g5.Count == 0)
                yield break;

            int minCount = Math.Min(
                g1.Count,
                Math.Min(g2.Count, Math.Min(g3.Count, Math.Min(g4.Count, g5.Count)))
            );
            HashSet<Entity> smallestGroup;
            int[] checkTypeIds;

            if (g1.Count == minCount)
            {
                smallestGroup = g1;
                checkTypeIds = new[] { t2, t3, t4, t5 };
            }
            else if (g2.Count == minCount)
            {
                smallestGroup = g2;
                checkTypeIds = new[] { t1, t3, t4, t5 };
            }
            else if (g3.Count == minCount)
            {
                smallestGroup = g3;
                checkTypeIds = new[] { t1, t2, t4, t5 };
            }
            else if (g4.Count == minCount)
            {
                smallestGroup = g4;
                checkTypeIds = new[] { t1, t2, t3, t5 };
            }
            else
            {
                smallestGroup = g5;
                checkTypeIds = new[] { t1, t2, t3, t4 };
            }

            foreach (var entity in smallestGroup)
            {
                bool hasAll = true;
                for (int i = 0; i < checkTypeIds.Length; i++)
                {
                    if (!entity.HasComponent(checkTypeIds[i]))
                    {
                        hasAll = false;
                        break;
                    }
                }
                if (hasAll)
                    yield return entity;
            }
        }

        public IEnumerable<IEntity> Query<T1, T2, T3, T4, T5, T6>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent
            where T5 : class, IEntityComponent
            where T6 : class, IEntityComponent
        {
            int t1 = ComponentTypeRegistry.GetTypeId<T1>();
            int t2 = ComponentTypeRegistry.GetTypeId<T2>();
            int t3 = ComponentTypeRegistry.GetTypeId<T3>();
            int t4 = ComponentTypeRegistry.GetTypeId<T4>();
            int t5 = ComponentTypeRegistry.GetTypeId<T5>();
            int t6 = ComponentTypeRegistry.GetTypeId<T6>();

            if (!_componentGroups.TryGetValue(t1, out var g1) || g1.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t2, out var g2) || g2.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t3, out var g3) || g3.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t4, out var g4) || g4.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t5, out var g5) || g5.Count == 0)
                yield break;
            if (!_componentGroups.TryGetValue(t6, out var g6) || g6.Count == 0)
                yield break;

            int minCount = Math.Min(
                g1.Count,
                Math.Min(
                    g2.Count,
                    Math.Min(g3.Count, Math.Min(g4.Count, Math.Min(g5.Count, g6.Count)))
                )
            );
            HashSet<Entity> smallestGroup;
            int[] checkTypeIds;

            if (g1.Count == minCount)
            {
                smallestGroup = g1;
                checkTypeIds = new[] { t2, t3, t4, t5, t6 };
            }
            else if (g2.Count == minCount)
            {
                smallestGroup = g2;
                checkTypeIds = new[] { t1, t3, t4, t5, t6 };
            }
            else if (g3.Count == minCount)
            {
                smallestGroup = g3;
                checkTypeIds = new[] { t1, t2, t4, t5, t6 };
            }
            else if (g4.Count == minCount)
            {
                smallestGroup = g4;
                checkTypeIds = new[] { t1, t2, t3, t5, t6 };
            }
            else if (g5.Count == minCount)
            {
                smallestGroup = g5;
                checkTypeIds = new[] { t1, t2, t3, t4, t6 };
            }
            else
            {
                smallestGroup = g6;
                checkTypeIds = new[] { t1, t2, t3, t4, t5 };
            }

            foreach (var entity in smallestGroup)
            {
                bool hasAll = true;
                for (int i = 0; i < checkTypeIds.Length; i++)
                {
                    if (!entity.HasComponent(checkTypeIds[i]))
                    {
                        hasAll = false;
                        break;
                    }
                }
                if (hasAll)
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
                catch (System.Exception ex)
                {
                    ALog.Error($"[EntityWorld] Error disposing entity {e.Id}: {ex}");
                }
            });
            _entities.Clear();

            lock (_generationLock)
            {
                _generations = new int[64];
                _generationCount = 0;
            }

            _componentGroups.Clear();
        }

        public IEntity GetEntityAt(int index) => _entities.GetAt(index);

        private bool IsValidId(EntityId id)
        {
            if (!id.IsValid)
                return false;

            lock (_generationLock)
            {
                if (id.Handle >= _generationCount)
                    return false;
                // 使用 Volatile.Read 确保读取的可见性
                return Volatile.Read(ref _generations[id.Handle]) == id.Generation;
            }
        }
    }
}
