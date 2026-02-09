using System;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招状态类型
    /// </summary>
    public enum ComboStateType
    {
        Idle, // 待机
        Startup, // 前摇
        Active, // 判定中
        Recovery, // 后摇
        ComboWindow, // 连招窗口
        Interrupted, // 中断
    }

    /// <summary>
    /// 中断原因
    /// </summary>
    public enum InterruptReason
    {
        Damaged, // 受到伤害
        Stunned, // 被眩晕
        KnockedDown, // 被击倒
        Forced, // 强制中断
        UserCancel, // 用户取消
    }

    /// <summary>
    /// 判定框形状
    /// </summary>
    public enum HitBoxShape
    {
        Box, // 盒子
        Sphere, // 球体
        Capsule, // 胶囊体
    }

    /// <summary>
    /// 条件类型
    /// </summary>
    public enum ConditionType
    {
        ComboCount, // 连击数
        TimeWindow, // 时间窗口
        HealthPercent, // 血量百分比
        StaminaCost, // 耐力消耗
        Custom, // 自定义
    }

    /// <summary>
    /// 重置模式
    /// </summary>
    public enum ResetComboMode
    {
        ResetToZero, // 重置为0
        KeepCount, // 保持当前计数
        Decay, // 固定值递减
        PercentageDecay, // 百分比递减
        SetToSpecific, // 设置为特定值
        CustomFunction, // 自定义函数
    }

    /// <summary>
    /// 组合策略模式
    /// </summary>
    public enum CompositeMode
    {
        Sequential, // 顺序执行，前一个结果作为后一个输入
        Minimum, // 取所有策略的最小值
        Maximum, // 取所有策略的最大值
        Average, // 取平均值
    }
}
