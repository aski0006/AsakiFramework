using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 对象池工厂接口 - 定义对象的创建、获取、归还和销毁行为
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiPoolObjectFactory<T>
        where T : class
    {
        /// <summary>
        /// 异步创建对象
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>创建的对象</returns>
        UniTask<T> CreateAsync(CancellationToken token = default);

        /// <summary>
        /// 同步创建对象 - 用于不需要异步加载的场景
        /// </summary>
        /// <returns>创建的对象</returns>
        T CreateSync();

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
}
