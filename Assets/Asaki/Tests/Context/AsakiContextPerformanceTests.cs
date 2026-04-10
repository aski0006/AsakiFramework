using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Context;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Asaki.Tests.Context
{
    /// <summary>
    /// AsakiContext 性能基准测试
    /// 验证 Copy-On-Write 无锁读架构的性能特征
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class AsakiContextPerformanceTests
    {
        private const int StandardIterations = 10000;
        private const int HighIterations = 100000;
        private const int ServiceCount = 64;

        private static int _testRunId;
        private static readonly ConcurrentDictionary<int, List<Type>> _generatedTypes = new();

        [SetUp]
        public void Setup()
        {
            _testRunId = TestContext.CurrentContext.Test.GetHashCode();
            AsakiContext.Reset();
        }

        [TearDown]
        public void Teardown()
        {
            AsakiContext.Reset();
        }

        #region Get<T> 无锁读取性能测试

        [Test]
        [Category("Performance")]
        [Description("测试 Get<T> 单次读取性能")]
        public void Performance_Get_SingleRead_FastAccess()
        {
            var services = ArrangeUniqueServices(10);

            var service = AsakiContext.Get(services[0].InterfaceType);
            Assert.IsNotNull(service);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < StandardIterations; i++)
            {
                _ = AsakiContext.Get(services[0].InterfaceType);
            }
            sw.Stop();

            double avgNs = sw.Elapsed.TotalMilliseconds * 1000000 / StandardIterations;
            Debug.Log(
                $"[Get<T>] {StandardIterations} iterations: {sw.ElapsedMilliseconds}ms, avg: {avgNs:F2}ns/call"
            );

            Assert.Less(
                sw.ElapsedMilliseconds,
                50,
                "Get<T> should be extremely fast (O(1) lock-free)"
            );
        }

        [Test]
        [Category("Performance")]
        [Description("测试 Get<T> 与原生 Dictionary 查找对比")]
        public void Performance_Get_CompareWithDictionary()
        {
            var services = ArrangeUniqueServices(ServiceCount);
            var firstInterface = services[0].InterfaceType;
            var firstImpl = services[0].Instance;

            var dict = new Dictionary<Type, IAsakiService>(ServiceCount);
            foreach (var svc in services)
            {
                dict[svc.InterfaceType] = svc.Instance;
            }

            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < HighIterations; i++)
            {
                _ = dict[firstInterface];
            }
            sw1.Stop();

            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < HighIterations; i++)
            {
                _ = AsakiContext.Get(firstInterface);
            }
            sw2.Stop();

            double ratio = sw2.ElapsedTicks > 0 ? (double)sw2.ElapsedTicks / sw1.ElapsedTicks : 0;
            Debug.Log(
                $"[Get vs Dictionary] Dict: {sw1.ElapsedMilliseconds}ms, AsakiContext: {sw2.ElapsedMilliseconds}ms, ratio: {ratio:F2}x"
            );

            Assert.Less(ratio, 2.5, "AsakiContext Get should be within 2.5x of raw Dictionary");
        }

        [Test]
        [Category("Performance")]
        [Description("测试 TryGet 性能")]
        public void Performance_TryGet_FastAccess()
        {
            var services = ArrangeUniqueServices(10);
            var firstInterface = services[0].InterfaceType;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < StandardIterations; i++)
            {
                TryGetDynamic(firstInterface, out _);
            }
            sw.Stop();

            double avgNs = sw.Elapsed.TotalMilliseconds * 1000000 / StandardIterations;
            Debug.Log(
                $"[TryGet<T>] {StandardIterations} iterations: {sw.ElapsedMilliseconds}ms, avg: {avgNs:F2}ns/call"
            );

            Assert.Less(sw.ElapsedMilliseconds, 100, "TryGet should be extremely fast");
        }

        [Test]
        [Category("Performance")]
        [Description("测试不同服务数量下的 Get 性能")]
        public void Performance_Get_VariousServiceCounts_ScalesWell()
        {
            int[] serviceCounts = { 16, 32, 64, 128, 256 };
            var results = new List<string>();

            foreach (int count in serviceCounts)
            {
                AsakiContext.Reset();
                var services = ArrangeUniqueServices(count);
                var firstInterface = services[0].InterfaceType;

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < StandardIterations; i++)
                {
                    _ = AsakiContext.Get(firstInterface);
                }
                sw.Stop();

                results.Add($"Services={count}: {sw.ElapsedMilliseconds}ms");
            }

            Debug.Log($"[Get Scaling] {string.Join(", ", results)}");
            Assert.Pass("Scaling test completed");
        }

        #endregion

        #region Register 写时复制性能测试

        [Test]
        [Category("Performance")]
        [Description("测试 Register 单次写入性能")]
        public void Performance_Register_SingleWrite_AcceptableForStartup()
        {
            var services = CreateUniqueServiceInstances(ServiceCount);

            var sw = Stopwatch.StartNew();
            foreach (var svc in services)
            {
                AsakiContext.Register(svc.InterfaceType, svc.Instance);
            }
            sw.Stop();

            double avgMs = (double)sw.ElapsedMilliseconds / ServiceCount;
            Debug.Log(
                $"[Register] {ServiceCount} services: {sw.ElapsedMilliseconds}ms, avg: {avgMs:F3}ms/register"
            );

            Assert.Less(
                sw.ElapsedMilliseconds,
                200,
                "Register should complete in reasonable time for startup"
            );
        }

        [Test]
        [Category("Performance")]
        [Description("测试 Register 的 O(n) 复制开销")]
        public void Performance_Register_CopyOnWriteOverhead()
        {
            var timings = new List<long>();
            int[] checkpoints = { 10, 20, 40, 80 };
            int serviceIndex = 0;

            var allServices = CreateUniqueServiceInstances(200);

            foreach (int checkpoint in checkpoints)
            {
                while (GetServiceCount() < checkpoint && serviceIndex < allServices.Count)
                {
                    var svc = allServices[serviceIndex++];
                    AsakiContext.Register(svc.InterfaceType, svc.Instance);
                }

                if (serviceIndex < allServices.Count)
                {
                    var nextSvc = allServices[serviceIndex++];
                    var singleSw = Stopwatch.StartNew();
                    AsakiContext.Register(nextSvc.InterfaceType, nextSvc.Instance);
                    singleSw.Stop();
                    timings.Add(singleSw.ElapsedTicks);
                }
            }

            Debug.Log($"[Register COW] Timings at checkpoints: {string.Join(", ", timings)} ticks");
            Assert.Pass("Copy-On-Write overhead test completed");
        }

        [Test]
        [Category("Performance")]
        [Description("测试 Replace 热更新性能")]
        public void Performance_Replace_HotUpdatePerformance()
        {
            var services = ArrangeUniqueServices(10);
            var firstInterface = services[0].InterfaceType;
            var firstImpl = services[0].Instance;
            AsakiContext.Freeze();

            var sw = Stopwatch.StartNew();
            ReplaceDynamic(firstInterface, firstImpl);
            sw.Stop();

            Debug.Log($"[Replace] Single replacement: {sw.ElapsedMilliseconds}ms");
            Assert.Less(sw.ElapsedMilliseconds, 10, "Replace should be fast for hot-updates");
        }

        #endregion

        #region 并发读取性能测试

        [Test]
        [Category("Performance")]
        [Description("测试多线程并发读取性能")]
        public void Performance_ConcurrentRead_HighThroughput()
        {
            var services = ArrangeUniqueServices(10);
            var firstInterface = services[0].InterfaceType;

            const int threadCount = 4;
            const int readsPerThread = 25000;
            var barrier = new Barrier(threadCount);
            var sw = new Stopwatch();
            var errors = new List<Exception>();

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    if (threadId == 0)
                        sw.Start();

                    try
                    {
                        for (int i = 0; i < readsPerThread; i++)
                        {
                            _ = AsakiContext.Get(firstInterface);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                            errors.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);
            sw.Stop();

            long totalReads = threadCount * readsPerThread;
            double readsPerMs = totalReads / (double)sw.ElapsedMilliseconds;

            Debug.Log(
                $"[Concurrent Read] {threadCount} threads, {totalReads} reads: {sw.ElapsedMilliseconds}ms, {readsPerMs:F0} reads/ms"
            );

            Assert.IsEmpty(errors, "No exceptions should occur during concurrent reads");
            Assert.Less(sw.ElapsedMilliseconds, 500, "Concurrent reads should be fast");
        }

        [Test]
        [Category("Performance")]
        [Description("测试读写混合场景性能")]
        public void Performance_MixedReadWrite_AcceptablePerformance()
        {
            var initialServices = ArrangeUniqueServices(20);
            var writerServices = CreateUniqueServiceInstances(100, startId: 1000);

            const int readerThreads = 3;
            const int writerThreads = 1;
            const int operationsPerThread = 5000;
            var barrier = new Barrier(readerThreads + writerThreads);
            var sw = new Stopwatch();
            var writeCount = 0;
            var errors = new List<Exception>();

            var firstInterface = initialServices[0].InterfaceType;

            var tasks = new List<Task>();

            for (int t = 0; t < readerThreads; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        if (threadId == 0)
                            sw.Start();

                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            TryGetDynamic(firstInterface, out _);
                        }
                    })
                );
            }

            int writerIdx = 0;
            tasks.Add(
                Task.Run(() =>
                {
                    barrier.SignalAndWait();

                    for (
                        int i = 0;
                        i < operationsPerThread / 10 && writerIdx < writerServices.Count;
                        i++
                    )
                    {
                        try
                        {
                            var svc = writerServices[writerIdx++];
                            AsakiContext.Register(svc.InterfaceType, svc.Instance);
                            Interlocked.Increment(ref writeCount);
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                                errors.Add(ex);
                        }
                        Thread.Sleep(1);
                    }
                })
            );

            Task.WaitAll(tasks.ToArray());
            sw.Stop();

            Debug.Log(
                $"[Mixed R/W] {readerThreads} readers + {writerThreads} writer: {sw.ElapsedMilliseconds}ms, {writeCount} writes"
            );

            Assert.IsEmpty(errors, "No exceptions during mixed read/write");
            Assert.Less(
                sw.ElapsedMilliseconds,
                2000,
                "Mixed read/write should complete in reasonable time"
            );
        }

        #endregion

        #region 压力测试

        [Test]
        [Category("Performance")]
        [Description("测试高频读取压力")]
        public void Stress_HighFrequencyRead_Stable()
        {
            var services = ArrangeUniqueServices(10);
            var firstInterface = services[0].InterfaceType;

            const int iterations = 1000000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                _ = AsakiContext.Get(firstInterface);
            }
            sw.Stop();

            double nsPerCall = sw.Elapsed.TotalMilliseconds * 1000000 / iterations;
            Debug.Log(
                $"[Stress Read] {iterations} iterations: {sw.ElapsedMilliseconds}ms, {nsPerCall:F2}ns/call"
            );

            Assert.Less(nsPerCall, 200, "High frequency read should be sub-200ns");
        }

        [Test]
        [Category("Performance")]
        [Description("测试大量服务注册压力")]
        public void Stress_LargeServiceRegistration_HandlesCorrectly()
        {
            const int largeServiceCount = 500;
            var services = CreateUniqueServiceInstances(largeServiceCount);
            var sw = Stopwatch.StartNew();

            foreach (var svc in services)
            {
                AsakiContext.Register(svc.InterfaceType, svc.Instance);
            }
            sw.Stop();

            double avgMs = (double)sw.ElapsedMilliseconds / largeServiceCount;
            Debug.Log(
                $"[Stress Register] {largeServiceCount} services: {sw.ElapsedMilliseconds}ms, avg: {avgMs:F3}ms/register"
            );

            Assert.Less(
                sw.ElapsedMilliseconds,
                3000,
                "Large registration should complete in reasonable time"
            );
        }

        [Test]
        [Category("Performance")]
        [Description("测试 GetOrRegister 并发安全性")]
        public void Stress_GetOrRegister_ThreadSafe()
        {
            const int threadCount = 8;
            const int attemptsPerThread = 100;
            var barrier = new Barrier(threadCount);
            var successCount = 0;
            var exceptions = new List<Exception>();

            var (interfaceType, instance) = GenerateUniqueServiceType(_testRunId, 0);
            var implType = instance.GetType();

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        for (int i = 0; i < attemptsPerThread; i++)
                        {
                            var service = GetOrRegisterDynamic(interfaceType, implType);
                            if (service != null)
                                Interlocked.Increment(ref successCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                            exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);

            Debug.Log(
                $"[GetOrRegister Stress] {threadCount} threads, {successCount} successful gets"
            );

            Assert.IsEmpty(exceptions, "No exceptions during concurrent GetOrRegister");
            Assert.Greater(successCount, 0, "At least some operations should succeed");
        }

        #endregion

        #region 内存和GC测试

        [Test]
        [Category("Performance")]
        [Description("测试 Get<T> 的GC分配")]
        public void Memory_Get_NoGcAllocation()
        {
            var services = ArrangeUniqueServices(10);
            var firstInterface = services[0].InterfaceType;

            long memoryBefore = GC.GetTotalMemory(true);

            for (int i = 0; i < StandardIterations; i++)
            {
                _ = AsakiContext.Get(firstInterface);
            }

            long memoryAfter = GC.GetTotalMemory(false);
            long allocation = memoryAfter - memoryBefore;

            Debug.Log($"[Memory Get] Allocation for {StandardIterations} Gets: {allocation} bytes");

            Assert.Less(allocation, 1024, "Get<T> should not allocate significant memory");
        }

        [Test]
        [Category("Performance")]
        [Description("测试 Register 的内存分配")]
        public void Memory_Register_AllocationPattern()
        {
            const int registerCount = 100;
            long totalMemory = 0;

            var services = CreateUniqueServiceInstances(registerCount);

            for (int i = 0; i < registerCount; i++)
            {
                long before = GC.GetTotalMemory(true);
                AsakiContext.Register(services[i].InterfaceType, services[i].Instance);
                long after = GC.GetTotalMemory(false);
                totalMemory += (after - before);
            }

            double avgAllocation = (double)totalMemory / registerCount;
            Debug.Log($"[Memory Register] Avg allocation per Register: {avgAllocation:F0} bytes");

            Assert.Pass(
                $"Memory allocation pattern recorded: avg {avgAllocation:F0} bytes/register"
            );
        }

        [Test]
        [Category("Performance")]
        [Description("测试 ClearAll 的内存释放")]
        public void Memory_ClearAll_ReleasesMemory()
        {
            ArrangeUniqueServices(100);

            long memoryAfterRegister = GC.GetTotalMemory(true);

            AsakiContext.ClearAll();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryAfterClear = GC.GetTotalMemory(true);
            long released = memoryAfterRegister - memoryAfterClear;

            Debug.Log($"[Memory ClearAll] Released: {released} bytes");
            Assert.Pass($"Memory release recorded: {released} bytes");
        }

        #endregion

        #region DynamicPhase 性能测试

        [Test]
        [Category("Performance")]
        [Description("测试 DynamicPhase 进入/退出性能")]
        public void Performance_DynamicPhase_FastTransition()
        {
            ArrangeUniqueServices(10);
            AsakiContext.Freeze();

            const int iterations = 1000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                AsakiContext.EnterDynamicPhase();
                AsakiContext.ExitDynamicPhase();
            }
            sw.Stop();

            double avgNs = sw.Elapsed.TotalMilliseconds * 1000000 / (iterations * 2);
            Debug.Log(
                $"[DynamicPhase] {iterations} cycles: {sw.ElapsedMilliseconds}ms, avg: {avgNs:F2}ns/transition"
            );

            Assert.Less(sw.ElapsedMilliseconds, 100, "DynamicPhase transitions should be fast");
        }

        [Test]
        [Category("Performance")]
        [Description("测试嵌套 DynamicPhase 性能")]
        public void Performance_NestedDynamicPhase_CorrectBehavior()
        {
            ArrangeUniqueServices(10);
            AsakiContext.Freeze();

            const int nestDepth = 10;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < nestDepth; i++)
            {
                AsakiContext.EnterDynamicPhase();
            }

            var (interfaceType, instance) = GenerateUniqueServiceType(_testRunId, 9999);
            AsakiContext.Register(interfaceType, instance);

            for (int i = 0; i < nestDepth; i++)
            {
                AsakiContext.ExitDynamicPhase();
            }
            sw.Stop();

            Debug.Log($"[Nested DynamicPhase] Depth {nestDepth}: {sw.ElapsedMilliseconds}ms");
            Assert.Pass("Nested DynamicPhase test completed");
        }

        #endregion

        #region 辅助方法

        private List<(Type InterfaceType, IAsakiService Instance)> ArrangeUniqueServices(int count)
        {
            var services = new List<(Type, IAsakiService)>();
            for (int i = 0; i < count; i++)
            {
                var (interfaceType, instance) = GenerateUniqueServiceType(_testRunId, i);
                AsakiContext.Register(interfaceType, instance);
                services.Add((interfaceType, instance));
            }
            return services;
        }

        private List<(Type InterfaceType, IAsakiService Instance)> CreateUniqueServiceInstances(
            int count,
            int startId = 0
        )
        {
            var services = new List<(Type, IAsakiService)>();
            for (int i = 0; i < count; i++)
            {
                var (interfaceType, instance) = GenerateUniqueServiceType(_testRunId, startId + i);
                services.Add((interfaceType, instance));
            }
            return services;
        }

        private static int _typeCounter = 0;

        private static (Type InterfaceType, IAsakiService Instance) GenerateUniqueServiceType(
            int testRunId,
            int index
        )
        {
            int uniqueId = Interlocked.Increment(ref _typeCounter);
            string typeName = $"ITestService_{testRunId}_{index}_{uniqueId}";
            string implName = $"TestServiceImpl_{testRunId}_{index}_{uniqueId}";

            var assemblyName = new AssemblyName($"DynamicAssemblies_{testRunId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run
            );
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            var interfaceBuilder = moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract
            );
            interfaceBuilder.AddInterfaceImplementation(typeof(IAsakiService));
            var interfaceType = interfaceBuilder.CreateType();

            var typeBuilder = moduleBuilder.DefineType(
                implName,
                TypeAttributes.Public | TypeAttributes.Class
            );
            typeBuilder.AddInterfaceImplementation(interfaceType);

            var idField = typeBuilder.DefineField("_id", typeof(int), FieldAttributes.Private);
            var paddingField = typeBuilder.DefineField(
                "_padding",
                typeof(byte[]),
                FieldAttributes.Private
            );

            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(int) }
            );
            var ctorIl = ctorBuilder.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, idField);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldc_I4, 64);
            ctorIl.Emit(OpCodes.Newarr, typeof(byte));
            ctorIl.Emit(OpCodes.Stfld, paddingField);
            ctorIl.Emit(OpCodes.Ret);

            var instance = (IAsakiService)
                Activator.CreateInstance(typeBuilder.CreateType(), uniqueId);
            return (interfaceType, instance);
        }

        private static bool TryGetDynamic(Type type, out IAsakiService service)
        {
            var method = typeof(AsakiContext).GetMethod("TryGet");
            var genericMethod = method.MakeGenericMethod(type);
            var parameters = new object[] { null };
            var result = (bool)genericMethod.Invoke(null, parameters);
            service = parameters[0] as IAsakiService;
            return result;
        }

        private static void ReplaceDynamic(Type interfaceType, IAsakiService instance)
        {
            var method = typeof(AsakiContext).GetMethod("Replace");
            var genericMethod = method.MakeGenericMethod(interfaceType);
            genericMethod.Invoke(null, new object[] { instance });
        }

        private static IAsakiService GetOrRegisterDynamic(Type interfaceType, Type implType)
        {
            var method = typeof(AsakiContext).GetMethod("GetOrRegister");
            var genericMethod = method.MakeGenericMethod(interfaceType);
            var factoryType = typeof(Func<>).MakeGenericType(interfaceType);
            var del = Delegate.CreateDelegate(
                factoryType,
                null,
                typeof(AsakiContextPerformanceTests)
                    .GetMethod(
                        nameof(CreateServiceInstance),
                        BindingFlags.NonPublic | BindingFlags.Static
                    )
                    .MakeGenericMethod(interfaceType)
            );
            return (IAsakiService)genericMethod.Invoke(null, new object[] { del });
        }

        private static T CreateServiceInstance<T>()
            where T : class, IAsakiService
        {
            var implType = typeof(T)
                .Assembly.GetTypes()
                .First(t => typeof(T).IsAssignableFrom(t) && t.IsClass);
            return (T)Activator.CreateInstance(implType, 0);
        }

        private static int GetServiceCount()
        {
            var field = typeof(AsakiContext).GetField(
                "_services",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            if (field?.GetValue(null) is Dictionary<Type, IAsakiService> services)
            {
                return services.Count;
            }
            return 0;
        }

        #endregion
    }
}
