using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 对象池工厂基接口 - 包含所有工厂共有的生命周期方法
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiPoolObjectFactoryBase<T>
        where T : class
    {
        /// <summary>
        /// 当对象从池中获取时调用
        /// </summary>
        void OnGet(T obj);

        /// <summary>
        /// 当对象归还到池时调用
        /// </summary>
        void OnReturn(T obj);

        /// <summary>
        /// 当对象被销毁时调用
        /// </summary>
        void OnDestroy(T obj);

        /// <summary>
        /// 验证对象是否有效
        /// </summary>
        bool Validate(T obj);
    }

    /// <summary>
    /// 异步对象工厂接口 - 仅支持异步创建
    /// 适用于需要资源加载的场景（如 Addressable、Resources）
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiAsyncPoolObjectFactory<T> : IAsakiPoolObjectFactoryBase<T>
        where T : class
    {
        /// <summary>
        /// 异步创建对象
        /// </summary>
        UniTask<T> CreateAsync(CancellationToken token = default);
    }

    /// <summary>
    /// 同步对象工厂接口 - 仅支持同步创建
    /// 适用于轻量级对象或已加载资源的实例化场景
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiSyncPoolObjectFactory<T> : IAsakiPoolObjectFactoryBase<T>
        where T : class
    {
        /// <summary>
        /// 同步创建对象
        /// </summary>
        T CreateSync();
    }

    /// <summary>
    /// 对象池工厂完整接口 - 同时支持异步和同步创建
    /// 实现此接口的工厂可以同时用于异步和同步场景
    /// </summary>
    /// <remarks>
    /// 对于只需要异步或同步的场景，可以考虑实现：
    /// - <see cref="IAsakiAsyncPoolObjectFactory{T}"/> - 仅异步创建
    /// - <see cref="IAsakiSyncPoolObjectFactory{T}"/> - 仅同步创建
    /// </remarks>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiPoolObjectFactory<T>
        : IAsakiAsyncPoolObjectFactory<T>,
            IAsakiSyncPoolObjectFactory<T>
        where T : class { }
}
