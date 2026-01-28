using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Context
{
    /// <summary>
    /// [Asaki 架构核心] 模块生命周期契约。
    /// <para>实现此接口的类必须同时标记 [AsakiModule] 特性。</para>
    /// </summary>
    public interface IAsakiModule : IAsakiService
    {
        /// <summary>
        /// [同步初始化阶段]
        /// <para>时机：模块实例被创建并注册到容器后立即调用。</para>
        /// <para>职责：</para>
        /// <list type="bullet">
        /// <item>获取配置 (AsakiContext.Get&lt;AsakiConfig&gt;)</item>
        /// <item>获取已就绪的依赖模块 (AsakiContext.Get&lt;TDependency&gt;)</item>
        /// <item>注册此模块提供的额外子服务 (AsakiContext.Register&lt;IService&gt;)</item>
        /// </list>
        /// <para>警告：严禁在此方法中再次注册模块自身 (this)，加载器已自动处理。</para>
        /// </summary>
        void OnInit();

        /// <summary>
        /// [异步初始化阶段]
        /// <para>时机：所有模块完成 OnInit 后，按 DAG 顺序依次调用。</para>
        /// <para>职责：执行耗时的异步操作，如资源加载、网络连接、数据库预热。</para>
        /// </summary>
        UniTask OnInitAsync();

        /// <summary>
        /// [销毁阶段]
        /// <para>时机：游戏退出或重启时调用。</para>
        /// <para>职责：清理非托管资源、断开连接。容器会自动清理引用，此处仅处理内部状态。</para>
        /// </summary>
        void OnDispose();
    }
}
