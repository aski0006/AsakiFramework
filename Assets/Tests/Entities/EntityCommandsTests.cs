using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Entities;
using Asaki.Core.Architecture.Entities.Commands;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// 实体命令单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class EntityCommandsTests
    {
        private TestArchitecture _architecture;

        [SetUp]
        public void Setup()
        {
            _architecture = new TestArchitecture();
            _architecture.Init(null);
        }

        [TearDown]
        public void Teardown()
        {
            _architecture?.Dispose();
            _architecture = null;
        }

        #region CreateEntityCommand

        [Test]
        [Category("Unit")]
        public void CreateEntityCommand_Execute_ReturnsValidEntityId()
        {
            // Arrange
            var command = new CreateEntityCommand();
            command.Create(_architecture);

            // Act
            var entityId = command.Execute();

            // Assert
            Assert.IsTrue(entityId.IsValid, "Should return valid entity ID");
        }

        [Test]
        [Category("Unit")]
        public void CreateEntityCommand_Execute_CreatesEntityInWorld()
        {
            // Arrange
            var command = new CreateEntityCommand();
            command.Create(_architecture);
            int initialCount = _architecture.GetModel<EntityModel>().World.EntityCount;

            // Act
            command.Execute();

            // Assert
            Assert.AreEqual(
                initialCount + 1,
                _architecture.GetModel<EntityModel>().World.EntityCount,
                "World should have one more entity"
            );
        }

        [Test]
        [Category("Unit")]
        public void CreateEntityCommand_ExecuteMultipleTimes_CreatesMultipleEntities()
        {
            // Arrange
            var command = new CreateEntityCommand();
            command.Create(_architecture);

            // Act
            var id1 = command.Execute();
            var id2 = command.Execute();
            var id3 = command.Execute();

            // Assert
            Assert.AreNotEqual(id1, id2, "IDs should be unique");
            Assert.AreNotEqual(id2, id3, "IDs should be unique");
            Assert.AreEqual(
                3,
                _architecture.GetModel<EntityModel>().World.EntityCount,
                "Should have 3 entities"
            );
        }

        #endregion

        #region DestroyEntityCommand

        [Test]
        [Category("Unit")]
        public void DestroyEntityCommand_Execute_RemovesEntityFromWorld()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();
            int countBeforeDestroy = _architecture.GetModel<EntityModel>().World.EntityCount;

            var destroyCmd = new DestroyEntityCommand(entityId);
            destroyCmd.Create(_architecture);

            // Act
            destroyCmd.Execute();

            // Assert
            Assert.AreEqual(
                countBeforeDestroy - 1,
                _architecture.GetModel<EntityModel>().World.EntityCount,
                "Entity should be removed"
            );
        }

        [Test]
        [Category("Unit")]
        public void DestroyEntityCommand_Execute_WithInvalidId_DoesNothing()
        {
            // Arrange
            var invalidId = new EntityId(999, 0);
            var command = new DestroyEntityCommand(invalidId);
            command.Create(_architecture);
            int countBefore = _architecture.GetModel<EntityModel>().World.EntityCount;

            // Act
            command.Execute();

            // Assert
            Assert.AreEqual(
                countBefore,
                _architecture.GetModel<EntityModel>().World.EntityCount,
                "Count should not change"
            );
        }

        #endregion

        #region AddComponentCommand

        [Test]
        [Category("Unit")]
        public void AddComponentCommand_Execute_AddsComponentToEntity()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var addCmd = new AddComponentCommand<TestCommandComponent>(entityId);
            addCmd.Create(_architecture);

            // Act
            var component = addCmd.Execute();

            // Assert
            Assert.IsNotNull(component, "Should return component");
            var entity = _architecture.GetModel<EntityModel>().World.GetEntity(entityId);
            Assert.IsTrue(
                entity.HasComponent<TestCommandComponent>(),
                "Entity should have component"
            );
        }

        [Test]
        [Category("Unit")]
        public void AddComponentCommand_Execute_WithInvalidEntityId_ReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            // Arrange
            var invalidId = new EntityId(999, 0);
            var command = new AddComponentCommand<TestCommandComponent>(invalidId);
            command.Create(_architecture);

            // Act
            var component = command.Execute();

            // Assert
            Assert.IsNull(component, "Should return null for invalid entity");
        }

        [Test]
        [Category("Unit")]
        public void AddComponentCommand_Undo_RemovesComponentFromEntity()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var addCmd = new AddComponentCommand<TestCommandComponent>(entityId);
            addCmd.Create(_architecture);
            addCmd.Execute();

            // Act
            addCmd.Undo();

            // Assert
            var entity = _architecture.GetModel<EntityModel>().World.GetEntity(entityId);
            Assert.IsFalse(
                entity.HasComponent<TestCommandComponent>(),
                "Component should be removed after undo"
            );
        }

        [Test]
        [Category("Unit")]
        public void AddComponentCommand_CanUndo_ReturnsTrue()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var command = new AddComponentCommand<TestCommandComponent>(entityId);
            command.Create(_architecture);

            // Act & Assert
            Assert.IsTrue(command.CanUndo, "Should be able to undo");
        }

        #endregion

        #region RemoveComponentCommand

        [Test]
        [Category("Unit")]
        public void RemoveComponentCommand_Execute_RemovesComponentFromEntity()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var entity = _architecture.GetModel<EntityModel>().World.GetEntity(entityId);
            entity.AddComponent<TestCommandComponent>();

            var removeCmd = new RemoveComponentCommand<TestCommandComponent>(entityId);
            removeCmd.Create(_architecture);

            // Act
            removeCmd.Execute();

            // Assert
            Assert.IsFalse(
                entity.HasComponent<TestCommandComponent>(),
                "Component should be removed"
            );
        }

        [Test]
        [Category("Unit")]
        public void RemoveComponentCommand_Undo_ReAddsComponentToEntity()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var entity = _architecture.GetModel<EntityModel>().World.GetEntity(entityId);
            var originalComponent = entity.AddComponent<TestCommandComponent>();
            originalComponent.Value = 42;

            var removeCmd = new RemoveComponentCommand<TestCommandComponent>(entityId);
            removeCmd.Create(_architecture);
            removeCmd.Execute();

            // Act
            removeCmd.Undo();

            // Assert
            Assert.IsTrue(
                entity.HasComponent<TestCommandComponent>(),
                "Component should be re-added"
            );
        }

        [Test]
        [Category("Unit")]
        public void RemoveComponentCommand_CanUndo_ReturnsTrue()
        {
            // Arrange
            var createCmd = new CreateEntityCommand();
            createCmd.Create(_architecture);
            var entityId = createCmd.Execute();

            var command = new RemoveComponentCommand<TestCommandComponent>(entityId);
            command.Create(_architecture);

            // Act & Assert
            Assert.IsTrue(command.CanUndo, "Should be able to undo");
        }

        #endregion
    }

    /// <summary>
    /// 测试用架构
    /// </summary>
    public class TestArchitecture : AsakiArchitecture
    {
        protected override void OnSetup()
        {
            RegisterModel(new EntityModel());
        }
    }

    /// <summary>
    /// 测试用组件
    /// </summary>
    public class TestCommandComponent : IEntityComponent
    {
        public IEntity Entity { get; set; }
        public int Value { get; set; }

        public void OnAttach() { }

        public void OnDetach() { }

        public void OnEnable() { }

        public void OnDisable() { }

        public void Dispose() { }
    }
}
