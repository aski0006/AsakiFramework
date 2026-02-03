using System.Collections.Generic;
using System.Diagnostics;
using Asaki.Core.Collections;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Asaki.Tests.Entities
{
    /// <summary>
    /// 魔法容器性能测试
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class MagicContainerPerformanceTests
    {
        private const int TestCount = 10000;

        [Test]
        [Category("Performance")]
        public void Performance_Add_MagicContainerVsDictionary()
        {
            // Arrange
            var magicContainer = new MagicContainer<PerfTestItem>();
            var dictionary = new Dictionary<int, PerfTestItem>();
            var items = new List<PerfTestItem>();

            for (int i = 0; i < TestCount; i++)
            {
                items.Add(new PerfTestItem { Id = i, Value = i * 0.5f });
            }

            // Act - MagicContainer
            var sw1 = Stopwatch.StartNew();
            var magicHandles = new int[TestCount];
            for (int i = 0; i < TestCount; i++)
            {
                magicHandles[i] = magicContainer.Add(items[i]);
            }
            sw1.Stop();

            // Act - Dictionary
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < TestCount; i++)
            {
                dictionary[i] = items[i];
            }
            sw2.Stop();

            // Log results
            Debug.Log(
                $"[Add {TestCount} items] MagicContainer: {sw1.ElapsedMilliseconds}ms, Dictionary: {sw2.ElapsedMilliseconds}ms"
            );

            // Assert - Both should complete in reasonable time
            Assert.Less(sw1.ElapsedMilliseconds, 1000, "MagicContainer add should be fast");
            Assert.Less(sw2.ElapsedMilliseconds, 1000, "Dictionary add should be fast");
        }

        [Test]
        [Category("Performance")]
        public void Performance_Traversal_MagicContainerVsDictionary()
        {
            // Arrange
            var magicContainer = new MagicContainer<PerfTestItem>();
            var dictionary = new Dictionary<int, PerfTestItem>();

            for (int i = 0; i < TestCount; i++)
            {
                var item = new PerfTestItem { Id = i, Value = i * 0.5f };
                magicContainer.Add(item);
                dictionary[i] = item;
            }

            long magicSum = 0;
            long dictSum = 0;

            // Act - MagicContainer (continuous memory)
            var sw1 = Stopwatch.StartNew();
            magicContainer.ForEach(item => magicSum += item.Id);
            sw1.Stop();

            // Act - Dictionary (random access)
            var sw2 = Stopwatch.StartNew();
            foreach (var pair in dictionary)
            {
                dictSum += pair.Value.Id;
            }
            sw2.Stop();

            // Log results
            Debug.Log(
                $"[Traverse {TestCount} items] MagicContainer: {sw1.ElapsedMilliseconds}ms, Dictionary: {sw2.ElapsedMilliseconds}ms"
            );
            Debug.Log(
                $"Sum verification: Magic={magicSum}, Dict={dictSum}, Equal={magicSum == dictSum}"
            );

            // Assert - Results should be equal
            Assert.AreEqual(magicSum, dictSum, "Sums should be equal");

            // MagicContainer should generally be faster for traversal due to cache locality
            Debug.Log(
                $"Traversal speedup: {sw2.ElapsedMilliseconds / (float)sw1.ElapsedMilliseconds:F2}x"
            );
        }

        [Test]
        [Category("Performance")]
        public void Performance_RandomAccess_MagicContainerVsDictionary()
        {
            // Arrange
            var magicContainer = new MagicContainer<PerfTestItem>();
            var dictionary = new Dictionary<int, PerfTestItem>();
            var handles = new int[TestCount];

            for (int i = 0; i < TestCount; i++)
            {
                var item = new PerfTestItem { Id = i, Value = i * 0.5f };
                handles[i] = magicContainer.Add(item);
                dictionary[i] = item;
            }

            long magicSum = 0;
            long dictSum = 0;

            // Act - MagicContainer random access
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < TestCount; i++)
            {
                var item = magicContainer.Get(handles[i]);
                if (item != null)
                    magicSum += item.Id;
            }
            sw1.Stop();

            // Act - Dictionary random access
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < TestCount; i++)
            {
                if (dictionary.TryGetValue(i, out var item))
                {
                    dictSum += item.Id;
                }
            }
            sw2.Stop();

            // Log results
            Debug.Log(
                $"[Random Access {TestCount} items] MagicContainer: {sw1.ElapsedMilliseconds}ms, Dictionary: {sw2.ElapsedMilliseconds}ms"
            );

            // Assert
            Assert.AreEqual(magicSum, dictSum, "Sums should be equal");
        }

        [Test]
        [Category("Performance")]
        public void Performance_Remove_MagicContainerVsDictionary()
        {
            // Arrange
            var magicContainer = new MagicContainer<PerfTestItem>();
            var dictionary = new Dictionary<int, PerfTestItem>();
            var magicHandles = new List<int>();
            var dictKeys = new List<int>();

            for (int i = 0; i < TestCount; i++)
            {
                var item = new PerfTestItem { Id = i };
                magicHandles.Add(magicContainer.Add(item));
                dictionary[i] = item;
                dictKeys.Add(i);
            }

            // Act - MagicContainer remove
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < TestCount / 2; i++)
            {
                magicContainer.Remove(magicHandles[i]);
            }
            sw1.Stop();

            // Act - Dictionary remove
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < TestCount / 2; i++)
            {
                dictionary.Remove(dictKeys[i]);
            }
            sw2.Stop();

            // Log results
            Debug.Log(
                $"[Remove {TestCount / 2} items] MagicContainer: {sw1.ElapsedMilliseconds}ms, Dictionary: {sw2.ElapsedMilliseconds}ms"
            );

            // Assert
            Assert.AreEqual(
                TestCount / 2,
                magicContainer.Count,
                "MagicContainer should have half remaining"
            );
            Assert.AreEqual(
                TestCount / 2,
                dictionary.Count,
                "Dictionary should have half remaining"
            );
        }

        [Test]
        [Category("Performance")]
        public void Performance_MemoryLayout_ContiguousVsScattered()
        {
            // This test demonstrates the memory layout benefits of MagicContainer
            // In real scenarios, MagicContainer's contiguous memory provides better cache locality

            // Arrange
            var magicContainer = new MagicContainer<PerfTestItem>();

            for (int i = 0; i < TestCount; i++)
            {
                magicContainer.Add(new PerfTestItem { Id = i, Value = i * 0.5f });
            }

            // Act - Sequential access (cache-friendly)
            var sw1 = Stopwatch.StartNew();
            long sum1 = 0;
            for (int i = 0; i < magicContainer.Capacity; i++)
            {
                var item = magicContainer.GetAt(i);
                if (item != null)
                {
                    sum1 += item.Id;
                }
            }
            sw1.Stop();

            // Act - Random access (less cache-friendly)
            var sw2 = Stopwatch.StartNew();
            long sum2 = 0;
            var random = new System.Random(42);
            for (int i = 0; i < TestCount; i++)
            {
                int index = random.Next(magicContainer.Capacity);
                var item = magicContainer.GetAt(index);
                if (item != null)
                {
                    sum2 += item.Id;
                }
            }
            sw2.Stop();

            // Log results
            Debug.Log(
                $"[Access Pattern] Sequential: {sw1.ElapsedMilliseconds}ms, Random: {sw2.ElapsedMilliseconds}ms"
            );
            Debug.Log($"Sequential access is typically faster due to cache prefetching");

            // Assert - Just verify the test runs without error
            Assert.Pass();
        }
    }

    /// <summary>
    /// 性能测试用数据类
    /// </summary>
    public class PerfTestItem
    {
        public int Id { get; set; }
        public float Value { get; set; }

        // Add some padding to simulate real-world object size
        public byte[] Padding = new byte[64];
    }
}
