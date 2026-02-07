using System.Collections.Generic;
using System.Linq;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招上下文 - 传递给重置策略的数据
    /// </summary>
    public class ComboContext
    {
        public AsakiComboController Controller;
        public ComboMove CurrentMove;
        public ComboMove PreviousMove;
        public int ComboCount;
        public float ComboTimer;
        public InterruptReason? InterruptReason;
        public Dictionary<string, object> Blackboard = new Dictionary<string, object>();

        public T GetData<T>(string key) =>
            Blackboard.TryGetValue(key, out var val) ? (T)val : default;

        public void SetData<T>(string key, T value) =>
            Blackboard[key] = value;
    }

    /// <summary>
    /// 连招重置策略接口
    /// </summary>
    public interface IComboResetStrategy
    {
        /// <summary>
        /// 计算重置后的连招计数
        /// </summary>
        /// <param name="currentCount">当前连击数</param>
        /// <param name="context">连招上下文</param>
        /// <returns>重置后的计数</returns>
        int CalculateResetCount(int currentCount, ComboContext context);

        /// <summary>
        /// 检查是否应该重置
        /// </summary>
        bool ShouldReset(ComboContext context);
    }

    /// <summary>
    /// 重置为0策略（默认）
    /// </summary>
    public class ResetToZeroStrategy : IComboResetStrategy
    {
        public int CalculateResetCount(int currentCount, ComboContext context) => 0;
        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 保持计数策略 - 连招中断后保持当前计数
    /// </summary>
    public class KeepCountStrategy : IComboResetStrategy
    {
        public int CalculateResetCount(int currentCount, ComboContext context) => currentCount;
        public bool ShouldReset(ComboContext context) => false; // 不真正"重置"，只是保持
    }

    /// <summary>
    /// 递减策略 - 每次中断减少固定值
    /// </summary>
    public class DecayCountStrategy : IComboResetStrategy
    {
        public int DecayAmount = 1;
        public int MinCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            return UnityEngine.Mathf.Max(MinCount, currentCount - DecayAmount);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 百分比递减策略
    /// </summary>
    public class PercentageDecayStrategy : IComboResetStrategy
    {
        public float DecayPercent = 0.5f;
        public int MinCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            int newCount = UnityEngine.Mathf.RoundToInt(currentCount * (1f - DecayPercent));
            return UnityEngine.Mathf.Max(MinCount, newCount);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 设置特定值策略
    /// </summary>
    public class SetToSpecificStrategy : IComboResetStrategy
    {
        public int TargetCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context) => TargetCount;
        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 条件重置策略 - 根据条件决定是否重置
    /// </summary>
    public class ConditionalResetStrategy : IComboResetStrategy
    {
        public System.Func<ComboContext, bool> Condition;
        public IComboResetStrategy TrueStrategy = new ResetToZeroStrategy();
        public IComboResetStrategy FalseStrategy = new KeepCountStrategy();

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            var strategy = Condition?.Invoke(context) == true ? TrueStrategy : FalseStrategy;
            return strategy.CalculateResetCount(currentCount, context);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 自定义函数策略 - 使用委托自定义逻辑
    /// </summary>
    public class CustomResetStrategy : IComboResetStrategy
    {
        public System.Func<int, ComboContext, int> ResetFunction;
        public System.Func<ComboContext, bool> ShouldResetFunction;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            return ResetFunction?.Invoke(currentCount, context) ?? 0;
        }

        public bool ShouldReset(ComboContext context)
        {
            return ShouldResetFunction?.Invoke(context) ?? true;
        }
    }

    /// <summary>
    /// 组合策略 - 多个策略链式执行
    /// </summary>
    public class CompositeResetStrategy : IComboResetStrategy
    {
        public System.Collections.Generic.List<IComboResetStrategy> Strategies = new System.Collections.Generic.List<IComboResetStrategy>();
        public CompositeMode Mode = CompositeMode.Sequential;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            if (Strategies.Count == 0) return currentCount;

            switch (Mode)
            {
                case CompositeMode.Sequential:
                    int result = currentCount;
                    foreach (var strategy in Strategies)
                    {
                        result = strategy.CalculateResetCount(result, context);
                    }
                    return result;

                case CompositeMode.Minimum:
                    return Strategies.Min(s => s.CalculateResetCount(currentCount, context));

                case CompositeMode.Maximum:
                    return Strategies.Max(s => s.CalculateResetCount(currentCount, context));

                case CompositeMode.Average:
                    var results = Strategies.Select(s => s.CalculateResetCount(currentCount, context));
                    return UnityEngine.Mathf.RoundToInt((float)results.Average());

                default:
                    return currentCount;
            }
        }

        public bool ShouldReset(ComboContext context)
        {
            // 任一策略认为应该重置，就重置
            return Strategies.Any(s => s.ShouldReset(context));
        }
    }
}
