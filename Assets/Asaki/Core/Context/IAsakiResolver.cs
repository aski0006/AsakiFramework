using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Asaki.Core.Context
{
    /// <summary>
    /// 循环依赖异常，当检测到服务解析过程中存在循环依赖时抛出。
    /// </summary>
    /// <remarks>
    /// 此异常记录了完整的循环依赖链，便于开发者定位和解决问题。
    /// </remarks>
    public class CircularDependencyException : Exception
    {
        /// <summary>
        /// 获取循环依赖链，记录了从根类型到触发类型的完整解析路径。
        /// </summary>
        public List<Type> CircularChain { get; }

        /// <summary>
        /// 获取触发循环依赖的类型。
        /// </summary>
        public Type TriggerType { get; }

        /// <summary>
        /// 初始化循环依赖异常实例。
        /// </summary>
        /// <param name="triggerType">触发循环依赖的类型。</param>
        /// <param name="chain">当前的解析链。</param>
        public CircularDependencyException(Type triggerType, IEnumerable<Type> chain)
            : base(BuildMessage(triggerType, chain))
        {
            TriggerType = triggerType;
            CircularChain = new List<Type>(chain);
        }

        /// <summary>
        /// 构建异常消息，包含完整的依赖链信息。
        /// </summary>
        /// <param name="triggerType">触发循环依赖的类型。</param>
        /// <param name="chain">当前的解析链。</param>
        /// <returns>格式化的异常消息字符串。</returns>
        private static string BuildMessage(Type triggerType, IEnumerable<Type> chain)
        {
            var sb = new StringBuilder("Circular dependency detected: ");
            var first = true;
            foreach (var type in chain)
            {
                if (!first)
                {
                    sb.Append(" -> ");
                }
                sb.Append(type.Name);
                first = false;
            }
            sb.Append(" -> ");
            sb.Append(triggerType.Name);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Asaki解析上下文，用于跟踪当前线程的服务解析链，检测循环依赖。
    /// </summary>
    /// <remarks>
    /// 使用AsyncLocal实现线程隔离，确保异步环境下的解析链跟踪正确性。
    /// </remarks>
    public static class AsakiResolveContext
    {
        private static readonly AsyncLocal<HashSet<Type>> _resolveChain =
            new AsyncLocal<HashSet<Type>>();

        /// <summary>
        /// 获取当前线程的解析链。
        /// </summary>
        public static HashSet<Type> CurrentChain
        {
            get
            {
                if (_resolveChain.Value == null)
                {
                    _resolveChain.Value = new HashSet<Type>();
                }
                return _resolveChain.Value;
            }
        }

        /// <summary>
        /// 开始解析指定类型，检查是否存在循环依赖。
        /// </summary>
        /// <param name="type">要解析的服务类型。</param>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        public static void BeginResolve(Type type)
        {
            if (CurrentChain.Contains(type))
            {
                throw new CircularDependencyException(type, CurrentChain);
            }
            CurrentChain.Add(type);
        }

        /// <summary>
        /// 结束解析指定类型，从解析链中移除。
        /// </summary>
        /// <param name="type">已完成解析的服务类型。</param>
        public static void EndResolve(Type type)
        {
            CurrentChain.Remove(type);
        }

        /// <summary>
        /// 清空当前解析链，用于异常恢复场景。
        /// </summary>
        public static void Clear()
        {
            CurrentChain.Clear();
        }
    }

    /// <summary>
    /// Asaki依赖解析器接口，用于解析注册的服务实例。
    /// </summary>
    /// <remarks>
    /// 此接口定义了获取服务实例的标准方法，是Asaki依赖注入系统的核心组成部分。
    /// 不同的实现可以提供不同的解析策略，如全局解析、场景级解析或临时解析。
    /// </remarks>
    public interface IAsakiResolver
    {
        /// <summary>
        /// 获取指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        T Get<T>()
            where T : class, IAsakiService;

        /// <summary>
        /// 尝试获取指定类型的服务实例，如果找到则返回true，否则返回false。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
        /// <returns>如果找到服务则返回true，否则返回false。</returns>
        bool TryGet<T>(out T service)
            where T : class, IAsakiService;
    }
}
