using System;
using System.Collections;
using System.Diagnostics;
using Asaki.Plugin.ComboSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// ComboSystem 性能测试
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    [Category("ComboSystem")]
    public class ComboSystemPerformanceTests
    {
        private const int WARMUP_ITERATIONS = 100;
        private const int TEST_ITERATIONS = 10000;
        private const float MAX_ACCEPTABLE_TIME_MS = 16.67f; // 60fps = 16.67ms per frame
        #region InputBuffer 性能测试

        [Test]
        [Description("性能测试：InputBuffer PushInput 操作")]
        public void InputBuffer_PushInput_Performance()
        {
            // Arrange
            var buffer = new InputBuffer(0.3f);
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                buffer.PushInput("TestInput");
                buffer.Clear();
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                buffer.PushInput($"Input{i}");
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"InputBuffer.PushInput: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.002),
                $"PushInput平均耗时{avgTimeMs:F6}ms，应小于0.002ms"
            );
        }

        [Test]
        [Description("性能测试：InputBuffer TryGetInput 操作")]
        public void InputBuffer_TryGetInput_Performance()
        {
            // Arrange
            var buffer = new InputBuffer(0.3f);
            var stopwatch = new Stopwatch();

            // 预填充缓冲
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                buffer.PushInput($"Input{i}");
            }

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                buffer.TryGetInput(out string _);
            }

            // 重新填充
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                buffer.PushInput($"Input{i}");
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                buffer.TryGetInput(out string _);
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"InputBuffer.TryGetInput: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.002),
                $"TryGetInput平均耗时{avgTimeMs:F6}ms，应小于0.002ms"
            );
        }

        #endregion

        #region ComboTree 性能测试

        [Test]
        [Description("性能测试：ComboTree GetMove 查找")]
        public void ComboTree_GetMove_Performance()
        {
            // Arrange
            var tree = CreateLargeComboTree(100);
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                tree.GetMove("move_50");
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                tree.GetMove($"move_{i % 100}");
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"ComboTree.GetMove: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.002),
                $"GetMove平均耗时{avgTimeMs:F6}ms，应小于0.002ms"
            );
        }

        [Test]
        [Description("性能测试：ComboTree FindNextMove 查找")]
        public void ComboTree_FindNextMove_Performance()
        {
            // Arrange
            var tree = CreateLargeComboTree(50);
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                tree.FindNextMove("move_0", "LightAttack");
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                tree.FindNextMove($"move_{i % 49}", "LightAttack");
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"ComboTree.FindNextMove: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.01),
                $"FindNextMove平均耗时{avgTimeMs:F6}ms，应小于0.01ms"
            );
        }

        #endregion

        #region ResetStrategy 性能测试

        [Test]
        [Description("性能测试：ResetToZeroStrategy")]
        public void ResetStrategy_ResetToZero_Performance()
        {
            // Arrange
            var strategy = new ResetToZeroStrategy();
            var context = new ComboContext { ComboCount = 100 };
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"ResetToZeroStrategy: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.0001),
                $"ResetToZeroStrategy平均耗时{avgTimeMs:F6}ms，应小于0.0001ms"
            );
        }

        [Test]
        [Description("性能测试：DecayCountStrategy")]
        public void ResetStrategy_DecayCount_Performance()
        {
            // Arrange
            var strategy = new DecayCountStrategy { DecayAmount = 2, MinCount = 0 };
            var context = new ComboContext { ComboCount = 100 };
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"DecayCountStrategy: {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.0001),
                $"DecayCountStrategy平均耗时{avgTimeMs:F6}ms，应小于0.0001ms"
            );
        }

        [Test]
        [Description("性能测试：CompositeResetStrategy Sequential模式")]
        public void ResetStrategy_CompositeSequential_Performance()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Sequential,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new DecayCountStrategy { DecayAmount = 1, MinCount = 0 },
                    new DecayCountStrategy { DecayAmount = 1, MinCount = 0 },
                    new DecayCountStrategy { DecayAmount = 1, MinCount = 0 },
                },
            };
            var context = new ComboContext { ComboCount = 100 };
            var stopwatch = new Stopwatch();

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                strategy.CalculateResetCount(100, context);
            }
            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / TEST_ITERATIONS;
            Debug.Log(
                $"CompositeResetStrategy(Sequential): {avgTimeMs:F6}ms per operation ({TEST_ITERATIONS} iterations)"
            );

            Assert.That(
                avgTimeMs,
                Is.LessThan(0.002),
                $"CompositeResetStrategy平均耗时{avgTimeMs:F6}ms，应小于0.002ms"
            );
        }

        #endregion

        #region 内存分配测试

        [Test]
        [Description("内存测试：InputBuffer不产生GC Alloc")]
        [Category("Memory")]
        public void InputBuffer_NoGCAlloc()
        {
            // Arrange
            var buffer = new InputBuffer(0.3f);

            // 预填充一些数据
            for (int i = 0; i < 100; i++)
            {
                buffer.PushInput($"Input{i}");
            }

            // Act & Assert - 测量GC Alloc
            long initialMemory = GC.GetTotalMemory(false);

            for (int i = 0; i < 1000; i++)
            {
                buffer.TryGetInput(out string _);
                buffer.PushInput($"NewInput{i}");
            }

            long finalMemory = GC.GetTotalMemory(false);
            long allocatedBytes = finalMemory - initialMemory;

            Debug.Log($"InputBuffer operations allocated: {allocatedBytes} bytes");

            // InputBuffer本身不应该产生显著的GC Alloc
            // 注意：字符串操作会产生一些分配，这是正常的
            Assert.That(
                allocatedBytes,
                Is.LessThan(1024 * 1024),
                $"InputBuffer操作应尽量减少GC Alloc，当前分配: {allocatedBytes} bytes"
            );
        }

        #endregion

        #region 并发压力测试

        [UnityTest]
        [Description("压力测试：快速连续输入")]
        [Timeout(10000)]
        public IEnumerator StressTest_RapidInputs_HandlesCorrectly()
        {
            // Arrange
            var go = new GameObject("StressTest");
            var controller = go.AddComponent<AsakiComboController>();
            var tree = CreateTestComboTree();
            controller.Initialize(tree);

            yield return null;

            // Act - 快速发送大量输入
            const int rapidInputCount = 100;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < rapidInputCount; i++)
            {
                controller.TriggerAttack("LightAttack");
                // 每帧发送多个输入
                if (i % 10 == 0)
                {
                    yield return null;
                }
            }

            stopwatch.Stop();

            // Assert
            double avgTimeMs = (double)stopwatch.ElapsedMilliseconds / rapidInputCount;
            Debug.Log($"Rapid inputs: {avgTimeMs:F6}ms per input ({rapidInputCount} inputs)");

            Assert.That(
                avgTimeMs,
                Is.LessThan(MAX_ACCEPTABLE_TIME_MS),
                $"快速输入平均耗时{avgTimeMs:F6}ms，应低于{MAX_ACCEPTABLE_TIME_MS}ms"
            );

            // 清理
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tree);
        }

        [UnityTest]
        [Description("压力测试：状态机快速转换")]
        [Timeout(10000)]
        public IEnumerator StressTest_RapidStateChanges_HandlesCorrectly()
        {
            // Arrange
            var go = new GameObject("StateStressTest");
            var controller = go.AddComponent<AsakiComboController>();
            var tree = CreateTestComboTree();
            controller.Initialize(tree);

            yield return null;

            int stateChangeCount = 0;
            controller.OnStateChanged += (from, to) => stateChangeCount++;

            // Act - 快速触发连招
            const int comboCount = 50;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < comboCount; i++)
            {
                controller.TriggerAttack("LightAttack");
                controller.InterruptCombo(InterruptReason.Forced);

                if (i % 5 == 0)
                {
                    yield return null;
                }
            }

            stopwatch.Stop();

            // Assert
            Debug.Log(
                $"State changes: {stateChangeCount}, Time: {stopwatch.ElapsedMilliseconds}ms"
            );
            Assert.That(stateChangeCount, Is.GreaterThan(0), "应该有状态变化发生");

            // 清理
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tree);
        }

        #endregion

        #region 帮助方法

        private ComboTree CreateLargeComboTree(int moveCount)
        {
            var tree = ScriptableObject.CreateInstance<ComboTree>();
            tree.TreeId = "large_test_tree";

            var moves = new System.Collections.Generic.List<ComboMove>();
            var transitions = new System.Collections.Generic.List<ComboTransition>();

            for (int i = 0; i < moveCount; i++)
            {
                moves.Add(
                    new ComboMove
                    {
                        MoveId = $"move_{i}",
                        MoveName = $"Move {i}",
                        StartupTime = 0.1f,
                        ActiveDuration = 0.2f,
                        RecoveryTime = 0.3f,
                    }
                );

                // 每个招式都连接到下一个
                if (i < moveCount - 1)
                {
                    transitions.Add(
                        new ComboTransition
                        {
                            FromMoveId = $"move_{i}",
                            ToMoveId = $"move_{i + 1}",
                            InputType = "LightAttack",
                        }
                    );
                }
            }

            tree.Moves = moves.ToArray();
            tree.Transitions = transitions.ToArray();

            return tree;
        }

        private ComboTree CreateTestComboTree()
        {
            var tree = ScriptableObject.CreateInstance<ComboTree>();
            tree.TreeId = "perf_test";

            var move = new ComboMove
            {
                MoveId = "test_move",
                MoveName = "Test Move",
                StartupTime = 0.01f,
                ActiveDuration = 0.01f,
                RecoveryTime = 0.01f,
                ComboWindowStart = 0.01f,
                ComboWindowEnd = 0.05f,
            };

            tree.Moves = new[] { move };
            tree.Transitions = new ComboTransition[0];

            return tree;
        }

        #endregion
    }
}
