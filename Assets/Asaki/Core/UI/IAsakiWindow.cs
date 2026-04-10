using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.UI
{
    /// <summary>
    /// UI 窗口基础接口，定义窗口的生命周期方法。
    /// </summary>
    public interface IAsakiWindow
    {
        /// <summary>
        /// 异步打开窗口。
        /// </summary>
        /// <param name="args">打开参数</param>
        /// <param name="token">取消令牌</param>
        /// <returns>异步任务</returns>
        UniTask OnOpenAsync(object args, CancellationToken token);

        /// <summary>
        /// 异步关闭窗口。
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>异步任务</returns>
        UniTask OnCloseAsync(CancellationToken token);

        /// <summary>
        /// 窗口被覆盖时调用。
        /// </summary>
        void OnCover();

        /// <summary>
        /// 窗口重新显示时调用。
        /// </summary>
        void OnReveal();
    }

    /// <summary>
    /// 带返回值的 UI 窗口接口。
    /// </summary>
    /// <remarks>
    /// <deprecated type="method">
    /// 2.3.1 - 请使用 <see cref="IAsakiWindowWithResult{TResult}"/> 泛型接口替代。
    /// 旧版本 <c>OnReturnValue(object value)</c> 会导致装箱/拆箱操作，新接口提供类型安全的返回值处理。
    /// 迁移：将 <c>OnReturnValue(object value)</c> 改为 <c>OnReturnValue(TResult value)</c>
    /// </deprecated>
    /// </remarks>
    public interface IAsakiWindowWithResult : IAsakiWindow
    {
        /// <summary>
        /// 设置窗口返回值。
        /// </summary>
        /// <param name="value">返回值</param>
        void OnReturnValue(object value);
    }

    /// <summary>
    /// 带泛型参数的 UI 窗口接口。
    /// </summary>
    /// <typeparam name="TArg">打开参数的类型</typeparam>
    public interface IAsakiWindow<TArg> : IAsakiWindow
    {
        /// <summary>
        /// 异步打开窗口（类型安全版本）。
        /// </summary>
        /// <param name="args">打开参数</param>
        /// <param name="token">取消令牌</param>
        /// <returns>异步任务</returns>
        UniTask OnOpenAsync(TArg args, CancellationToken token);
    }

    /// <summary>
    /// 带泛型返回值的 UI 窗口接口（推荐用法）。
    /// </summary>
    /// <typeparam name="TResult">返回值的类型</typeparam>
    /// <remarks>
    /// <para>此接口提供类型安全的返回值处理，避免装箱/拆箱操作。</para>
    /// <para>示例：</para>
    /// <code>
    /// public class LoginWindow : MonoBehaviour, IAsakiWindowWithResult&lt;string&gt;
    /// {
    ///     public void OnReturnValue(string username) { ... }
    /// }
    /// </code>
    /// </remarks>
    public interface IAsakiWindowWithResult<TResult> : IAsakiWindowWithResult
    {
        /// <summary>
        /// 设置窗口返回值（类型安全版本）。
        /// </summary>
        /// <param name="value">返回值</param>
        void OnReturnValue(TResult value);
    }
}
