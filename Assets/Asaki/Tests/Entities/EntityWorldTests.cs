using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Architecture.Entities;
using Asaki.Core.Architecture.Entities.Extensions;
using NUnit.Framework;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// EntityWorld 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class EntityWorldTests
    {
        private EntityWorld _world;

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

        #region Creation

        [Test]
        [Category("Unit")]
        public void CreateEntity_WhenCalled_ReturnsValidEntity()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.IsNotNull(entity, "Entity should not be null");
            Assert.IsTrue(entity.Id.IsValid, "Entity ID should be valid");
        }

        [Test]
        [Category("Unit")]
        public void CreateEntity_WhenCalled_IncrementsCount()
        {
            // Arrange
            int initialCount = _world.EntityCount;

            // Act
            _world.CreateEntity();

            // Assert
            Assert.AreEqual(initialCount + 1, _world.EntityCount, "Count should increment");
        }

        [Test]
        [Category("Unit")]
        public void CreateEntity_WhenCalledMultipleTimes_CreatesUniqueIds()
        {
            // Act
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            var entity3 = _world.CreateEntity();

            // Assert
            Assert.AreNotEqual(entity1.Id, entity2.Id, "IDs should be unique");
            Assert.AreNotEqual(entity2.Id, entity3.Id, "IDs should be unique");
            Assert.AreNotEqual(entity1.Id, entity3.Id, "IDs should be unique");
        }

        #endregion

        #region Destruction

        [Test]
        [Category("Unit")]
        public void DestroyEntity_WhenEntityExists_RemovesEntity()
        {
            // Arrange
            var entity = _world.CreateEntity();
            int initialCount = _world.EntityCount;

            // Act
            _world.DestroyEntity(entity.Id);

            // Assert
            Assert.AreEqual(initialCount - 1, _world.EntityCount, "Count should decrement");
        }

        [Test]
        [Category("Unit")]
        public void DestroyEntity_WhenEntityExists_CallsDisposeOnEntity()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestEntityComponent>();

            // Act
            _world.DestroyEntity(entity.Id);

            // Assert
            // If dispose was called correctly, no exception should occur
            Assert.Pass();
        }

        [Test]
        [Category("Unit")]
        public void DestroyEntity_WhenEntityDoesNotExist_DoesNothing()
        {
            // Arrange
            var invalidId = new EntityId(999, 0);
            int initialCount = _world.EntityCount;

            // Act
            _world.DestroyEntity(invalidId);

            // Assert
            Assert.AreEqual(initialCount, _world.EntityCount, "Count should not change");
        }

        [Test]
        [Category("Unit")]
        public void DestroyEntity_WithInvalidGeneration_DoesNothing()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var wrongGenerationId = new EntityId(entity.Id.Handle, entity.Id.Generation + 1);
            int initialCount = _world.EntityCount;

            // Act
            _world.DestroyEntity(wrongGenerationId);

            // Assert
            Assert.AreEqual(
                initialCount,
                _world.EntityCount,
                "Count should not change for wrong generation"
            );
        }

        [Test]
        [Category("Unit")]
        public void DestroyEntity_AfterReusingHandle_OldIdIsInvalid()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var id1 = entity1.Id;
            _world.DestroyEntity(id1);

            // Act - Create new entity (should reuse handle)
            var entity2 = _world.CreateEntity();

            // Assert
            Assert.AreNotEqual(
                id1,
                entity2.Id,
                "New entity should have different ID (different generation)"
            );
            Assert.AreEqual(id1.Handle, entity2.Id.Handle, "Handle should be reused");
        }

        #endregion

        #region Retrieval

        [Test]
        [Category("Unit")]
        public void GetEntity_WhenEntityExists_ReturnsEntity()
        {
            // Arrange
            var created = _world.CreateEntity();

            // Act
            var retrieved = _world.GetEntity(created.Id);

            // Assert
            Assert.IsNotNull(retrieved, "Should retrieve entity");
            Assert.AreEqual(created.Id, retrieved.Id, "Should be same entity");
        }

        [Test]
        [Category("Unit")]
        public void GetEntity_WhenEntityDoesNotExist_ReturnsNull()
        {
            // Arrange
            var invalidId = new EntityId(999, 0);

            // Act
            var retrieved = _world.GetEntity(invalidId);

            // Assert
            Assert.IsNull(retrieved, "Should return null for non-existent entity");
        }

        [Test]
        [Category("Unit")]
        public void GetEntity_AfterDestruction_ReturnsNull()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var id = entity.Id;
            _world.DestroyEntity(id);

            // Act
            var retrieved = _world.GetEntity(id);

            // Assert
            Assert.IsNull(retrieved, "Should return null for destroyed entity");
        }

        [Test]
        [Category("Unit")]
        public void TryGetEntity_WhenEntityExists_ReturnsTrueAndEntity()
        {
            // Arrange
            var created = _world.CreateEntity();

            // Act
            bool success = _world.TryGetEntity(created.Id, out var retrieved);

            // Assert
            Assert.IsTrue(success, "Should return true");
            Assert.IsNotNull(retrieved, "Should return entity");
            Assert.AreEqual(created.Id, retrieved.Id, "Should be same entity");
        }

        [Test]
        [Category("Unit")]
        public void TryGetEntity_WhenEntityDoesNotExist_ReturnsFalseAndNull()
        {
            // Arrange
            var invalidId = new EntityId(999, 0);

            // Act
            bool success = _world.TryGetEntity(invalidId, out var retrieved);

            // Assert
            Assert.IsFalse(success, "Should return false");
            Assert.IsNull(retrieved, "Should return null");
        }

        #endregion

        #region Enumeration

        [Test]
        [Category("Unit")]
        public void GetAllEntities_WhenNoEntities_ReturnsEmpty()
        {
            // Act
            var entities = _world.GetAllEntities().ToList();

            // Assert
            Assert.IsEmpty(entities, "Should return empty collection");
        }

        [Test]
        [Category("Unit")]
        public void GetAllEntities_WithMultipleEntities_ReturnsAll()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            var entity3 = _world.CreateEntity();

            // Act
            var entities = _world.GetAllEntities().ToList();

            // Assert
            Assert.AreEqual(3, entities.Count, "Should return 3 entities");
        }

        [Test]
        [Category("Unit")]
        public void GetAllEntities_AfterDestruction_DoesNotIncludeDestroyed()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            _world.DestroyEntity(entity1.Id);

            // Act
            var entities = _world.GetAllEntities().ToList();

            // Assert
            Assert.AreEqual(1, entities.Count, "Should return 1 entity");
            Assert.AreEqual(entity2.Id, entities[0].Id, "Should be the remaining entity");
        }

        [Test]
        [Category("Unit")]
        public void ForEach_WhenEntitiesExist_ProcessesAll()
        {
            // Arrange
            var ids = new List<EntityId>();
            _world.CreateEntity();
            _world.CreateEntity();
            _world.CreateEntity();

            // Act
            _world.ForEach(e => ids.Add(e.Id));

            // Assert
            Assert.AreEqual(3, ids.Count, "Should process all 3 entities");
        }


        #endregion

        #region Query

        [Test]
        [Category("Unit")]
        public void Query_WithSingleComponentType_ReturnsMatchingEntities()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            entity1.AddComponent<TestEntityComponent>();

            var entity2 = _world.CreateEntity();
            entity2.AddComponent<AnotherEntityComponent>();

            var entity3 = _world.CreateEntity();
            entity3.AddComponent<TestEntityComponent>();

            // Act
            var results = _world.Query<TestEntityComponent>().ToList();

            // Assert
            Assert.AreEqual(2, results.Count, "Should find 2 entities with TestEntityComponent");
        }

        [Test]
        [Category("Unit")]
        public void Query_WithTwoComponentTypes_ReturnsEntitiesWithBoth()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            entity1.AddComponent<TestEntityComponent>();
            entity1.AddComponent<AnotherEntityComponent>();

            var entity2 = _world.CreateEntity();
            entity2.AddComponent<TestEntityComponent>();

            var entity3 = _world.CreateEntity();
            entity3.AddComponent<AnotherEntityComponent>();

            // Act
            var results = _world.Query<TestEntityComponent, AnotherEntityComponent>().ToList();

            // Assert
            Assert.AreEqual(1, results.Count, "Should find 1 entity with both components");
            Assert.AreEqual(entity1.Id, results[0].Id, "Should be entity1");
        }

        [Test]
        [Category("Unit")]
        public void Query_WithThreeComponentTypes_ReturnsEntitiesWithAll()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            entity1.AddComponent<TestEntityComponent>();
            entity1.AddComponent<AnotherEntityComponent>();
            entity1.AddComponent<ThirdEntityComponent>();

            var entity2 = _world.CreateEntity();
            entity2.AddComponent<TestEntityComponent>();
            entity2.AddComponent<AnotherEntityComponent>();

            // Act
            var results = _world
                .Query<TestEntityComponent, AnotherEntityComponent, ThirdEntityComponent>()
                .ToList();

            // Assert
            Assert.AreEqual(1, results.Count, "Should find 1 entity with all 3 components");
        }

        [Test]
        [Category("Unit")]
        public void Query_WhenNoMatches_ReturnsEmpty()
        {
            // Arrange
            _world.CreateEntity();

            // Act
            var results = _world.Query<TestEntityComponent>().ToList();

            // Assert
            Assert.IsEmpty(results, "Should return empty when no matches");
        }

        #endregion



        #region Dispose

        [Test]
        [Category("Unit")]
        public void Dispose_WhenCalled_DisposesAllEntities()
        {
            // Arrange
            var entity = _world.CreateEntity();
            entity.AddComponent<TestEntityComponent>();

            // Act
            _world.Dispose();

            // Assert
            // If dispose works correctly, no exception should occur
            Assert.Pass();
        }


        #endregion
    }

    #region Test Components

    public class TestEntityComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }

    public class AnotherEntityComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }

    public class ThirdEntityComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }

    #endregion
}
