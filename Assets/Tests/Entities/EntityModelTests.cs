using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Entities;
using NUnit.Framework;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// EntityModel 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class EntityModelTests
    {
        private EntityModel _model;

        [SetUp]
        public void Setup()
        {
            _model = new EntityModel();
        }

        [TearDown]
        public void Teardown()
        {
            _model?.Dispose();
            _model = null;
        }

        #region Initialization

        [Test]
        [Category("Unit")]
        public void Create_WhenCalled_CreatesWorld()
        {
            // Arrange
            var model = new EntityModel();

            // Act
            model.Create();

            // Assert
            Assert.IsNotNull(model.World, "World should be created");
        }

        [Test]
        [Category("Unit")]
        public void Create_WhenCalledMultipleTimes_CreatesNewWorldEachTime()
        {
            // Arrange
            var model = new EntityModel();

            // Act
            model.Create();
            var world1 = model.World;
            model.Create();
            var world2 = model.World;

            // Assert
            Assert.IsNotNull(world1, "First world should exist");
            Assert.IsNotNull(world2, "Second world should exist");
            Assert.AreNotSame(world1, world2, "Should create new world instance each time");
        }

        [Test]
        [Category("Unit")]
        public void World_BeforeCreate_IsNull()
        {
            // Arrange
            var model = new EntityModel();

            // Act & Assert
            Assert.IsNull(model.World, "World should be null before Create()");
        }

        #endregion

        #region World Access

        [Test]
        [Category("Unit")]
        public void World_AfterCreate_CanCreateEntities()
        {
            // Arrange
            _model.Create();

            // Act
            var entity = _model.World.CreateEntity();

            // Assert
            Assert.IsNotNull(entity, "Should be able to create entity");
            Assert.IsTrue(entity.Id.IsValid, "Entity should have valid ID");
        }

        [Test]
        [Category("Unit")]
        public void World_AfterCreate_ReturnsSameInstance()
        {
            // Arrange
            _model.Create();

            // Act
            var world1 = _model.World;
            var world2 = _model.World;

            // Assert
            Assert.AreSame(world1, world2, "Should return same world instance");
        }

        #endregion

        #region Disposal

        [Test]
        [Category("Unit")]
        public void Dispose_WhenCalled_DisposesWorld()
        {
            // Arrange
            _model.Create();
            var entity = _model.World.CreateEntity();
            var id = entity.Id;

            // Act
            _model.Dispose();

            // Assert
            // After disposal, creating a new model and world should work
            var newModel = new EntityModel();
            newModel.Create();
            var newEntity = newModel.World.CreateEntity();
            Assert.IsTrue(newEntity.Id.IsValid, "New world should work after disposal");
            newModel.Dispose();
        }

        [Test]
        [Category("Unit")]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            _model.Create();

            // Act & Assert
            Assert.DoesNotThrow(
                () =>
                {
                    _model.Dispose();
                    _model.Dispose();
                },
                "Should not throw on multiple dispose"
            );
        }

        [Test]
        [Category("Unit")]
        public void Dispose_WhenWorldIsNull_DoesNotThrow()
        {
            // Arrange
            var model = new EntityModel(); // Not calling Create()

            // Act & Assert
            Assert.DoesNotThrow(() => model.Dispose(), "Should not throw when world is null");
        }

        [Test]
        [Category("Unit")]
        public void Dispose_WhenCalled_SetsWorldToNull()
        {
            // Arrange
            _model.Create();
            Assert.IsNotNull(_model.World, "World should exist before dispose");

            // Act
            _model.Dispose();

            // Assert
            Assert.IsNull(_model.World, "World should be null after dispose");
        }

        #endregion

        #region Integration with IAsakiModel

        [Test]
        [Category("Unit")]
        public void EntityModel_ImplementsIAsakiModel()
        {
            // Arrange
            var model = new EntityModel();

            // Act & Assert
            Assert.IsInstanceOf<IAsakiModel>(model, "EntityModel should implement IAsakiModel");
        }

        [Test]
        [Category("Unit")]
        public void EntityModel_ImplementsIDisposable()
        {
            // Arrange
            var model = new EntityModel();

            // Act & Assert
            Assert.IsInstanceOf<System.IDisposable>(
                model,
                "EntityModel should implement IDisposable"
            );
        }

        #endregion
    }
}
