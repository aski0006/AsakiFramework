using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体实现 - 数组化存储优化版
    /// </summary>
    public class Entity : IEntity
    {
        // 强制转换为具体类型以访问内部优化方法
        private readonly EntityWorld _worldImpl;
        public IEntityWorld World => _worldImpl;

        // 优化 1: 使用数组替代 Dictionary，利用 TypeId 直接索引
        private IEntityComponent[] _componentsArray = new IEntityComponent[
            AsakiArchitectureConstants.DefaultEntityComponentArraySize
        ];

        // 优化 2: 保持 BitArray 用于极速 HasComponent 检查
        private BitArray _componentMask = new BitArray(
            AsakiArchitectureConstants.DefaultEntityComponentArraySize
        );

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
                // 数组遍历比 Dictionary Values 遍历更快
                for (int i = 0; i < _componentsArray.Length; i++)
                {
                    var c = _componentsArray[i];
                    if (c == null)
                        continue;
                    if (_isActive)
                        c.OnEnable();
                    else
                        c.OnDisable();
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

            // 数组扩容检查
            if (typeId >= _componentsArray.Length)
            {
                int newSize = Math.Max(typeId + 1, _componentsArray.Length * 2);
                Array.Resize(ref _componentsArray, newSize);
                _componentMask.Length = newSize;
            }

            if (_componentsArray[typeId] != null)
                throw new InvalidOperationException(
                    $"Entity {Id} already has component {type.Name}"
                );

            component.Entity = this;
            _componentsArray[typeId] = component;
            _componentMask[typeId] = true;
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
            if (typeId >= _componentsArray.Length)
                return null;
            // 数组直接访问，极快
            return _componentsArray[typeId] as T;
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
            if (typeId >= _componentsArray.Length)
                return false;

            var component = _componentsArray[typeId];
            if (component == null)
                return false;

            // 1. 生命周期处理
            if (_isActive)
                component.OnDisable();
            component.OnDetach();
            component.Dispose();

            // 2. 数据清理
            _componentsArray[typeId] = null;
            _componentMask[typeId] = false;
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
            int typeId = ComponentTypeRegistry.GetTypeId<T>();
            return HasComponent(typeId);
        }

        internal bool HasComponent(int typeId)
        {
            if (typeId >= _componentMask.Length)
                return false;
            return _componentMask[typeId];
        }

        public bool HasComponent(Type componentType)
        {
            return HasComponent(ComponentTypeRegistry.GetTypeId(componentType));
        }

        public IEnumerable<IEntityComponent> GetAllComponents()
        {
            for (int i = 0; i < _componentsArray.Length; i++)
            {
                var c = _componentsArray[i];
                if (c != null)
                    yield return c;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            for (int typeId = 0; typeId < _componentsArray.Length; typeId++)
            {
                var c = _componentsArray[typeId];
                if (c != null)
                {
                    try
                    {
                        _worldImpl.OnComponentRemoved(this, typeId);

                        if (_isActive)
                            c.OnDisable();
                        c.OnDetach();
                        c.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        Asaki.Core.Logging.ALog.Error(
                            $"[Entity] Error disposing component {c.GetType().Name} on {Id}: {ex}"
                        );
                    }
                    _componentsArray[typeId] = null;
                }
            }

            _componentMask.SetAll(false);
            _componentCount = 0;
            Id = EntityId.Invalid;
        }

        public override string ToString() => $"Entity[{Id}] (Comps: {_componentCount})";
    }
}
