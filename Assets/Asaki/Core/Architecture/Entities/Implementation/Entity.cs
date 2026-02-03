using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体实现
    /// </summary>
    public class Entity : IEntity
    {
        private readonly IEntityWorld _world;
        private readonly Dictionary<int, IEntityComponent> _components = new();
        private BitArray _componentMask;
        private bool _isDisposed;

        /// <summary>
        /// 实体唯一标识符
        /// </summary>
        public EntityId Id { get; private set; }

        /// <summary>
        /// 实体是否激活
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;
                _isActive = value;

                if (_isActive)
                {
                    foreach (var component in _components.Values)
                    {
                        component.OnEnable();
                    }
                }
                else
                {
                    foreach (var component in _components.Values)
                    {
                        component.OnDisable();
                    }
                }
            }
        }
        private bool _isActive = true;

        /// <summary>
        /// 实体所属世界
        /// </summary>
        public IEntityWorld World => _world;

        /// <summary>
        /// 组件数量
        /// </summary>
        public int ComponentCount => _components.Count;

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsDisposed => _isDisposed;

        internal Entity(IEntityWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _componentMask = new BitArray(32); // 初始支持32种组件
        }

        internal void Initialize(EntityId id)
        {
            Id = id;
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        public T AddComponent<T>()
            where T : class, IEntityComponent, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(
                    nameof(Entity),
                    $"Cannot add component to disposed entity {Id}"
                );

            int typeId = ComponentTypeRegistry.GetTypeId<T>();

            if (_components.ContainsKey(typeId))
            {
                throw new InvalidOperationException(
                    $"Entity {Id} already has component of type {typeof(T).Name}"
                );
            }

            var component = new T();
            component.Entity = this;

            _components[typeId] = component;
            EnsureMaskCapacity(typeId);
            _componentMask[typeId] = true;

            component.OnAttach();
            if (IsActive)
                component.OnEnable();

            // 发布组件添加事件
            AsakiBroker.Publish(
                new ComponentAddedEvent { EntityId = Id, ComponentTypeName = typeof(T).Name }
            );

            return component;
        }

        /// <summary>
        /// 添加已有组件实例
        /// </summary>
        public T AddComponent<T>(T component)
            where T : class, IEntityComponent
        {
            if (_isDisposed)
                throw new ObjectDisposedException(
                    nameof(Entity),
                    $"Cannot add component to disposed entity {Id}"
                );

            if (component == null)
                throw new ArgumentNullException(nameof(component));

            int typeId = ComponentTypeRegistry.GetTypeId<T>();

            if (_components.ContainsKey(typeId))
            {
                throw new InvalidOperationException(
                    $"Entity {Id} already has component of type {typeof(T).Name}"
                );
            }

            component.Entity = this;
            _components[typeId] = component;
            EnsureMaskCapacity(typeId);
            _componentMask[typeId] = true;

            component.OnAttach();
            if (IsActive)
                component.OnEnable();

            return component;
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>()
            where T : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T>();
            return _components.TryGetValue(typeId, out var component) ? component as T : null;
        }

        /// <summary>
        /// 尝试获取组件
        /// </summary>
        public bool TryGetComponent<T>(out T component)
            where T : class, IEntityComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        public bool RemoveComponent<T>()
            where T : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T>();

            if (!_components.TryGetValue(typeId, out var component))
                return false;

            if (IsActive)
                component.OnDisable();
            component.OnDetach();
            component.Dispose();

            _components.Remove(typeId);
            if (typeId < _componentMask.Length)
                _componentMask[typeId] = false;

            // 发布组件移除事件
            AsakiBroker.Publish(
                new ComponentRemovedEvent { EntityId = Id, ComponentTypeName = typeof(T).Name }
            );

            return true;
        }

        /// <summary>
        /// 检查是否具有指定组件
        /// </summary>
        public bool HasComponent<T>()
            where T : class, IEntityComponent
        {
            int typeId = ComponentTypeRegistry.GetTypeId<T>();
            return HasComponent(typeId);
        }

        /// <summary>
        /// 检查是否具有指定组件（内部使用）
        /// </summary>
        internal bool HasComponent(int typeId)
        {
            return typeId < _componentMask.Length && _componentMask[typeId];
        }

        /// <summary>
        /// 获取所有组件
        /// </summary>
        public IEnumerable<IEntityComponent> GetAllComponents()
        {
            return _components.Values;
        }

        /// <summary>
        /// 释放实体
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // 清理所有组件
            foreach (var component in _components.Values)
            {
                try
                {
                    if (IsActive)
                        component.OnDisable();
                    component.OnDetach();
                    component.Dispose();
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断清理过程
                    Core.Logging.ALog.Error($"[Entity] Error disposing component: {ex.Message}");
                }
            }

            _components.Clear();
            _componentMask.SetAll(false);
            Id = EntityId.Invalid;
        }

        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"Entity[{Id}] (Components: {ComponentCount})";
        }

        private void EnsureMaskCapacity(int typeId)
        {
            if (typeId >= _componentMask.Length)
            {
                _componentMask.Length = typeId + 8; // 每次扩展8个位
            }
        }
    }
}
