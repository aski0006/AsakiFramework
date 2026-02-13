using System.Threading.Tasks;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Resources;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    // 优先级 200，且显式依赖 RoutineModule
    [AsakiModule(125, typeof(AsakiAsyncModule), typeof(AsakiEventBusModule))]
    public class AsakiResourcesModule : IAsakiModule
    {
        private IAsakiResourceService _resourceService;
        private IAsakiAsyncService _asyncService;
        private IAsakiEventService _eventService;

        [AsakiInject]
        public void Init(IAsakiAsyncService asyncService, IAsakiEventService eventService)
        {
            _asyncService = asyncService;
            _eventService = eventService;
        }

        public void OnInit()
        {
            AsakiFrameworkSetting frameworkSetting = AsakiContext.Get<AsakiFrameworkSetting>();

            // 2. 创建工厂
            AsakiResKitMode mode = frameworkSetting
                ? frameworkSetting.ResConfig.Mode
                : AsakiResKitMode.Resources;
            int timeoutSeconds = frameworkSetting.ResConfig.TimeoutSeconds;
            _resourceService = AsakiResKitFactory.Create(mode, _asyncService, _eventService);
            _resourceService.SetTimeoutSeconds(timeoutSeconds);
            // 3. 注册服务
            AsakiContext.Register(_resourceService);

            ALog.Info($"[Asaki] Resources initialized in {mode} mode.");
        }

        public async UniTask OnInitAsync()
        {
            // 4. 执行异步初始化 (加载 Manifest 等)
            if (_resourceService != null)
            {
                await _resourceService.OnInitAsync();
            }
        }

        public void OnDispose() { }
    }
}
