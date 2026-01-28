using System;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Resources.Lookup;
using Asaki.Unity.Services.Resources.Strategies;

namespace Asaki.Unity.Services.Resources
{
    /// <summary>
    /// [Resources 工厂]
    /// 负责组装 Strategy, Lookup 和 Service，产出可用的 IAsakiResourceService。
    /// </summary>
    public static class AsakiResKitFactory
    {
        // 用于存储自定义策略的构建器 (针对 Custom 模式)
        private static Func<IAsakiResStrategy> _customStrategyBuilder;
        private static Func<IAsakiResDependencyLookup> _customLookupBuilder;

        /// <summary>
        /// 注册自定义策略 (如果你想用 AssetBundle 或其他方案)
        /// </summary>
        public static void RegisterCustom(
            Func<IAsakiResStrategy> strategyBuilder,
            Func<IAsakiResDependencyLookup> lookupBuilder = null
        )
        {
            _customStrategyBuilder = strategyBuilder;
            _customLookupBuilder = lookupBuilder;
        }

        /// <summary>
        /// 创建 Resources 服务实例
        /// </summary>
        /// <param name="mode">运行模式</param>
        /// <param name="asyncService">异步驱动服务 (必须已初始化)</param>
        /// <param name="eventService">事件服务 (必须已初始化)</param>
        /// <returns>初始化好的资源服务</returns>
        public static IAsakiResourceService Create(
            AsakiResKitMode mode,
            IAsakiAsyncService asyncService,
            IAsakiEventService eventService
        )
        {
            if (asyncService == null)
                throw new ArgumentNullException(
                    nameof(asyncService),
                    "[ResKitFactory] RoutineService cannot be null."
                );

            IAsakiResStrategy strategy;
            IAsakiResDependencyLookup lookup;

            switch (mode)
            {
                case AsakiResKitMode.Resources:
                    // 策略：原生 Resources
                    strategy = new AsakiResourcesStrategy(asyncService);
                    // 依赖：Resources 自动管理，不需要手动 Lookup
                    lookup = AsakiNullResDependencyLookup.Instance;
                    break;

                case AsakiResKitMode.Addressables:
#if ASAKI_USE_ADDRESSABLES
                    // 策略：Addressables
                    strategy = new AsakiAddressablesStrategy(asyncService);
                    // 依赖：Addressables 内部 Catalog 自动管理
                    lookup = AsakiNullResDependencyLookup.Instance;
#else
                    throw new NotSupportedException(
                        "[ResKitFactory] Addressables mode requires 'ASAKI_USE_ADDRESSABLES' macro and Addressables package installed."
                    );
#endif
                    break;

                case AsakiResKitMode.Custom:
                    if (_customStrategyBuilder == null)
                        throw new InvalidOperationException(
                            "[ResKitFactory] Custom mode selected but no custom strategy registered."
                        );

                    strategy = _customStrategyBuilder();
                    // 如果没提供 lookup，默认给个空的
                    lookup =
                        _customLookupBuilder != null
                            ? _customLookupBuilder()
                            : AsakiNullResDependencyLookup.Instance;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            // 组装并返回
            AsakiResourceService service = new AsakiResourceService(strategy, asyncService, lookup);
            return service;
        }
    }
}
