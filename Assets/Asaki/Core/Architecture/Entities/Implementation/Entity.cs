using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体实现 - 混合存储优化版
    /// 小 TypeId 使用数组直接索引 (O(1) 性能)
    /// 大 TypeId 使用 Dictionary 存储 (避免内存浪费)
    /// </summary>
    public class Entity : IEntity
    {
        // 强制转换为具体类型以访问内部优化方法
        private readonly EntityWorld _worldImpl;
        public IEntityWorld World => _worldImpl;

        // 常量：数组直接索引的最大 TypeId 阈值
        // TypeId <= 127 使用数组 (128 个元素 = 约 1KB 内存)
        // TypeId > 127 使用 Dictionary
        private const int ArrayIndexThreshold = AsakiArchitectureConstants.EntityComponentArrayIndexThreshold;

        // 优化 1: 小 TypeId 使用数组直接索引 (O(1) 性能)
        private IEntityComponent[] _fastComponentsArray = new IEntityComponent[
            ArrayIndexThreshold + 1
        ];

        // 优化 2: 大 TypeId 使用 Dictionary 存储 (避免稀疏数组内存浪费)
        private Dictionary<int, IEntityComponent> _sparseComponents;

        // 优化 3: BitArray 用于极速 HasComponent 检查 (仅用于数组部分)
        private BitArray _componentMask = new BitArray(ArrayIndexThreshold + 1);

        // 稀疏组件的 HashSet 用于快速存在性检查
        private HashSet<int> _sparseComponentTypeIds;

        private int _componentCount;
        private bool _isActive = true;
        private bool _isDisposed;

        public EntityId Id { get; private set; }
        public int ComponentCount => _componentCount;
        public bool IsDisposed => _isDisposed;

        internal Entity(EntityWorld world)
        {
            _worldImpl = world ?? throw new ArgumentNullException(nameof(world));
        }

        internal void Initialize(EntityId id)
        {
            Id = id;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;
                _isActive = value;

                // 遍历数组部分
                for (int i = 0; i < _fastComponentsArray.Length; i++)
                {
                    var c = _fastComponentsArray[i];
                    if (c != null)
                    {
                        if (_isActive)
                            c.OnEnable();
                        else
                            c.OnDisable();
                    }
                }

                // 遍历 Dictionary 部分
                if (_sparseComponents != null)
                {
                    foreach (var c in _sparseComponents.Values)
                    {
                        if (_isActive)
                            c.OnEnable();
                        else
                            c.OnDisable();
                    }
                }
            }
        }

        public T AddComponent<T>()
            where T : class, IEntityComponent, new()
        {
            var component = new T();
            return AddComponentInternal(component, ComponentTypeRegistry.GetTypeId<T>(), typeof(T));
        }

        public T AddComponent<T>(T component)
            where T : class, IEntityComponent
        {
            return AddComponentInternal(component, ComponentTypeRegistry.GetTypeId<T>(), typeof(T));
        }

        private T AddComponentInternal<T>(T component, int typeId, Type type)
            where T : class, IEntityComponent
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Entity));

            // 检查是否已存在该组件
            if (HasComponent(typeId))
            {
                throw new InvalidOperationException(
                    $"Entity {Id} already has component {type.Name}"
                );
            }

            component.Entity = this;

            // 根据 TypeId 选择存储方式
            if (typeId <= ArrayIndexThreshold)
            {
                // 数组直接索引 (O(1))
                _fastComponentsArray[typeId] = component;
                _componentMask[typeId] = true;
            }
            else
            {
                // Dictionary 存储 (避免内存浪费)
                if (_sparseComponents == null)
                {
                    _sparseComponents = new Dictionary<int, IEntityComponent>(4);
                    _sparseComponentTypeIds = new HashSet<int>();
                }
                _sparseComponents[typeId] = component;
                _sparseComponentTypeIds.Add(typeId);
            }

            _componentCount++;

            // 通知 World 更新缓存
            _worldImpl.OnComponentAdded(this, typeId);

            component.OnAttach();
            if (_isActive)
                component.OnEnable();

            AsakiBroker.Publish(
                new ComponentAddedEvent { EntityId = Id, ComponentTypeName = type.Name }
            );
            return component;
        }

        public T GetComponent<T>()
            where T : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T>();

            if (typeId <= ArrayIndexThreshold)
            {
                // 数组直接访问，极快
                if (typeId >= _fastComponentsArray.Length)
                    return null;
                return _fastComponentsArray[typeId] as T;
            }
            else
            {
                // Dictionary 查找
                if (_sparseComponents == null)
                    return null;
                return _sparseComponents.TryGetValue(typeId, out var comp) ? comp as T : null;
            }
        }

        public bool TryGetComponent<T>(out T component)
            where T : class, IEntityComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        public bool RemoveComponent<T>()
            where T : class, IEntityComponent
        {
            return RemoveComponent(typeof(T));
        }

        public bool RemoveComponent(Type componentType)
        {
            int typeId = ComponentTypeRegistry.GetTypeId(componentType);

            IEntityComponent component = GetComponentInternal(typeId);
            if (component == null)
                return false;

            // 1. 生命周期处理
            if (_isActive)
                component.OnDisable();
            component.OnDetach();
            component.Dispose();

            // 2. 数据清理
            if (typeId <= ArrayIndexThreshold)
            {
                _fastComponentsArray[typeId] = null;
                _componentMask[typeId] = false;
            }
            else
            {
                if (_sparseComponents != null)
                {
                    _sparseComponents.Remove(typeId);
                    _sparseComponentTypeIds?.Remove(typeId);

                    // 如果稀疏字典为空，清理它以节省内存
                    if (_sparseComponents.Count == 0)
                    {
                        _sparseComponents = null;
                        _sparseComponentTypeIds = null;
                    }
                }
            }

            _componentCount--;

            // 3. 通知 World
            _worldImpl.OnComponentRemoved(this, typeId);

            AsakiBroker.Publish(
                new ComponentRemovedEvent { EntityId = Id, ComponentTypeName = componentType.Name }
            );
            return true;
        }

        public bool HasComponent<T>()
            where T : class, IEntityComponent
        {
            return HasComponent(ComponentTypeRegistry.GetTypeId<T>());
        }

        internal bool HasComponent(int typeId)
        {
            if (typeId <= ArrayIndexThreshold)
            {
                if (typeId >= _componentMask.Length)
                    return false;
                return _componentMask[typeId];
            }
            else
            {
                return _sparseComponentTypeIds?.Contains(typeId) == true;
            }
        }

        public bool HasComponent(Type componentType)
        {
            return HasComponent(ComponentTypeRegistry.GetTypeId(componentType));
        }

        // 内部方法：获取组件（不关心类型）
        private IEntityComponent GetComponentInternal(int typeId)
        {
            if (typeId <= ArrayIndexThreshold)
            {
                if (typeId >= _fastComponentsArray.Length)
                    return null;
                return _fastComponentsArray[typeId];
            }
            else
            {
                if (_sparseComponents == null)
                    return null;
                return _sparseComponents.TryGetValue(typeId, out var comp) ? comp : null;
            }
        }

        public IEnumerable<IEntityComponent> GetAllComponents()
        {
            // 遍历数组部分
            for (int i = 0; i < _fastComponentsArray.Length; i++)
            {
                var c = _fastComponentsArray[i];
                if (c != null)
                    yield return c;
            }

            // 遍历 Dictionary 部分
            if (_sparseComponents != null)
            {
                foreach (var c in _sparseComponents.Values)
                {
                    yield return c;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // 创建快照，避免遍历时修改
            var componentsToDispose = new List<(int typeId, IEntityComponent component)>();

            // 收集数组部分
            for (int i = 0; i < _fastComponentsArray.Length; i++)
            {
                var c = _fastComponentsArray[i];
                if (c != null)
                {
                    componentsToDispose.Add((i, c));
                }
            }

            // 收集 Dictionary 部分
            if (_sparseComponents != null)
            {
                foreach (var kvp in _sparseComponents)
                {
                    componentsToDispose.Add((kvp.Key, kvp.Value));
                }
            }

            // 逐个安全处置
            foreach (var (typeId, component) in componentsToDispose)
            {
                try
                {
                    _worldImpl.OnComponentRemoved(this, typeId);
                    if (_isActive)
                        component.OnDisable();
                    component.OnDetach();
                    component.Dispose();
                }
                catch (System.Exception ex)
                {
                    Asaki.Core.Logging.ALog.Error(
                        $"[Entity] Error disposing component {component.GetType().Name} on {Id}: {ex}"
                    );
                }
                finally
                {
                    // finally 确保清理
                    if (typeId <= ArrayIndexThreshold && typeId < _fastComponentsArray.Length)
                    {
                        _fastComponentsArray[typeId] = null;
                        _componentMask[typeId] = false;
                    }
                    else
                    {
                        _sparseComponents?.Remove(typeId);
                        _sparseComponentTypeIds?.Remove(typeId);
                    }
                }
            }

            // 清理集合
            _componentMask.SetAll(false);
            _sparseComponents?.Clear();
            _sparseComponentTypeIds?.Clear();
            _componentCount = 0;
            Id = EntityId.Invalid;
        }

        public override string ToString() => $"Entity[{Id}] (Comps: {_componentCount})";
    }
}
