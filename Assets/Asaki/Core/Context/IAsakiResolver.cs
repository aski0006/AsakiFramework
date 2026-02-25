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
    /// 采用栈结构管理解析链，确保每个异步上下文拥有独立的解析链实例。
    /// </remarks>
    public static class AsakiResolveContext
    {
        /// <summary>
        /// 解析链上下文数据，封装单个异步上下文的解析链状态。
        /// </summary>
        private sealed class ResolveChainContext
        {
            /// <summary>
            /// 当前解析链中的类型集合，用于快速检测循环依赖。
            /// </summary>
            public HashSet<Type> ChainSet { get; } = new HashSet<Type>();

            /// <summary>
            /// 解析链栈，按顺序记录解析路径，用于生成详细的错误信息。
            /// </summary>
            public Stack<Type> ChainStack { get; } = new Stack<Type>();
        }

        /// <summary>
        /// 当前异步上下文的解析链数据。
        /// 使用AsyncLocal确保异步上下文隔离。
        /// </summary>
        private static readonly AsyncLocal<ResolveChainContext> _context =
            new AsyncLocal<ResolveChainContext>();

        /// <summary>
        /// 当前解析的源服务类型（用于日志追踪依赖关系）
        /// </summary>
        private static readonly AsyncLocal<Type> _sourceType = new AsyncLocal<Type>();

        /// <summary>
        /// 获取当前异步上下文的解析链上下文，如果不存在则创建新实例。
        /// </summary>
        /// <remarks>
        /// 每次访问时检查是否需要创建新实例，确保异步上下文的独立性。
        /// 当AsyncLocal在新的异步上下文中复制引用时，首次写入操作会触发新实例创建。
        /// </remarks>
        private static ResolveChainContext CurrentContext
        {
            get
            {
                if (_context.Value == null)
                {
                    _context.Value = new ResolveChainContext();
                }
                return _context.Value;
            }
        }

        /// <summary>
        /// 获取当前解析链的只读快照，用于调试和错误报告。
        /// </summary>
        /// <returns>当前解析链的类型集合的只读副本。</returns>
        public static IReadOnlyCollection<Type> GetCurrentChain()
        {
            return new List<Type>(CurrentContext.ChainSet);
        }

        /// <summary>
        /// 开始解析指定类型，检查是否存在循环依赖。
        /// </summary>
        /// <param name="type">要解析的服务类型。</param>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        public static void BeginResolve(Type type)
        {
            var context = CurrentContext;

            if (context.ChainSet.Contains(type))
            {
                throw new CircularDependencyException(type, context.ChainStack);
            }

            context.ChainSet.Add(type);
            context.ChainStack.Push(type);
        }

        /// <summary>
        /// 结束解析指定类型，从解析链中移除。
        /// </summary>
        /// <param name="type">已完成解析的服务类型。</param>
        /// <remarks>
        /// 同时从集合和栈中移除，确保数据一致性。
        /// </remarks>
        public static void EndResolve(Type type)
        {
            var context = CurrentContext;

            context.ChainSet.Remove(type);

            // 从栈中移除（栈顶应该是当前类型）
            if (context.ChainStack.Count > 0 && context.ChainStack.Peek() == type)
            {
                context.ChainStack.Pop();
            }
        }

        /// <summary>
        /// 清空当前解析链，用于异常恢复场景。
        /// </summary>
        /// <remarks>
        /// 重置整个上下文，确保后续解析从干净状态开始。
        /// </remarks>
        public static void Clear()
        {
            var context = _context.Value;
            if (context != null)
            {
                context.ChainSet.Clear();
                context.ChainStack.Clear();
            }
        }

        /// <summary>
        /// 重置当前异步上下文，创建全新的解析链实例。
        /// </summary>
        /// <remarks>
        /// 用于需要完全隔离的解析场景，确保不会继承父上下文的解析链状态。
        /// </remarks>
        public static void ResetContext()
        {
            _context.Value = new ResolveChainContext();
        }

        /// <summary>
        /// 设置当前解析的源服务类型
        /// </summary>
        /// <param name="type">源服务类型</param>
        public static void SetSourceType(Type type)
        {
            _sourceType.Value = type;
        }

        /// <summary>
        /// 获取当前解析的源服务类型
        /// </summary>
        /// <returns>源服务类型，未设置时返回 null</returns>
        public static Type GetSourceType()
        {
            return _sourceType.Value;
        }

        /// <summary>
        /// 清除源服务类型
        /// </summary>
        public static void ClearSourceType()
        {
            _sourceType.Value = null;
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
