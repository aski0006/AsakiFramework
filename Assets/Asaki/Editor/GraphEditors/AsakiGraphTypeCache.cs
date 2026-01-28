using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Asaki.Editor.GraphEditors
{
    /// <summary>
    /// 端口元数据定义
    /// </summary>
    public struct PortInfo
    {
        public string FieldName;
        public string PortName;
        public bool IsInput;
        public bool AllowMultiple;
        public Type DataType;
    }

    /// <summary>
    /// 可绘制字段元数据（新增）
    /// </summary>
    public struct FieldDrawInfo
    {
        public string FieldName; // 字段名
        public bool IsSerializable; // 是否可序列化
    }

    /// <summary>
    /// 节点类型元数据缓存中心（性能核心）
    /// 将运行时反射开销降至 O(1)
    /// </summary>
    public static class AsakiGraphTypeCache
    {
        // 端口缓存：Type -> PortInfo列表
        private static readonly Dictionary<Type, List<PortInfo>> _portCache =
            new Dictionary<Type, List<PortInfo>>();

        // 可绘制字段缓存：Type -> FieldDrawInfo列表（新增）
        private static readonly Dictionary<Type, List<FieldDrawInfo>> _drawFieldCache =
            new Dictionary<Type, List<FieldDrawInfo>>();

        /// <summary>
        /// 获取节点类型的所有端口定义（O(1) 访问）
        /// </summary>
        public static List<PortInfo> GetPorts(Type nodeType)
        {
            if (_portCache.TryGetValue(nodeType, out var list))
                return list;

            // 缓存未命中，执行反射扫描（仅一次）
            var result = new List<PortInfo>();
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                // 扫描 Input 特性
                AsakiNodeInputAttribute inputAttr =
                    field.GetCustomAttribute<AsakiNodeInputAttribute>();
                if (inputAttr != null)
                {
                    result.Add(
                        new PortInfo
                        {
                            FieldName = field.Name,
                            PortName = inputAttr.PortName,
                            IsInput = true,
                            AllowMultiple = inputAttr.Multiple,
                            DataType = field.FieldType,
                        }
                    );
                }

                // 扫描 Output 特性
                AsakiNodeOutputAttribute outputAttr =
                    field.GetCustomAttribute<AsakiNodeOutputAttribute>();
                if (outputAttr != null)
                {
                    result.Add(
                        new PortInfo
                        {
                            FieldName = field.Name,
                            PortName = outputAttr.PortName,
                            IsInput = false,
                            AllowMultiple = outputAttr.Multiple,
                            DataType = field.FieldType,
                        }
                    );
                }
            }

            _portCache[nodeType] = result;
            return result;
        }

        /// <summary>
        /// 获取节点类型需要绘制的非端口字段（新增，O(1)）
        /// </summary>
        public static List<FieldDrawInfo> GetDrawableFields(Type nodeType)
        {
            if (_drawFieldCache.TryGetValue(nodeType, out var fields))
                return fields;

            fields = new List<FieldDrawInfo>();
            // 只扫描公共实例字段（Unity默认序列化规则）
            var publicFields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in publicFields)
            {
                // 跳过基础字段
                if (field.Name is "GUID" or "Position" or "ExecutionOrder")
                    continue;

                // 跳过端口字段（已用特性标记）
                if (
                    Attribute.IsDefined(field, typeof(AsakiNodeInputAttribute))
                    || Attribute.IsDefined(field, typeof(AsakiNodeOutputAttribute))
                )
                    continue;

                // ★ 仅缓存Unity可序列化的字段
                if (IsUnitySerializable(field.FieldType))
                {
                    fields.Add(new FieldDrawInfo { FieldName = field.Name, IsSerializable = true });
                }
            }

            _drawFieldCache[nodeType] = fields;
            return fields;
        }

        /// <summary>
        /// 判断类型是否可被Unity序列化（避免缓存无效字段）
        /// </summary>
        private static bool IsUnitySerializable(Type type)
        {
            // 基础可序列化类型
            if (
                type.IsPrimitive
                || type == typeof(string)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
            )
                return true;

            // Unity对象引用
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return true;

            // 枚举
            if (type.IsEnum)
                return true;

            // 标记为[System.Serializable]的结构体或类
            return Attribute.IsDefined(type, typeof(SerializableAttribute));
        }

        /// <summary>
        /// 清除所有缓存（在脚本重载时调用）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _portCache.Clear();
            _drawFieldCache.Clear();
        }

        /// <summary>
        /// 异步预热所有节点类型缓存（编辑器启动时执行）
        /// </summary>
        [InitializeOnLoadMethod]
        private static async void PreheatCacheAsync()
        {
            // 延迟到编辑器完全加载
            await System.Threading.Tasks.Task.Yield();

            Stopwatch stopwatch = Stopwatch.StartNew();
            TypeCache.TypeCollection nodeTypes = TypeCache.GetTypesDerivedFrom<AsakiNodeBase>();
            int preheatedCount = 0;

            foreach (Type type in nodeTypes)
            {
                if (type.IsAbstract)
                    continue;

                // 预填充端口和字段缓存
                GetPorts(type);
                GetDrawableFields(type);
                preheatedCount++;

                // 每帧让出控制权，避免卡死（8ms ≈ 1帧）
                if (stopwatch.ElapsedMilliseconds > 8)
                {
                    await System.Threading.Tasks.Task.Yield();
                    stopwatch.Restart();
                }
            }
        }
    }
}
