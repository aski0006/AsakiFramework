using Asaki.Core.Context;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// 服务提供者接口 - 用于解耦 Command/Query 对 Architecture 的依赖
    /// </summary>
    public interface IAsakiServiceProvider
    {
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        T GetService<T>() where T : class, IAsakiService;

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">输出服务实例</param>
        /// <returns>是否成功获取</returns>
        bool TryGetService<T>(out T service) where T : class, IAsakiService;
    }
}
