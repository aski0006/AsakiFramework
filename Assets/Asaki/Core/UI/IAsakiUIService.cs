using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.UI
{
    /// <summary>
    /// UI服务接口，提供窗口管理、导航控制和生命周期管理功能。
    /// </summary>
    /// <remarks>
    /// <para>IAsakiUIService 是 UI 系统的核心接口，负责：</para>
    /// <list type="bullet">
    /// <item><description>窗口的打开、关闭和替换操作</description></item>
    /// <item><description>导航栈管理（Back、BackTo、ClearStack）</description></item>
    /// <item><description>窗口状态查询和实例获取</description></item>
    /// </list>
    /// </remarks>
    public interface IAsakiUIService : IAsakiModule
    {
        /// <summary>
        /// 异步打开指定ID的UI窗口。
        /// </summary>
        /// <typeparam name="T">窗口类型，必须实现 IAsakiWindow 接口</typeparam>
        /// <param name="uiId">UI配置ID，对应 UIConfig 中的配置项</param>
        /// <param name="args">传递给窗口 OnOpenAsync 方法的参数对象</param>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>打开的窗口实例；如果打开失败或被取消，返回 null</returns>
        UniTask<T> OpenAsync<T>(int uiId, object args = null, CancellationToken token = default)
            where T : class, IAsakiWindow;

        /// <summary>
        /// 关闭指定类型的窗口。
        /// </summary>
        /// <typeparam name="T">要关闭的窗口类型</typeparam>
        void Close<T>()
            where T : class, IAsakiWindow;

        /// <summary>
        /// 关闭指定的窗口实例。
        /// </summary>
        /// <param name="window">要关闭的窗口实例</param>
        void Close(IAsakiWindow window);

        /// <summary>
        /// 返回到导航栈的上一级窗口（关闭当前栈顶窗口）。
        /// </summary>
        void Back();

        /// <summary>
        /// 返回到指定类型的窗口，关闭该窗口上方的所有窗口。
        /// </summary>
        /// <typeparam name="T">目标窗口类型</typeparam>
        void BackTo<T>()
            where T : class, IAsakiWindow;

        /// <summary>
        /// 返回到指定ID的窗口，关闭该窗口上方的所有窗口。
        /// </summary>
        /// <param name="uiId">目标窗口的UI ID</param>
        void BackTo(int uiId);

        /// <summary>
        /// 返回到上一级窗口并传递返回值。
        /// </summary>
        /// <param name="returnValue">传递给上一级窗口的返回值</param>
        /// <returns>异步任务</returns>
        UniTask Back(object returnValue);

        /// <summary>
        /// 清空导航栈。
        /// </summary>
        /// <param name="includePopup">是否同时关闭所有 Popup 层窗口</param>
        void ClearStack(bool includePopup = false);

        /// <summary>
        /// 异步替换当前栈顶窗口（先关闭栈顶，再打开新窗口）。
        /// </summary>
        /// <typeparam name="T">新窗口类型</typeparam>
        /// <param name="uiId">新窗口的UI ID</param>
        /// <param name="args">传递给新窗口的参数</param>
        /// <param name="token">取消令牌</param>
        /// <returns>新打开的窗口实例</returns>
        UniTask<T> ReplaceAsync<T>(int uiId, object args = null, CancellationToken token = default)
            where T : class, IAsakiWindow;

        /// <summary>
        /// 检查指定ID的窗口是否已打开。
        /// </summary>
        /// <param name="uiId">窗口ID</param>
        /// <returns>如果窗口已打开返回 true，否则返回 false</returns>
        bool IsOpened(int uiId);

        /// <summary>
        /// 获取指定类型的已打开窗口实例。
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <returns>窗口实例；如果未找到返回 null</returns>
        T GetWindow<T>()
            where T : class, IAsakiWindow;

        /// <summary>
        /// 获取指定ID的窗口实例。
        /// </summary>
        /// <param name="uiId">窗口ID</param>
        /// <returns>窗口实例；如果未找到返回 null</returns>
        IAsakiWindow GetWindow(int uiId);

        /// <summary>
        /// 获取所有已打开的窗口列表。
        /// </summary>
        /// <param name="layer">可选的层级过滤条件</param>
        /// <returns>已打开窗口的只读列表</returns>
        IReadOnlyList<IAsakiWindow> GetOpenedWindows(AsakiUILayer? layer = null);

        /// <summary>
        /// 检查是否存在已打开的 Popup 层窗口。
        /// </summary>
        /// <returns>如果存在 Popup 窗口返回 true，否则返回 false</returns>
        bool HasPopup();

        /// <summary>
        /// 获取指定层级的活动窗口数量。
        /// </summary>
        /// <param name="layer">目标层级</param>
        /// <returns>该层级的窗口数量</returns>
        int GetActiveWindowCount(AsakiUILayer layer);
    }
}
