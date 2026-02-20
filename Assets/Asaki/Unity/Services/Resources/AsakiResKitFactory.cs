using System;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Resources.Lookup;
using Asaki.Unity.Services.Resources.Strategies;

namespace Asaki.Unity.Services.Resources
{
    /// <summary>
    /// 资源服务工厂
    /// <para>负责组装Strategy、Lookup和Service，产出可用的IAsakiResourceService实例。</para>
    /// <para>采用工厂模式封装复杂的对象创建逻辑。</para>
    /// </summary>
    /// <remarks>
    /// <para>支持的运行模式：</para>
    /// <list type="bullet">
    /// <item><description>Resources：Unity原生Resources，适用于开发期</description></item>
    /// <item><description>Addressables：Unity Addressables系统，适用于生产环境</description></item>
    /// <item><description>Custom：自定义加载方式，需通过RegisterCustom注册</description></item>
    /// </list>
    /// <para>使用示例：</para>
    /// <code>
    /// // 创建Resources模式服务
    /// var service = AsakiResKitFactory.Create(
    ///     AsakiResKitMode.Resources,
    ///     asyncService,
    ///     eventService
    /// );
    /// 
    /// // 注册自定义策略
    /// AsakiResKitFactory.RegisterCustom(
    ///     () => new MyCustomStrategy(),
    ///     () => new MyCustomLookup()
    /// );
    /// var customService = AsakiResKitFactory.Create(
    ///     AsakiResKitMode.Custom,
    ///     asyncService,
    ///     eventService
    /// );
    /// </code>
    /// </remarks>
    public static class AsakiResKitFactory
    {
        private static Func<IAsakiResStrategy> _customStrategyBuilder;
        private static Func<IAsakiResDependencyLookup> _customLookupBuilder;

        /// <summary>
        /// 注册自定义策略
        /// <para>用于扩展支持AssetBundle或其他自定义加载方式。</para>
        /// </summary>
        /// <param name="strategyBuilder">策略实例创建委托</param>
        /// <param name="lookupBuilder">依赖查询实例创建委托（可选，默认使用空实现）</param>
        public static void RegisterCustom(
            Func<IAsakiResStrategy> strategyBuilder,
            Func<IAsakiResDependencyLookup> lookupBuilder = null
        )
        {
            _customStrategyBuilder = strategyBuilder ?? throw new ArgumentNullException(nameof(strategyBuilder));
            _customLookupBuilder = lookupBuilder;
        }

        /// <summary>
        /// 清除自定义策略注册
        /// </summary>
        public static void ClearCustom()
        {
            _customStrategyBuilder = null;
            _customLookupBuilder = null;
        }

        /// <summary>
        /// 创建资源服务实例
        /// </summary>
        /// <param name="mode">运行模式</param>
        /// <param name="asyncService">异步驱动服务（必须已初始化）</param>
        /// <param name="eventService">事件服务（必须已初始化）</param>
        /// <returns>初始化完成的资源服务实例</returns>
        /// <exception cref="ArgumentNullException">asyncService为null时抛出</exception>
        /// <exception cref="NotSupportedException">Addressables模式但未定义编译宏时抛出</exception>
        /// <exception cref="InvalidOperationException">Custom模式但未注册自定义策略时抛出</exception>
        public static IAsakiResourceService Create(
            AsakiResKitMode mode,
            IAsakiAsyncService asyncService,
            IAsakiEventService eventService
        )
        {
            if (asyncService == null)
                throw new ArgumentNullException(nameof(asyncService), "AsyncService cannot be null.");

            IAsakiResStrategy strategy;
            IAsakiResDependencyLookup lookup;

            switch (mode)
            {
                case AsakiResKitMode.Resources:
                    strategy = new AsakiResourcesStrategy(asyncService);
                    lookup = AsakiNullResDependencyLookup.Instance;
                    break;

                case AsakiResKitMode.Addressables:
#if ASAKI_USE_ADDRESSABLES
                    strategy = new AsakiAddressablesStrategy(asyncService);
                    lookup = AsakiNullResDependencyLookup.Instance;
#else
                    throw new NotSupportedException(
                        "Addressables mode requires 'ASAKI_USE_ADDRESSABLES' macro and Addressables package installed."
                    );
#endif
                    break;

                case AsakiResKitMode.Custom:
                    if (_customStrategyBuilder == null)
                        throw new InvalidOperationException("Custom mode selected but no custom strategy registered.");

                    strategy = _customStrategyBuilder();
                    lookup = _customLookupBuilder?.Invoke() ?? AsakiNullResDependencyLookup.Instance;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Unsupported mode: {mode}");
            }

            return new AsakiResourceService(strategy, asyncService, lookup);
        }
    }
}
