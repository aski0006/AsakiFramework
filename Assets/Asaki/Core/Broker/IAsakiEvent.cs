namespace Asaki.Core.Broker
{
    /// <summary>
    /// 作为所有事件的基础接口。
    /// 任何要在事件总线系统中发布和处理的事件都应实现此接口。
    /// 它为事件提供了统一的标识，使得事件总线能够识别和处理不同类型的事件。
    /// </summary>
    public interface IAsakiEvent { }

    /// <summary>
    /// 定义处理特定类型事件的接口。
    /// 泛型类型参数 <typeparamref name="T"/> 必须实现 <see cref="IAsakiEvent"/> 接口。
    /// 实现此接口的类负责定义处理事件的具体逻辑，通过 <see cref="OnEvent(in T)"/> 方法来响应相应的事件。
    /// </summary>
    /// <typeparam name="T">要处理的事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
    public interface IAsakiHandler<T>
        where T : IAsakiEvent
    {
        /// <summary>
        /// 处理事件的方法。
        /// 当对应的事件被发布到事件总线时，实现此接口的对象的该方法将被调用，
        /// 以执行对该事件的具体处理逻辑。
        /// </summary>
        /// <param name="e">要处理的事件实例（只读引用传递）。</param>
        void OnEvent(in T e);
    }
}
