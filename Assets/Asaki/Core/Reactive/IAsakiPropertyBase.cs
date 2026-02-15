using System;

namespace Asaki.Core.Reactive
{
    /// <summary>
    /// Asaki 可观察属性的非泛型基接口，提供类型擦除后的统一访问能力。
    /// </summary>
    /// <remarks>
    /// <para>该接口是 <see cref="AsakiProperty{T}"/> 的非泛型抽象，
    /// 允许在不知道具体类型参数的情况下操作可观察属性。</para>
    /// <para>主要用途：</para>
    /// <list type="bullet">
    /// <item>在数据绑定系统中统一处理不同类型的属性</item>
    /// <item>支持反射或动态调用场景</item>
    /// <item>提供类型安全的值回调机制</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 通过接口访问属性
    /// IAsakiPropertyBase property = new AsakiProperty&lt;int&gt;(10);
    ///
    /// // 获取值类型
    /// Type valueType = property.ValueType; // typeof(int)
    ///
    /// // 通过回调设置值
    /// property.InvokeCallback(20);
    ///
    /// // 释放资源
    /// property.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="AsakiProperty{T}"/>
    public interface IAsakiPropertyBase : IDisposable
    {
        /// <summary>
        /// 获取属性值的类型。
        /// </summary>
        /// <value>属性值的 <see cref="Type"/>，即泛型参数 T 的类型。</value>
        /// <example>
        /// <code>
        /// var intProperty = new AsakiProperty&lt;int&gt;(5);
        /// IAsakiPropertyBase baseProperty = intProperty;
        /// Debug.Log(baseProperty.ValueType.Name); // 输出: Int32
        /// </code>
        /// </example>
        Type ValueType { get; }

        /// <summary>
        /// 使用类型擦除的方式触发值变化回调。
        /// </summary>
        /// <param name="value">新的属性值，将被强制转换为 T 类型。</param>
        /// <remarks>
        /// <para>该方法用于在不知道具体类型参数的情况下更新属性值并触发通知。</para>
        /// <para>如果 <paramref name="value"/> 无法转换为 T 类型，则不会执行任何操作。</para>
        /// <para>⚠️ 注意：此方法会跳过值相等性检查，直接触发通知。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// IAsakiPropertyBase property = new AsakiProperty&lt;float&gt;(1.0f);
        /// property.InvokeCallback(2.5f); // 更新值并通知订阅者
        /// </code>
        /// </example>
        void InvokeCallback(object value);
    }
}
