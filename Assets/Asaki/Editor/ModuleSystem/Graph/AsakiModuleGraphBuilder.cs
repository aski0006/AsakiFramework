using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.ModuleSystem.Graph
{
    public static class AsakiModuleGraphBuilder
    {
        public static AsakiModuleGraph Build()
        {
            AsakiModuleGraph graph = ScriptableObject.CreateInstance<AsakiModuleGraph>();
            graph.name = "Module Dependency Graph (Generated)";

            // 1. 扫描模块
            var moduleTypes = TypeCache
                .GetTypesDerivedFrom<IAsakiModule>()
                .Where(t => !t.IsAbstract && t.IsDefined(typeof(AsakiModuleAttribute), false))
                .ToList();

            var nodeMap = new Dictionary<Type, AsakiModuleNode>();

            // 2. 创建节点 (Nodes)
            // 简单布局：按 Priority 分层，或者直接网格排列，后续让 GraphView 自动布局
            int x = 0;
            int y = 0;
            int colWidth = 250;

            // 按优先级排序，方便视觉分层
            var sortedTypes = moduleTypes
                .OrderBy(t => t.GetCustomAttribute<AsakiModuleAttribute>().Priority)
                .ToList();

            foreach (Type type in sortedTypes)
            {
                AsakiModuleAttribute attr = type.GetCustomAttribute<AsakiModuleAttribute>();

                AsakiModuleNode node = new AsakiModuleNode
                {
                    GUID = Guid.NewGuid().ToString(),
                    Position = new Vector2(x, y),
                    ModuleName = type.Name,
                    Priority = attr.Priority,
                };

                graph.Nodes.Add(node);
                nodeMap[type] = node;

                // 简单的自动换行布局
                x += colWidth;
                if (x > 1000)
                {
                    x = 0;
                    y += 150;
                }
            }

            // 3. 创建连线 (Edges)
            foreach (Type type in sortedTypes)
            {
                AsakiModuleAttribute attr = type.GetCustomAttribute<AsakiModuleAttribute>();
                AsakiModuleNode sourceNode = nodeMap[type];

                foreach (Type depType in attr.Dependencies)
                {
                    if (nodeMap.TryGetValue(depType, out AsakiModuleNode targetNode))
                    {
                        // 依赖关系: Source -> Dependency
                        // 即 Source 的 "Out" 连到 Dependency 的 "In"
                        // 或者是反过来？看你习惯。
                        // 通常依赖图是：使用者 -> 被使用者。
                        // AsakiPoolModule -> AsakiResourcesModule

                        AsakiEdgeData edge = new AsakiEdgeData
                        {
                            BaseNodeGUID = sourceNode.GUID,
                            BasePortName = "Out", // 我依赖...
                            TargetNodeGUID = targetNode.GUID,
                            TargetPortName = "In", // ...被依赖
                        };
                        graph.Edges.Add(edge);
                    }
                }
            }

            return graph;
        }
    }
}
