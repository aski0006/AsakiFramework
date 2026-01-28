using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.Logging;

namespace Asaki.Unity.Bootstrapper
{
    /// <summary>
    /// [Asaki V5] 全局注入总线
    /// <para>负责聚合所有程序集生成的注入器。</para>
    /// </summary>
    public static class AsakiGlobalInjector
    {
        // 持有所有已注册的注入器 (Framework, Game, DLCs...)
        private static readonly List<IAsakiInjector> _injectors = new List<IAsakiInjector>();

        /// <summary>
        /// [由生成的代码调用] 注册一个新的程序集注入器
        /// </summary>
        public static void Register(IAsakiInjector injector)
        {
            if (injector == null || _injectors.Contains(injector))
                return;
            _injectors.Add(injector);
            ALog.Info($"[Asaki] Registered injector: {injector.GetType().Name}");
        }

        /// <summary>
        /// [核心入口] 对目标对象执行全量注入
        /// </summary>
        public static void Inject(object target, IAsakiResolver resolver = null)
        {
            if (target == null)
                return;

            foreach (IAsakiInjector injector in _injectors)
            {
                // 将 resolver 透传给具体生成的注入器
                injector.Inject(target, resolver);
            }
        }
    }
}
