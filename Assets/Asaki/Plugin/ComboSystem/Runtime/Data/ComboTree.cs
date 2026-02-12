using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem
{
    /// <summary>
    /// 重置策略定义
    /// </summary>
    [Serializable]
    public class ResetStrategyDefinition
    {
        public string GroupName;
        public ResetComboMode Mode;

        // Decay模式参数
        public int DecayAmount = 1;
        public float DecayPercent = 0.5f;
        public int MinCount = 0;

        // SetToSpecific模式参数
        public int SpecificValue = 0;

        // CustomFunction模式参数
        [NonSerialized]
        public Func<int, ComboContext, int> CustomResetFunction;

        [NonSerialized]
        public Func<ComboContext, bool> CustomShouldResetFunction;
    }

    /// <summary>
    /// 连招树 - 包含所有招式和转换关系
    /// </summary>
    [CreateAssetMenu(fileName = "ComboTree", menuName = "Asaki/ComboSystem/ComboTree")]
    public class ComboTree : ScriptableObject
    {
        [Header("Info")]
        public string TreeId;
        public string Description;

        [Header("Moves")]
        public ComboMove[] Moves;

        [Header("Transitions")]
        public ComboTransition[] Transitions;

        [Header("Settings")]
        public float InputBufferWindow = 0.3f;
        public float MaxComboDuration = 10f;
        public int MaxComboLength = 10;

        [Header("Reset Strategies")]
        public ResetStrategyDefinition[] ResetStrategies;

        [Header("Default Reset")]
        public ResetComboMode DefaultResetMode = ResetComboMode.ResetToZero;

        // 运行时查找表
        private Dictionary<string, ComboMove> _moveLookup;
        private Dictionary<string, List<ComboTransition>> _transitionLookup;
        private Dictionary<string, IComboResetStrategy> _strategyCache;

        void OnEnable()
        {
            BuildLookupTables();
            BuildResetStrategies();
        }

        void BuildLookupTables()
        {
            _moveLookup = Moves?.ToDictionary(m => m.MoveId) ?? new Dictionary<string, ComboMove>();

            _transitionLookup = new Dictionary<string, List<ComboTransition>>();
            if (Transitions != null)
            {
                foreach (var t in Transitions)
                {
                    if (!_transitionLookup.ContainsKey(t.FromMoveId))
                        _transitionLookup[t.FromMoveId] = new List<ComboTransition>();
                    _transitionLookup[t.FromMoveId].Add(t);
                }
            }
        }

        void BuildResetStrategies()
        {
            _strategyCache = new Dictionary<string, IComboResetStrategy>();
            if (ResetStrategies != null)
            {
                foreach (var def in ResetStrategies)
                {
                    _strategyCache[def.GroupName] = CreateStrategy(def);
                }
            }
        }

        IComboResetStrategy CreateStrategy(ResetStrategyDefinition def)
        {
            return def.Mode switch
            {
                ResetComboMode.ResetToZero => new ResetToZeroStrategy(),
                ResetComboMode.KeepCount => new KeepCountStrategy(),
                ResetComboMode.Decay => new DecayCountStrategy
                {
                    DecayAmount = def.DecayAmount,
                    MinCount = def.MinCount,
                },
                ResetComboMode.PercentageDecay => new PercentageDecayStrategy
                {
                    DecayPercent = def.DecayPercent,
                    MinCount = def.MinCount,
                },
                ResetComboMode.SetToSpecific => new SetToSpecificStrategy
                {
                    TargetCount = def.SpecificValue,
                },
                ResetComboMode.CustomFunction => new CustomResetStrategy
                {
                    ResetFunction = def.CustomResetFunction,
                    ShouldResetFunction = def.CustomShouldResetFunction,
                },
                _ => new ResetToZeroStrategy(),
            };
        }

        public ComboMove GetMove(string moveId) => _moveLookup?.GetValueOrDefault(moveId);

        public List<ComboTransition> GetTransitions(string fromMoveId) =>
            _transitionLookup?.GetValueOrDefault(fromMoveId) ?? new List<ComboTransition>();

        /// <summary>
        /// 根据输入类型查找下一个招式
        /// </summary>
        public ComboMove FindNextMove(string currentMoveId, string inputTypeId)
        {
            var transitions = GetTransitions(currentMoveId);
            return transitions
                .Where(t => t.InputType == inputTypeId && t.IsValid())
                .Select(t => GetMove(t.ToMoveId))
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取指定招式的所有可用转换
        /// </summary>
        public List<ComboTransition> GetAvailableTransitions(string currentMoveId)
        {
            return GetTransitions(currentMoveId).Where(t => t.IsValid()).ToList();
        }

        /// <summary>
        /// 应用重置策略
        /// </summary>
        public int ApplyResetStrategy(string groupName, int currentCount, ComboContext context)
        {
            if (_strategyCache.TryGetValue(groupName, out var strategy))
            {
                if (strategy.ShouldReset(context))
                {
                    return strategy.CalculateResetCount(currentCount, context);
                }
                return currentCount;
            }

            // 默认重置为0
            return 0;
        }
    }
}
