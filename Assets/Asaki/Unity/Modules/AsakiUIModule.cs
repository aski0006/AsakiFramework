using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    // 优先级 300，依赖 Resources 加载预制体
    [AsakiModule(
        225,
        typeof(AsakiResourcesModule),
        typeof(AsakiPoolModule),
        typeof(AsakiEventBusModule),
        typeof(AsakiSimulationModule)
    )]
    public class AsakiUIModule
        : IAsakiModule,
            IAsakiInject<IAsakiEventService, IAsakiResourceService, IAsakiPoolService>
    {
        private AsakiUIManageService _uiManageService;
        private IAsakiEventService _eventService;
        private IAsakiResourceService _resourceService;
        private IAsakiPoolService _poolService;

        [AsakiInject]
        public void Inject(
            IAsakiEventService eventService,
            IAsakiResourceService resourceService,
            IAsakiPoolService poolService
        )
        {
            _eventService = eventService;
            _resourceService = resourceService;
            _poolService = poolService;
        }

        public void OnInit()
        {
            AsakiFrameworkSetting frameworkSetting = AsakiContext.Get<AsakiFrameworkSetting>();
            // 如果没配置 UI，直接跳过
            if (!frameworkSetting)
                return;

            _uiManageService = new AsakiUIManageService(
                frameworkSetting.UIConfig,
                frameworkSetting.UIConfig.ReferenceResolution,
                frameworkSetting.UIConfig.MatchWidthOrHeight,
                _eventService,
                _resourceService,
                _poolService
            );

            // 内部 OnInit 会调用 Resources 接口，此时 Resources 已注册
            _uiManageService.OnInit();

            // 应用配置化的诊断开关
            _uiManageService.DiagnosticsEnabled = frameworkSetting.UIConfig.EnableDiagnostics;

            AsakiContext.Register<IAsakiUIService>(_uiManageService);
        }

        public async UniTask OnInitAsync()
        {
            if (_uiManageService != null)
            {
                await _uiManageService.OnInitAsync();
            }
        }

        public void OnDispose() { }
    }
}
