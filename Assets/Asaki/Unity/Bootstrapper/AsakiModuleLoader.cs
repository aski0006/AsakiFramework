using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Bootstrapper
{
    /// <summary>
    /// 模块加载结果类。
    /// <para>记录单个模块的加载状态、异常信息和耗时。</para>
    /// </summary>
    public sealed class ModuleLoadResult
    {
        /// <summary>
        /// 模块名称。
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// 是否加载成功。
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 失败时的异常信息。
        /// <para>成功时为 null。</para>
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 是否为可选模块。
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// 加载耗时（毫秒）。
        /// </summary>
        public long ElapsedMs { get; }

        /// <summary>
        /// 模块类型。
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// 创建加载成功的结果。
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleType">模块类型</param>
        /// <param name="isOptional">是否为可选模块</param>
        /// <param name="elapsedMs">加载耗时（毫秒）</param>
        /// <returns>成功的加载结果</returns>
        public static ModuleLoadResult Succeeded(
            string moduleName,
            Type moduleType,
            bool isOptional,
            long elapsedMs
        )
        {
            return new ModuleLoadResult(moduleName, moduleType, true, null, isOptional, elapsedMs);
        }

        /// <summary>
        /// 创建加载失败的结果。
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleType">模块类型</param>
        /// <param name="exception">异常信息</param>
        /// <param name="isOptional">是否为可选模块</param>
        /// <param name="elapsedMs">加载耗时（毫秒）</param>
        /// <returns>失败的加载结果</returns>
        public static ModuleLoadResult Failed(
            string moduleName,
            Type moduleType,
            Exception exception,
            bool isOptional,
            long elapsedMs
        )
        {
            return new ModuleLoadResult(
                moduleName,
                moduleType,
                false,
                exception,
                isOptional,
                elapsedMs
            );
        }

        private ModuleLoadResult(
            string moduleName,
            Type moduleType,
            bool success,
            Exception exception,
            bool isOptional,
            long elapsedMs
        )
        {
            ModuleName = moduleName;
            ModuleType = moduleType;
            Success = success;
            Exception = exception;
            IsOptional = isOptional;
            ElapsedMs = elapsedMs;
        }
    }

    /// <summary>
    /// 模块加载汇总类。
    /// <para>汇总所有模块的加载结果，提供统计信息。</para>
    /// </summary>
    public sealed class ModuleLoadSummary
    {
        /// <summary>
        /// 所有模块加载结果列表。
        /// </summary>
        public List<ModuleLoadResult> Modules { get; }

        /// <summary>
        /// 成功加载的模块数量。
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// 加载失败的模块数量。
        /// </summary>
        public int FailCount { get; }

        /// <summary>
        /// 总加载耗时（毫秒）。
        /// </summary>
        public long TotalElapsedMs { get; }

        /// <summary>
        /// 是否所有模块都加载成功。
        /// </summary>
        public bool IsAllSuccess => FailCount == 0;

        /// <summary>
        /// 是否有必需模块加载失败。
        /// </summary>
        public bool HasRequiredFailure => Modules.Any(m => !m.Success && !m.IsOptional);

        /// <summary>
        /// 初始化模块加载汇总。
        /// </summary>
        /// <param name="modules">模块加载结果列表</param>
        /// <param name="totalElapsedMs">总加载耗时（毫秒）</param>
        public ModuleLoadSummary(List<ModuleLoadResult> modules, long totalElapsedMs)
        {
            Modules = modules ?? new List<ModuleLoadResult>();
            SuccessCount = Modules.Count(m => m.Success);
            FailCount = Modules.Count(m => !m.Success);
            TotalElapsedMs = totalElapsedMs;
        }

        /// <summary>
        /// 获取所有失败的模块结果。
        /// </summary>
        /// <returns>失败的模块结果列表</returns>
        public IEnumerable<ModuleLoadResult> GetFailedModules()
        {
            return Modules.Where(m => !m.Success);
        }

        /// <summary>
        /// 获取所有成功的模块结果。
        /// </summary>
        /// <returns>成功的模块结果列表</returns>
        public IEnumerable<ModuleLoadResult> GetSucceededModules()
        {
            return Modules.Where(m => m.Success);
        }
    }

    /// <summary>
    /// 模块加载异常类。
    /// <para>当模块加载失败时抛出，包含模块名称和原始异常。</para>
    /// </summary>
    public sealed class ModuleLoadException : Exception
    {
        /// <summary>
        /// 失败的模块名称。
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// 模块类型。
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// 是否为可选模块。
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// 初始化模块加载异常。
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleType">模块类型</param>
        /// <param name="isOptional">是否为可选模块</param>
        /// <param name="innerException">原始异常</param>
        public ModuleLoadException(
            string moduleName,
            Type moduleType,
            bool isOptional,
            Exception innerException
        )
            : base($"Module '{moduleName}' failed to load. Optional: {isOptional}", innerException)
        {
            ModuleName = moduleName;
            ModuleType = moduleType;
            IsOptional = isOptional;
        }

        /// <summary>
        /// 初始化模块加载异常（无内部异常）。
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleType">模块类型</param>
        /// <param name="isOptional">是否为可选模块</param>
        /// <param name="message">错误消息</param>
        public ModuleLoadException(
            string moduleName,
            Type moduleType,
            bool isOptional,
            string message
        )
            : base(message)
        {
            ModuleName = moduleName;
            ModuleType = moduleType;
            IsOptional = isOptional;
        }
    }

    /// <summary>
    /// Asaki 模块加载器。
    /// <para>负责模块的发现、排序、实例化和初始化。</para>
    /// </summary>
    public static class AsakiModuleLoader
    {
        /// <summary>
        /// 启动整个模块系统。
        /// <para>包含错误隔离与容错机制，可选模块失败不会阻止系统启动。</para>
        /// </summary>
        /// <param name="discovery">模块发现器</param>
        /// <returns>模块加载汇总结果</returns>
        /// <exception cref="ModuleLoadException">必需模块加载失败时抛出</exception>
        public static async UniTask<ModuleLoadSummary> Startup(IAsakiModuleDiscovery discovery)
        {
            var totalStopwatch = Stopwatch.StartNew();

            // 1. 发现
            var allModuleTypes = discovery.GetModuleTypes().ToList();
            ALog.Info($"[Asaki] Discovered {allModuleTypes.Count} modules.");

            // 2. 排序 (DAG)
            var sortedTypes = TopologicalSort(allModuleTypes);

            // 3. 实例化与注册 (Phase 1: Sync Init)
            var loadResults = new List<ModuleLoadResult>();
            var activeModules = new List<IAsakiModule>();

            ALog.Info("== [Asaki] Phase 1: Registration & Sync Init ==");

            foreach (Type type in sortedTypes)
            {
                var attr = type.GetCustomAttribute<AsakiModuleAttribute>();
                var result = await TryLoadModuleAsync(type, attr, isAsyncPhase: false);

                loadResults.Add(result);

                if (result.Success)
                {
                    activeModules.Add((IAsakiModule)AsakiContext.Get(type));
                    ALog.Info($"[{result.ElapsedMs, 5}ms] {type.Name} -> [OK]");
                }
                else
                {
                    LogModuleFailure(result);

                    // 必需模块失败，立即抛出异常
                    if (!result.IsOptional)
                    {
                        throw new ModuleLoadException(
                            result.ModuleName,
                            result.ModuleType,
                            result.IsOptional,
                            result.Exception
                        );
                    }
                }
            }

            // 4. 异步初始化 (Phase 2: Async Init)
            ALog.Info("== [Asaki] Phase 2: Async Initialization ==");

            foreach (IAsakiModule module in activeModules)
            {
                var type = module.GetType();
                var attr = type.GetCustomAttribute<AsakiModuleAttribute>();

                // 查找 Phase 1 的结果
                var phase1Result = loadResults.First(r => r.ModuleType == type);

                // 执行异步初始化
                var asyncResult = await TryLoadModuleAsync(type, attr, isAsyncPhase: true, module);

                // 更新结果（合并耗时）
                var combinedElapsedMs = phase1Result.ElapsedMs + asyncResult.ElapsedMs;
                var updatedResult = asyncResult.Success
                    ? ModuleLoadResult.Succeeded(
                        asyncResult.ModuleName,
                        asyncResult.ModuleType,
                        asyncResult.IsOptional,
                        combinedElapsedMs
                    )
                    : ModuleLoadResult.Failed(
                        asyncResult.ModuleName,
                        asyncResult.ModuleType,
                        asyncResult.Exception,
                        asyncResult.IsOptional,
                        combinedElapsedMs
                    );

                // 替换原结果
                var index = loadResults.FindIndex(r => r.ModuleType == type);
                loadResults[index] = updatedResult;

                if (updatedResult.Success)
                {
                    ALog.Info($"[{updatedResult.ElapsedMs, 5}ms] {type.Name} -> [OK]");
                }
                else
                {
                    LogModuleFailure(updatedResult);

                    // 必需模块失败，立即抛出异常
                    if (!updatedResult.IsOptional)
                    {
                        throw new ModuleLoadException(
                            updatedResult.ModuleName,
                            updatedResult.ModuleType,
                            updatedResult.IsOptional,
                            updatedResult.Exception
                        );
                    }
                }
            }

            totalStopwatch.Stop();

            // 5. 输出初始化结果报告
            var summary = new ModuleLoadSummary(loadResults, totalStopwatch.ElapsedMilliseconds);
            PrintLoadReport(summary);

            return summary;
        }

        /// <summary>
        /// 尝试加载单个模块。
        /// <para>包含超时控制和异常捕获，支持同步和异步两个阶段。</para>
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="attr">模块特性</param>
        /// <param name="isAsyncPhase">是否为异步初始化阶段</param>
        /// <param name="existingModule">已存在的模块实例（异步阶段使用）</param>
        /// <returns>模块加载结果</returns>
        private static async UniTask<ModuleLoadResult> TryLoadModuleAsync(
            Type moduleType,
            AsakiModuleAttribute attr,
            bool isAsyncPhase,
            IAsakiModule existingModule = null
        )
        {
            var stopwatch = Stopwatch.StartNew();
            var moduleName = moduleType.Name;
            var isOptional = attr?.Optional ?? false;
            var timeoutMs = attr?.TimeoutMs ?? 30000;

            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);

                if (!isAsyncPhase)
                {
                    // Phase 1: 同步初始化
                    // 强制无参构造
                    if (Activator.CreateInstance(moduleType) is not IAsakiModule module)
                    {
                        return ModuleLoadResult.Failed(
                            moduleName,
                            moduleType,
                            new InvalidCastException(
                                $"Type '{moduleName}' does not implement IAsakiModule"
                            ),
                            isOptional,
                            stopwatch.ElapsedMilliseconds
                        );
                    }

                    // 静态依赖注入
                    InjectDependenciesSafe(module);

                    // 托管注册
                    AsakiContext.Register(moduleType, module);

                    // 执行同步初始化
                    module.OnInit();

                    stopwatch.Stop();
                    return ModuleLoadResult.Succeeded(
                        moduleName,
                        moduleType,
                        isOptional,
                        stopwatch.ElapsedMilliseconds
                    );
                }
                else
                {
                    // Phase 2: 异步初始化
                    if (existingModule == null)
                    {
                        return ModuleLoadResult.Failed(
                            moduleName,
                            moduleType,
                            new InvalidOperationException(
                                $"Module '{moduleName}' instance not found for async init"
                            ),
                            isOptional,
                            stopwatch.ElapsedMilliseconds
                        );
                    }

                    // 执行异步初始化（带超时）
                    await existingModule.OnInitAsync().AttachExternalCancellation(cts.Token);

                    stopwatch.Stop();
                    return ModuleLoadResult.Succeeded(
                        moduleName,
                        moduleType,
                        isOptional,
                        stopwatch.ElapsedMilliseconds
                    );
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return ModuleLoadResult.Failed(
                    moduleName,
                    moduleType,
                    new TimeoutException(
                        $"Module '{moduleName}' initialization timed out after {timeoutMs}ms"
                    ),
                    isOptional,
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ModuleLoadResult.Failed(
                    moduleName,
                    moduleType,
                    ex,
                    isOptional,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }

        /// <summary>
        /// 记录模块失败日志。
        /// </summary>
        /// <param name="result">加载结果</param>
        private static void LogModuleFailure(ModuleLoadResult result)
        {
            var optionalTag = result.IsOptional ? "[Optional]" : "[Required]";
            ALog.Error($"[{result.ElapsedMs, 5}ms] {result.ModuleName} -> [FAILED] {optionalTag}");

            if (result.Exception != null)
            {
                ALog.Error($"  Error: {result.Exception.Message}");
                ALog.Error($"  StackTrace: {result.Exception.StackTrace}");
            }
        }

        /// <summary>
        /// 输出初始化结果报告。
        /// </summary>
        /// <param name="summary">加载汇总</param>
        private static void PrintLoadReport(ModuleLoadSummary summary)
        {
            ALog.Info("== [Asaki] Initialization Report ==");
            ALog.Info($"  Total Modules: {summary.Modules.Count}");
            ALog.Info($"  Success: {summary.SuccessCount}");
            ALog.Info($"  Failed:  {summary.FailCount}");
            ALog.Info($"  Total Time: {summary.TotalElapsedMs}ms");

            if (summary.FailCount > 0)
            {
                ALog.Warn("== Failed Modules ==");
                foreach (var failed in summary.GetFailedModules())
                {
                    var optionalTag = failed.IsOptional ? "[Optional]" : "[Required]";
                    ALog.Warn(
                        $"  - {failed.ModuleName} {optionalTag}: {failed.Exception?.Message ?? "Unknown error"}"
                    );
                }
            }

            if (summary.IsAllSuccess)
            {
                ALog.Info("== [Asaki] Framework Ready ==");
            }
            else if (!summary.HasRequiredFailure)
            {
                ALog.Warn("== [Asaki] Framework Ready (with optional module failures) ==");
            }
        }

        /// <summary>
        /// 安全注入依赖项。
        /// <para>使用反射作为开发期回退，防止第一次编译时 Generated 代码不存在导致报错。</para>
        /// </summary>
        /// <param name="module">目标模块</param>
        private static void InjectDependenciesSafe(IAsakiModule module)
        {
            AsakiGlobalInjector.Inject(module);
        }

        /// <summary>
        /// 拓扑排序算法。
        /// <para>解决模块依赖顺序，确保依赖项先于依赖者初始化。</para>
        /// </summary>
        /// <param name="nodes">待排序的模块类型列表</param>
        /// <returns>排序后的模块类型列表</returns>
        /// <exception cref="Exception">循环依赖或缺失依赖时抛出</exception>
        private static List<Type> TopologicalSort(List<Type> nodes)
        {
            // 映射: 类型 -> (优先级, 依赖列表)
            var moduleInfo = new Dictionary<Type, (int Priority, Type[] Deps)>();

            // 构建查找表
            foreach (Type node in nodes)
            {
                AsakiModuleAttribute attr = node.GetCustomAttribute<AsakiModuleAttribute>();
                moduleInfo[node] = (attr.Priority, attr.Dependencies);
            }

            // 构建图
            var edges = new Dictionary<Type, List<Type>>();
            var inDegree = new Dictionary<Type, int>();

            foreach (Type node in nodes)
                inDegree[node] = 0;

            foreach (Type dependent in nodes)
            {
                var dependencies = moduleInfo[dependent].Deps;
                foreach (Type dependency in dependencies)
                {
                    // 确保依赖项在扫描列表中存在
                    if (!moduleInfo.ContainsKey(dependency))
                    {
                        throw new Exception(
                            $"[Asaki] Module '{dependent.Name}' depends on '{dependency.Name}', but it was not found in discovery."
                        );
                    }

                    if (!edges.ContainsKey(dependency))
                        edges[dependency] = new List<Type>();

                    edges[dependency].Add(dependent);
                    inDegree[dependent]++;
                }
            }

            // 准备队列 (所有入度为0的节点，按优先级排序)
            var queue = new Queue<Type>(
                nodes.Where(n => inDegree[n] == 0).OrderBy(n => moduleInfo[n].Priority)
            );

            var result = new List<Type>();

            while (queue.Count > 0)
            {
                Type current = queue.Dequeue();
                result.Add(current);

                if (edges.TryGetValue(current, out var neighbors))
                {
                    var sortedNeighbors = neighbors.OrderBy(n => moduleInfo[n].Priority);

                    foreach (Type neighbor in sortedNeighbors)
                    {
                        inDegree[neighbor]--;
                        if (inDegree[neighbor] == 0)
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            if (result.Count != nodes.Count)
            {
                throw new Exception(
                    "[Asaki] Circular dependency detected! Initialization aborted."
                );
            }

            return result;
        }
    }
}
