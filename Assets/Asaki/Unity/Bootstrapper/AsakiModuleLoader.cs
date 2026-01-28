using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Unity.Bootstrapper
{
    public static class AsakiModuleLoader
    {
        /// <summary>
        /// 启动整个模块系统
        /// </summary>
        public static async Task Startup(IAsakiModuleDiscovery discovery)
        {
            // 1. 发现
            var allModuleTypes = discovery.GetModuleTypes().ToList();
            ALog.Info($"[Asaki] Discovered {allModuleTypes.Count} modules.");

            // 2. 排序 (DAG)
            var sortedTypes = TopologicalSort(allModuleTypes);

            // 3. 实例化与注册 (Phase 1: Sync Init)
            var activeModules = new List<IAsakiModule>();
            ALog.Info("== [Asaki] Phase 1: Registration & Sync Init ==");
            foreach (Type type in sortedTypes)
            {
                // 强制无参构造
                if (Activator.CreateInstance(type) is not IAsakiModule module)
                    continue;

                // 2. [新增] 静态依赖注入
                // 使用反射作为“开发期回退”，防止第一次编译时 Generated 代码不存在导致报错
                // 正式构建时，Roslyn 会生成 Asaki.Generated.AsakiModuleInjector
                InjectDependenciesSafe(module);

                // [关键设计] 托管注册：将具体类型注册进容器
                // 这样下游模块可以通过 AsakiContext.Get<ResKitModule>() 获取它
                AsakiContext.Register(type, module);

                // 执行同步初始化 (获取配置，注册子服务)
                module.OnInit();

                activeModules.Add(module);
                ALog.Info($"{type.Name} -> [OK] ");
            }

            // 4. 异步初始化 (Phase 2: Async Init)
            ALog.Info("== [Asaki] Phase 2: Async Initialization ==");
            foreach (IAsakiModule module in activeModules)
            {
                await module.OnInitAsync();
            }

            ALog.Info("== [Asaki] Framework Ready ==");
        }

        private static void InjectDependenciesSafe(IAsakiModule module)
        {
            AsakiGlobalInjector.Inject(module);
        }

        /// <summary>
        /// 拓扑排序算法 (解决依赖顺序)
        /// </summary>
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
            var edges = new Dictionary<Type, List<Type>>(); // Key依赖于Value -> 错误，应该是 Value 依赖 Key (Edge: Dependency -> Dependent)
            // 修正图方向：如果我们希望 A 在 B 之前初始化 (B 依赖 A)，则边应该是 A -> B。
            // 这样排序输出才是 A, B。

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

                    edges[dependency].Add(dependent); // 边：依赖项 -> 依赖者
                    inDegree[dependent]++;
                }
            }

            // 准备队列 (所有入度为0的节点，即没有任何依赖的模块)
            // 优先处理 Priority 值小的 (Sort by Priority first)
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
                    // 那些依赖 current 的模块
                    // 这里的 neighbors 需要按优先级排序吗？
                    // 为了简单起见，这里不进行二次排序，但实际入队时如果能保持优先级更好。
                    // 优化：在此处对 neighbors 排序再处理能保证同一层级的优先级

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
                // 找到循环链以便调试
                throw new Exception(
                    "[Asaki] Circular dependency detected! Initialization aborted."
                );
            }

            return result;
        }
    }
}
