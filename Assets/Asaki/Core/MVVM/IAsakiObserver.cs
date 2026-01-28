namespace Asaki.Core.MVVM
{
    /// <summary>
    /// Asaki MVVM框架的观察者接口，用于接收可观察对象的值变化通知。
    /// </summary>
    /// <typeparam name="T">观察的值类型</typeparam>
    /// <remarks>
    /// 该接口是Asaki MVVM框架的核心组件之一，实现了观察者设计模式。
    /// 当<see cref="AsakiProperty{T}"/>等可观察对象的值发生变化时，会通知所有实现了该接口的观察者。
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyObserver : IAsakiObserver&lt;int&gt;
    /// {
    ///     public void OnValueChange(int value)
    ///     {
    ///         Debug.Log($"值已更新: {value}");
    ///     }
    /// }
    ///
    /// // 使用示例
    /// var property = new AsakiProperty&lt;int&gt;(5);
    /// var observer = new MyObserver();
    /// property.Bind(observer);
    /// property.Value = 10; // 输出: 值已更新: 10
    /// </code>
    /// </example>
    /// <seealso cref="AsakiProperty{T}"/>
    public interface IAsakiObserver<T>
    {
        /// <summary>
        /// 当观察的值发生变化时调用的方法。
        /// </summary>
        /// <param name="value">变化后的新值</param>
        /// <remarks>
        /// 实现该方法以响应可观察对象的值变化，执行相应的业务逻辑或UI更新。
        /// </remarks>
        void OnValueChange(T value);
    }
}
