using System;
using System.Linq;
using Asaki.Core.Architecture.Entities;
using NUnit.Framework;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// Entity 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class EntityTests
    {
        private IEntityWorld _world;

        [SetUp]
        public void Setup()
        {
            _world = new EntityWorld();
        }

        [TearDown]
        public void Teardown()
        {
            _world?.Dispose();
            _world = null;
        }

        #region Basic Properties

        [Test]
        [Category("Unit")]
        public void Id_AfterCreation_ReturnsValidId()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.IsTrue(entity.Id.IsValid, "Entity ID should be valid");
            Assert.GreaterOrEqual(entity.Id.Handle, 0, "Handle should be non-negative");
        }

        [Test]
        [Category("Unit")]
        public void World_AfterCreation_ReturnsCorrectWorld()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.AreSame(_world, entity.World, "World should match");
        }

        [Test]
        [Category("Unit")]
        public void IsActive_AfterCreation_IsTrue()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.IsTrue(entity.IsActive, "Entity should be active by default");
        }

        [Test]
        [Category("Unit")]
        public void ComponentCount_AfterCreation_IsZero()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.AreEqual(0, entity.ComponentCount, "New entity should have no components");
        }

        #endregion

        #region Component Addition

        [Test]
        [Category("Unit")]
        public void AddComponent_WhenNewComponent_AddsSuccessfully()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var component = entity.AddComponent<TestComponent>();

            // Assert
            Assert.IsNotNull(component, "Component should be added");
            Assert.AreEqual(1, entity.ComponentCount, "Component count should be 1");
            Assert.AreSame(entity, component.Entity, "Component should reference entity");
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WhenComponentTypeAlreadyExists_ThrowsException()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestComponent>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => entity.AddComponent<TestComponent>());
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WithInstance_AddsSuccessfully()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = new TestComponent { Value = 42 };

            // Act
            var added = entity.AddComponent(component);

            // Assert
            Assert.IsNotNull(added, "Component should be added");
            Assert.AreEqual(42, added.Value, "Component data should be preserved");
            Assert.AreSame(entity, added.Entity, "Component should reference entity");
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WithNullInstance_ThrowsArgumentNullException()
        {
            // Arrange
            var entity = _world.CreateEntity();
            TestComponent nullComponent = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => entity.AddComponent(nullComponent));
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_MultipleDifferentTypes_AddsAll()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var comp1 = entity.AddComponent<TestComponent>();
            var comp2 = entity.AddComponent<AnotherComponent>();

            // Assert
            Assert.AreEqual(2, entity.ComponentCount, "Should have 2 components");
            Assert.IsNotNull(comp1, "First component should exist");
            Assert.IsNotNull(comp2, "Second component should exist");
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WhenEntityIsDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => entity.AddComponent<TestComponent>());
        }

        #endregion

        #region Component Retrieval

        [Test]
        [Category("Unit")]
        public void GetComponent_WhenComponentExists_ReturnsComponent()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var added = entity.AddComponent<TestComponent>();

            // Act
            var retrieved = entity.GetComponent<TestComponent>();

            // Assert
            Assert.IsNotNull(retrieved, "Should retrieve component");
            Assert.AreSame(added, retrieved, "Should be same instance");
        }

        [Test]
        [Category("Unit")]
        public void GetComponent_WhenComponentDoesNotExist_ReturnsNull()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var retrieved = entity.GetComponent<TestComponent>();

            // Assert
            Assert.IsNull(retrieved, "Should return null for non-existent component");
        }

        [Test]
        [Category("Unit")]
        public void TryGetComponent_WhenComponentExists_ReturnsTrueAndComponent()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var added = entity.AddComponent<TestComponent>();

            // Act
            bool success = entity.TryGetComponent<TestComponent>(out var retrieved);

            // Assert
            Assert.IsTrue(success, "Should return true");
            Assert.IsNotNull(retrieved, "Should return component");
            Assert.AreSame(added, retrieved, "Should be same instance");
        }

        [Test]
        [Category("Unit")]
        public void TryGetComponent_WhenComponentDoesNotExist_ReturnsFalseAndNull()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            bool success = entity.TryGetComponent<TestComponent>(out var retrieved);

            // Assert
            Assert.IsFalse(success, "Should return false");
            Assert.IsNull(retrieved, "Should return null");
        }

        #endregion

        #region Component Removal

        [Test]
        [Category("Unit")]
        public void RemoveComponent_WhenComponentExists_RemovesSuccessfully()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = entity.AddComponent<TestComponent>();

            // Act
            bool removed = entity.RemoveComponent<TestComponent>();

            // Assert
            Assert.IsTrue(removed, "Should return true");
            Assert.AreEqual(0, entity.ComponentCount, "Component count should be 0");
            Assert.IsNull(entity.GetComponent<TestComponent>(), "Should not find component");
        }

        [Test]
        [Category("Unit")]
        public void RemoveComponent_WhenComponentDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            bool removed = entity.RemoveComponent<TestComponent>();

            // Assert
            Assert.IsFalse(removed, "Should return false for non-existent component");
        }

        [Test]
        [Category("Unit")]
        public void RemoveComponent_CallsLifecycleMethods()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = entity.AddComponent<LifecycleTrackingComponent>();

            // Act
            entity.RemoveComponent<LifecycleTrackingComponent>();

            // Assert
            Assert.IsTrue(component.OnDisableCalled, "OnDisable should be called");
            Assert.IsTrue(component.OnDetachCalled, "OnDetach should be called");
            Assert.IsTrue(component.DisposeCalled, "Dispose should be called");
        }

        #endregion

        #region Component Existence Check

        [Test]
        [Category("Unit")]
        public void HasComponent_WhenComponentExists_ReturnsTrue()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestComponent>();

            // Act
            bool hasComponent = entity.HasComponent<TestComponent>();

            // Assert
            Assert.IsTrue(hasComponent, "Should return true for existing component");
        }

        [Test]
        [Category("Unit")]
        public void HasComponent_WhenComponentDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            bool hasComponent = entity.HasComponent<TestComponent>();

            // Assert
            Assert.IsFalse(hasComponent, "Should return false for non-existent component");
        }

        [Test]
        [Category("Unit")]
        public void HasComponent_AfterRemoval_ReturnsFalse()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestComponent>();
            entity.RemoveComponent<TestComponent>();

            // Act
            bool hasComponent = entity.HasComponent<TestComponent>();

            // Assert
            Assert.IsFalse(hasComponent, "Should return false after removal");
        }

        #endregion

        #region Lifecycle Methods

        [Test]
        [Category("Unit")]
        public void AddComponent_CallsOnAttach()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var component = entity.AddComponent<LifecycleTrackingComponent>();

            // Assert
            Assert.IsTrue(component.OnAttachCalled, "OnAttach should be called");
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WhenEntityIsActive_CallsOnEnable()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.IsActive = true;

            // Act
            var component = entity.AddComponent<LifecycleTrackingComponent>();

            // Assert
            Assert.IsTrue(
                component.OnEnableCalled,
                "OnEnable should be called when entity is active"
            );
        }

        [Test]
        [Category("Unit")]
        public void AddComponent_WhenEntityIsInactive_DoesNotCallOnEnable()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.IsActive = false;

            // Act
            var component = entity.AddComponent<LifecycleTrackingComponent>();

            // Assert
            Assert.IsFalse(
                component.OnEnableCalled,
                "OnEnable should not be called when entity is inactive"
            );
        }

        [Test]
        [Category("Unit")]
        public void SetIsActive_ToTrue_CallsOnEnable()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.IsActive = false;
            var component = entity.AddComponent<LifecycleTrackingComponent>();
            component.ResetTracking();

            // Act
            entity.IsActive = true;

            // Assert
            Assert.IsTrue(
                component.OnEnableCalled,
                "OnEnable should be called when activating entity"
            );
        }

        [Test]
        [Category("Unit")]
        public void SetIsActive_ToFalse_CallsOnDisable()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.IsActive = true;
            var component = entity.AddComponent<LifecycleTrackingComponent>();
            component.ResetTracking();

            // Act
            entity.IsActive = false;

            // Assert
            Assert.IsTrue(
                component.OnDisableCalled,
                "OnDisable should be called when deactivating entity"
            );
        }

        #endregion

        #region GetAllComponents

        [Test]
        [Category("Unit")]
        public void GetAllComponents_WhenNoComponents_ReturnsEmpty()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var components = entity.GetAllComponents();

            // Assert
            Assert.IsEmpty(components, "Should return empty collection");
        }

        [Test]
        [Category("Unit")]
        public void GetAllComponents_WithMultipleComponents_ReturnsAll()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var comp1 = entity.AddComponent<TestComponent>();
            var comp2 = entity.AddComponent<AnotherComponent>();

            // Act
            var components = entity.GetAllComponents();

            // Assert
            Assert.AreEqual(2, components.Count(), "Should return 2 components");
        }

        #endregion

        #region Dispose

        [Test]
        [Category("Unit")]
        public void Dispose_CallsLifecycleMethodsOnAllComponents()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var comp1 = entity.AddComponent<LifecycleTrackingComponent>();
            var comp2 = entity.AddComponent<AnotherLifecycleTrackingComponent>();

            // Act
            entity.Dispose();

            // Assert
            Assert.IsTrue(comp1.OnDisableCalled, "OnDisable should be called on comp1");
            Assert.IsTrue(comp1.OnDetachCalled, "OnDetach should be called on comp1");
            Assert.IsTrue(comp1.DisposeCalled, "Dispose should be called on comp1");
            Assert.IsTrue(comp2.OnDisableCalled, "OnDisable should be called on comp2");
            Assert.IsTrue(comp2.OnDetachCalled, "OnDetach should be called on comp2");
            Assert.IsTrue(comp2.DisposeCalled, "Dispose should be called on comp2");
        }

        [Test]
        [Category("Unit")]
        public void Dispose_AfterDisposal_EntityIsDisposed()
        {
            // Arrange
            var entity = _world.CreateEntity() as Entity;

            // Act
            entity.Dispose();

            // Assert
            Assert.IsTrue(entity.IsDisposed, "Entity should be disposed");
        }

        #endregion

        #region ToString

        [Test]
        [Category("Unit")]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestComponent>();

            // Act
            string result = entity.ToString();

            // Assert
            StringAssert.Contains("Entity", result, "Should contain 'Entity'");
            StringAssert.Contains("1", result, "Should contain component count");
        }

        #endregion
    }

    #region Test Components

    public class TestComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }
        public int Value { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }

    public class AnotherComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }
        public string Data { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }

    public class LifecycleTrackingComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }
        public bool OnAttachCalled { get; private set; }
        public bool OnDetachCalled { get; private set; }
        public bool OnEnableCalled { get; private set; }
        public bool OnDisableCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public void ResetTracking()
        {
            OnAttachCalled = false;
            OnDetachCalled = false;
            OnEnableCalled = false;
            OnDisableCalled = false;
            DisposeCalled = false;
        }

        public void OnAttach()
        {
            OnAttachCalled = true;
        }

        public void OnDetach()
        {
            OnDetachCalled = true;
        }

        public void OnEnable()
        {
            OnEnableCalled = true;
        }

        public void OnDisable()
        {
            OnDisableCalled = true;
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class AnotherLifecycleTrackingComponent : LifecycleTrackingComponent { }

    #endregion
}
