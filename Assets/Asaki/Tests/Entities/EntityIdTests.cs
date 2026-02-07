using Asaki.Core.Architecture.Entities;
using NUnit.Framework;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// EntityId 结构体单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class EntityIdTests
    {
        #region Construction

        [Test]
        [Category("Unit")]
        public void Constructor_WithValidHandleAndGeneration_SetsProperties()
        {
            // Arrange
            int handle = 5;
            int generation = 3;

            // Act
            var entityId = new EntityId(handle, generation);

            // Assert
            Assert.AreEqual(handle, entityId.Handle, "Handle should match");
            Assert.AreEqual(generation, entityId.Generation, "Generation should match");
        }

        [Test]
        [Category("Unit")]
        public void Constructor_WithZeroValues_CreatesValidId()
        {
            // Act
            var entityId = new EntityId(0, 0);

            // Assert
            Assert.AreEqual(0, entityId.Handle, "Handle should be 0");
            Assert.AreEqual(0, entityId.Generation, "Generation should be 0");
            Assert.IsTrue(entityId.IsValid, "Should be valid with handle 0");
        }

        #endregion

        #region IsValid Property

        [Test]
        [Category("Unit")]
        public void IsValid_WhenHandleIsNonNegative_ReturnsTrue()
        {
            // Arrange
            var validId = new EntityId(0, 0);
            var validId2 = new EntityId(1, 5);

            // Assert
            Assert.IsTrue(validId.IsValid, "Handle 0 should be valid");
            Assert.IsTrue(validId2.IsValid, "Positive handle should be valid");
        }

        [Test]
        [Category("Unit")]
        public void IsValid_WhenHandleIsNegative_ReturnsFalse()
        {
            // Arrange
            var invalidId = new EntityId(-1, 0);

            // Assert
            Assert.IsFalse(invalidId.IsValid, "Negative handle should be invalid");
        }

        [Test]
        [Category("Unit")]
        public void Invalid_StaticProperty_HasNegativeHandle()
        {
            // Act
            var invalid = EntityId.Invalid;

            // Assert
            Assert.AreEqual(-1, invalid.Handle, "Invalid handle should be -1");
            Assert.AreEqual(0, invalid.Generation, "Invalid generation should be 0");
            Assert.IsFalse(invalid.IsValid, "Invalid should not be valid");
        }

        #endregion

        #region Equality

        [Test]
        [Category("Unit")]
        public void Equals_WhenSameHandleAndGeneration_ReturnsTrue()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(5, 3);

            // Act
            bool areEqual = id1.Equals(id2);

            // Assert
            Assert.IsTrue(areEqual, "Same handle and generation should be equal");
        }

        [Test]
        [Category("Unit")]
        public void Equals_WhenDifferentHandle_ReturnsFalse()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(6, 3);

            // Act
            bool areEqual = id1.Equals(id2);

            // Assert
            Assert.IsFalse(areEqual, "Different handles should not be equal");
        }

        [Test]
        [Category("Unit")]
        public void Equals_WhenDifferentGeneration_ReturnsFalse()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(5, 4);

            // Act
            bool areEqual = id1.Equals(id2);

            // Assert
            Assert.IsFalse(areEqual, "Different generations should not be equal");
        }

        [Test]
        [Category("Unit")]
        public void Equals_WhenComparedToObject_ReturnsCorrectResult()
        {
            // Arrange
            var id = new EntityId(5, 3);
            object sameId = new EntityId(5, 3);
            object differentId = new EntityId(5, 4);
            object nonEntityId = "not an entity id";

            // Act & Assert
            Assert.IsTrue(id.Equals(sameId), "Should equal same EntityId as object");
            Assert.IsFalse(id.Equals(differentId), "Should not equal different EntityId as object");
            Assert.IsFalse(id.Equals(nonEntityId), "Should not equal non-EntityId object");
        }

        [Test]
        [Category("Unit")]
        public void EqualsOperator_WhenSameValues_ReturnsTrue()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(5, 3);

            // Act
            bool areEqual = id1 == id2;

            // Assert
            Assert.IsTrue(areEqual, "== should return true for same values");
        }

        [Test]
        [Category("Unit")]
        public void NotEqualsOperator_WhenDifferentValues_ReturnsTrue()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(6, 3);

            // Act
            bool areNotEqual = id1 != id2;

            // Assert
            Assert.IsTrue(areNotEqual, "!= should return true for different values");
        }

        [Test]
        [Category("Unit")]
        public void GetHashCode_WhenSameValues_ReturnsSameHash()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(5, 3);

            // Act
            int hash1 = id1.GetHashCode();
            int hash2 = id2.GetHashCode();

            // Assert
            Assert.AreEqual(hash1, hash2, "Same values should have same hash code");
        }

        [Test]
        [Category("Unit")]
        public void GetHashCode_WhenDifferentValues_ReturnsDifferentHash()
        {
            // Arrange
            var id1 = new EntityId(5, 3);
            var id2 = new EntityId(6, 3);
            var id3 = new EntityId(5, 4);

            // Act
            int hash1 = id1.GetHashCode();
            int hash2 = id2.GetHashCode();
            int hash3 = id3.GetHashCode();

            // Assert
            Assert.AreNotEqual(hash1, hash2, "Different handles should have different hash codes");
            Assert.AreNotEqual(
                hash1,
                hash3,
                "Different generations should have different hash codes"
            );
        }

        #endregion

        #region ToString

        [Test]
        [Category("Unit")]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var id = new EntityId(5, 3);

            // Act
            string result = id.ToString();

            // Assert
            Assert.AreEqual("Entity(5:3)", result, "ToString should return formatted entity id");
        }

        [Test]
        [Category("Unit")]
        public void ToString_ForInvalidId_ReturnsFormattedString()
        {
            // Arrange
            var invalidId = EntityId.Invalid;

            // Act
            string result = invalidId.ToString();

            // Assert
            Assert.AreEqual("Entity(-1:0)", result, "ToString should work for invalid id");
        }

        #endregion

        #region Usage in Collections

        [Test]
        [Category("Unit")]
        public void EntityId_CanBeUsedAsDictionaryKey()
        {
            // Arrange
            var dictionary = new System.Collections.Generic.Dictionary<EntityId, string>();
            var id1 = new EntityId(1, 0);
            var id2 = new EntityId(2, 0);

            // Act
            dictionary[id1] = "Entity1";
            dictionary[id2] = "Entity2";

            // Assert
            Assert.AreEqual("Entity1", dictionary[id1], "Should retrieve correct value");
            Assert.AreEqual("Entity2", dictionary[id2], "Should retrieve correct value");
        }

        [Test]
        [Category("Unit")]
        public void EntityId_CanBeUsedInHashSet()
        {
            // Arrange
            var hashSet = new System.Collections.Generic.HashSet<EntityId>();
            var id1 = new EntityId(1, 0);
            var id2 = new EntityId(1, 0); // Same value

            // Act
            hashSet.Add(id1);
            hashSet.Add(id2);

            // Assert
            Assert.AreEqual(1, hashSet.Count, "HashSet should contain only 1 item for same value");
        }

        #endregion
    }
}
