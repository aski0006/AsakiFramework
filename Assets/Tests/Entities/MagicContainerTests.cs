using System;
using System.Collections.Generic;
using Asaki.Core.Collections;
using NUnit.Framework;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// 魔法容器单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class MagicContainerTests
    {
        private MagicContainer<TestItem> _container;

        [SetUp]
        public void Setup()
        {
            _container = new MagicContainer<TestItem>();
        }

        [TearDown]
        public void Teardown()
        {
            _container?.Clear();
            _container = null;
        }

        #region Basic Operations

        [Test]
        [Category("Unit")]
        public void Add_WhenItemIsValid_ReturnsValidHandle()
        {
            // Arrange
            var item = new TestItem { Id = 1, Name = "Test" };

            // Act
            int handle = _container.Add(item);

            // Assert
            Assert.GreaterOrEqual(handle, 0, "Handle should be non-negative");
            Assert.AreEqual(1, _container.Count, "Count should be 1");
        }

        [Test]
        [Category("Unit")]
        public void Add_WhenItemIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            TestItem nullItem = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Add(nullItem));
        }

        [Test]
        [Category("Unit")]
        public void Get_WhenHandleIsValid_ReturnsCorrectItem()
        {
            // Arrange
            var item = new TestItem { Id = 1, Name = "Test" };
            int handle = _container.Add(item);

            // Act
            var retrieved = _container.Get(handle);

            // Assert
            Assert.IsNotNull(retrieved, "Retrieved item should not be null");
            Assert.AreEqual(item.Id, retrieved.Id, "Id should match");
            Assert.AreEqual(item.Name, retrieved.Name, "Name should match");
        }

        [Test]
        [Category("Unit")]
        public void Get_WhenHandleIsInvalid_ReturnsNull()
        {
            // Arrange
            int invalidHandle = -1;

            // Act
            var result = _container.Get(invalidHandle);

            // Assert
            Assert.IsNull(result, "Should return null for invalid handle");
        }

        [Test]
        [Category("Unit")]
        public void Get_WhenHandleWasRemoved_ReturnsNull()
        {
            // Arrange
            var item = new TestItem { Id = 1 };
            int handle = _container.Add(item);
            _container.Remove(handle);

            // Act
            var result = _container.Get(handle);

            // Assert
            Assert.IsNull(result, "Should return null for removed handle");
        }

        [Test]
        [Category("Unit")]
        public void TryGet_WhenHandleIsValid_ReturnsTrueAndItem()
        {
            // Arrange
            var item = new TestItem { Id = 1 };
            int handle = _container.Add(item);

            // Act
            bool success = _container.TryGet(handle, out var retrieved);

            // Assert
            Assert.IsTrue(success, "Should return true for valid handle");
            Assert.IsNotNull(retrieved, "Should return item");
            Assert.AreEqual(item.Id, retrieved.Id, "Item should match");
        }

        [Test]
        [Category("Unit")]
        public void TryGet_WhenHandleIsInvalid_ReturnsFalseAndNull()
        {
            // Arrange
            int invalidHandle = 999;

            // Act
            bool success = _container.TryGet(invalidHandle, out var retrieved);

            // Assert
            Assert.IsFalse(success, "Should return false for invalid handle");
            Assert.IsNull(retrieved, "Should return null for invalid handle");
        }

        #endregion

        #region Remove Operations

        [Test]
        [Category("Unit")]
        public void Remove_WhenHandleIsValid_ReturnsTrueAndDecrementsCount()
        {
            // Arrange
            var item = new TestItem { Id = 1 };
            int handle = _container.Add(item);
            int initialCount = _container.Count;

            // Act
            bool result = _container.Remove(handle);

            // Assert
            Assert.IsTrue(result, "Should return true for valid handle");
            Assert.AreEqual(initialCount - 1, _container.Count, "Count should decrement");
        }

        [Test]
        [Category("Unit")]
        public void Remove_WhenHandleIsInvalid_ReturnsFalse()
        {
            // Arrange
            int invalidHandle = -1;

            // Act
            bool result = _container.Remove(invalidHandle);

            // Assert
            Assert.IsFalse(result, "Should return false for invalid handle");
        }

        [Test]
        [Category("Unit")]
        public void Remove_WhenCalledMultipleTimesOnSameHandle_ReturnsFalseOnSecondCall()
        {
            // Arrange
            var item = new TestItem { Id = 1 };
            int handle = _container.Add(item);
            _container.Remove(handle);

            // Act
            bool secondRemove = _container.Remove(handle);

            // Assert
            Assert.IsFalse(
                secondRemove,
                "Should return false when removing already removed handle"
            );
        }

        [Test]
        [Category("Unit")]
        public void Remove_WithSwapToEnd_MaintainsDataIntegrity()
        {
            // Arrange
            var items = new List<TestItem>();
            var handles = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                var item = new TestItem { Id = i };
                items.Add(item);
                handles.Add(_container.Add(item));
            }

            // Act - Remove middle item (should trigger swap)
            _container.Remove(handles[2]);

            // Assert - Verify remaining items are intact
            Assert.AreEqual(4, _container.Count, "Count should be 4");

            // The last item should now be accessible at the removed position
            var lastItem = _container.Get(handles[4]);
            Assert.IsNotNull(lastItem, "Last item should still be accessible via its handle");
            Assert.AreEqual(4, lastItem.Id, "Last item data should be intact");
        }

        #endregion

        #region Handle Reuse

        [Test]
        [Category("Unit")]
        public void Add_AfterRemove_ReusesHandle()
        {
            // Arrange
            var item1 = new TestItem { Id = 1 };
            int handle1 = _container.Add(item1);
            _container.Remove(handle1);

            // Act
            var item2 = new TestItem { Id = 2 };
            int handle2 = _container.Add(item2);

            // Assert
            Assert.AreEqual(handle1, handle2, "Should reuse the freed handle");
        }

        #endregion

        #region Bulk Operations

        [Test]
        [Category("Unit")]
        public void ForEach_WhenContainerHasItems_ProcessesAllItems()
        {
            // Arrange
            var processedIds = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                _container.Add(new TestItem { Id = i });
            }

            // Act
            _container.ForEach(item => processedIds.Add(item.Id));

            // Assert
            Assert.AreEqual(5, processedIds.Count, "Should process all 5 items");
            for (int i = 0; i < 5; i++)
            {
                Assert.Contains(i, processedIds, $"Should contain item with Id {i}");
            }
        }

        [Test]
        [Category("Unit")]
        public void ForEach_WithIndex_ProvidesCorrectIndices()
        {
            // Arrange
            var indices = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                _container.Add(new TestItem { Id = i });
            }

            // Act
            _container.ForEach((index, item) => indices.Add(index));

            // Assert
            Assert.AreEqual(3, indices.Count, "Should have 3 indices");
            Assert.AreEqual(0, indices[0], "First index should be 0");
            Assert.AreEqual(1, indices[1], "Second index should be 1");
            Assert.AreEqual(2, indices[2], "Third index should be 2");
        }

        [Test]
        [Category("Unit")]
        public void Find_WhenItemExists_ReturnsMatchingItem()
        {
            // Arrange
            _container.Add(new TestItem { Id = 1, Name = "Alice" });
            _container.Add(new TestItem { Id = 2, Name = "Bob" });
            _container.Add(new TestItem { Id = 3, Name = "Charlie" });

            // Act
            var result = _container.Find(item => item.Name == "Bob");

            // Assert
            Assert.IsNotNull(result, "Should find the item");
            Assert.AreEqual(2, result.Id, "Should return correct item");
        }

        [Test]
        [Category("Unit")]
        public void Find_WhenItemDoesNotExist_ReturnsNull()
        {
            // Arrange
            _container.Add(new TestItem { Id = 1, Name = "Alice" });

            // Act
            var result = _container.Find(item => item.Name == "Bob");

            // Assert
            Assert.IsNull(result, "Should return null when item not found");
        }

        [Test]
        [Category("Unit")]
        public void FindAll_WhenMultipleMatches_ReturnsAllMatches()
        {
            // Arrange
            _container.Add(new TestItem { Id = 1, Category = "A" });
            _container.Add(new TestItem { Id = 2, Category = "B" });
            _container.Add(new TestItem { Id = 3, Category = "A" });

            // Act
            var results = _container.FindAll(item => item.Category == "A");

            // Assert
            Assert.AreEqual(2, results.Count, "Should find 2 items");
        }

        [Test]
        [Category("Unit")]
        public void Exists_WhenItemExists_ReturnsTrue()
        {
            // Arrange
            _container.Add(new TestItem { Id = 1, Name = "Test" });

            // Act
            bool exists = _container.Exists(item => item.Name == "Test");

            // Assert
            Assert.IsTrue(exists, "Should return true when item exists");
        }

        [Test]
        [Category("Unit")]
        public void Exists_WhenItemDoesNotExist_ReturnsFalse()
        {
            // Arrange
            _container.Add(new TestItem { Id = 1, Name = "Test" });

            // Act
            bool exists = _container.Exists(item => item.Name == "Other");

            // Assert
            Assert.IsFalse(exists, "Should return false when item doesn't exist");
        }

        #endregion

        #region Clear Operations

        [Test]
        [Category("Unit")]
        public void Clear_WhenContainerHasItems_RemovesAllItems()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                _container.Add(new TestItem { Id = i });
            }

            // Act
            _container.Clear();

            // Assert
            Assert.AreEqual(0, _container.Count, "Count should be 0");
            Assert.IsTrue(_container.IsEmpty, "IsEmpty should be true");
        }

        [Test]
        [Category("Unit")]
        public void Clear_WhenCalled_OldHandlesBecomeInvalid()
        {
            // Arrange
            int handle = _container.Add(new TestItem { Id = 1 });

            // Act
            _container.Clear();
            var item = _container.Get(handle);

            // Assert
            Assert.IsNull(item, "Old handle should be invalid after clear");
        }

        #endregion

        #region Capacity Tests

        [Test]
        [Category("Unit")]
        public void Capacity_AfterAddingItems_ReturnsCorrectCapacity()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                _container.Add(new TestItem { Id = i });
            }

            // Act
            int capacity = _container.Capacity;

            // Assert
            Assert.AreEqual(10, capacity, "Capacity should match added items");
        }

        [Test]
        [Category("Unit")]
        public void Capacity_AfterRemoveAndAdd_ReusesSpace()
        {
            // Arrange
            int handle = _container.Add(new TestItem { Id = 1 });
            _container.Remove(handle);

            // Act
            _container.Add(new TestItem { Id = 2 });

            // Assert
            Assert.AreEqual(1, _container.Capacity, "Capacity should remain 1 after reuse");
        }

        #endregion

        #region Edge Cases

        [Test]
        [Category("Unit")]
        public void GetAt_WhenIndexIsValid_ReturnsItem()
        {
            // Arrange
            var item = new TestItem { Id = 1 };
            _container.Add(item);

            // Act
            var result = _container.GetAt(0);

            // Assert
            Assert.IsNotNull(result, "Should return item at index 0");
            Assert.AreEqual(1, result.Id, "Should return correct item");
        }

        [Test]
        [Category("Unit")]
        public void GetAt_WhenIndexIsInvalid_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _container.GetAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _container.GetAt(999));
        }

        [Test]
        [Category("Unit")]
        public void IsValidHandle_WhenHandleIsValid_ReturnsTrue()
        {
            // Arrange
            int handle = _container.Add(new TestItem { Id = 1 });

            // Act
            bool isValid = _container.IsValidHandle(handle);

            // Assert
            Assert.IsTrue(isValid, "Valid handle should return true");
        }

        [Test]
        [Category("Unit")]
        public void IsValidHandle_WhenHandleIsRemoved_ReturnsFalse()
        {
            // Arrange
            int handle = _container.Add(new TestItem { Id = 1 });
            _container.Remove(handle);

            // Act
            bool isValid = _container.IsValidHandle(handle);

            // Assert
            Assert.IsFalse(isValid, "Removed handle should return false");
        }

        [Test]
        [Category("Unit")]
        public void IsValidHandle_WhenHandleIsNeverCreated_ReturnsFalse()
        {
            // Act
            bool isValid = _container.IsValidHandle(999);

            // Assert
            Assert.IsFalse(isValid, "Never-created handle should return false");
        }

        #endregion

        #region Enumeration

        [Test]
        [Category("Unit")]
        public void GetEnumerator_WhenUsedInForeach_IteratesAllItems()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                _container.Add(new TestItem { Id = i });
            }

            // Act
            var ids = new List<int>();
            foreach (var item in _container)
            {
                ids.Add(item.Id);
            }

            // Assert
            Assert.AreEqual(3, ids.Count, "Should iterate all 3 items");
        }

        #endregion
    }

    /// <summary>
    /// 测试用数据类
    /// </summary>
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
