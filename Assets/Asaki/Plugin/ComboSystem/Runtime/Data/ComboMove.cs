using System;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem
{
    /// <summary>
    /// 判定框定义 - 纯数据
    /// </summary>
    [Serializable]
    public class HitBoxDefinition
    {
        public string HitBoxId;
        public HitBoxShape Shape;
        public Vector3 Offset;
        public Vector3 Size; // Box用
        public float Radius; // Sphere/Capsule用
        public float Height; // Capsule用
        public string BoneName; // 跟随的骨骼名称
    }

    /// <summary>
    /// 招式数据定义 - 纯数据，无逻辑
    /// </summary>
    [Serializable]
    public class ComboMove
    {
        [Header("Basic")]
        public string MoveId;
        public string MoveName;

        [Header("Animation")]
        public string AnimationStateName;
        public float AnimationSpeed = 1f;

        [Header("Timing")]
        public float StartupTime; // 前摇时间（从动画开始到判定开始）
        public float ActiveDuration; // 判定持续时间
        public float RecoveryTime; // 后摇时间
        public float ComboWindowStart; // 连招窗口开始时间（相对于动画开始）
        public float ComboWindowEnd; // 连招窗口结束时间

        [Header("Hit Boxes")]
        public HitBoxDefinition[] HitBoxes;

        [Header("Requirements")]
        public int MinComboCount; // 最小连击数要求
        public int MaxComboCount; // 最大连击数限制
        public float Cooldown; // 冷却时间

        // 运行时数据
        [NonSerialized]
        public float LastUsedTime = -999f;

        public bool IsOnCooldown(float currentTime) => currentTime - LastUsedTime < Cooldown;
    }

    /// <summary>
    /// 转换条件
    /// </summary>
    [Serializable]
    public class TransitionCondition
    {
        public ConditionType Type;
        public string Parameter;
        public float Value;
    }

    /// <summary>
    /// 连招转换
    /// </summary>
    [Serializable]
    public class ComboTransition
    {
        public string FromMoveId;
        public string ToMoveId;

        /// <summary>
        /// 输入类型ID（使用可扩展的类型系统，而非硬编码枚举）
        /// </summary>
        public string InputType = "LightAttack";

        public TransitionCondition[] Conditions;

        [Header("Reset")]
        public string ResetGroup = "default"; // 使用哪个重置策略组

        public bool IsValid() =>
            !string.IsNullOrEmpty(FromMoveId) && !string.IsNullOrEmpty(ToMoveId);
    }
}
