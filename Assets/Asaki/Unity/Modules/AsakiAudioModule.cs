using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Audio;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    // 优先级 400，通常依赖 Resources 加载音频
    [AsakiModule(
        400,
        typeof(AsakiResourcesModule),
        typeof(AsakiPoolModule),
        typeof(AsakiEventBusModule)
    )]
    public class AsakiAudioModule
        : IAsakiModule,
            IAsakiInit<IAsakiResourceService, IAsakiPoolService>
    {
        private IAsakiAudioService _audioService;
        private IAsakiResourceService _resService;
        private IAsakiPoolService _poolService;

        [AsakiInject]
        public void Init(IAsakiResourceService resource, IAsakiPoolService poolService)
        {
            _resService = resource;
            _poolService = poolService;
        }

        public void OnInit()
        {
            AsakiFrameworkSetting frameworkSetting = AsakiContext.Get<AsakiFrameworkSetting>();
            if (!frameworkSetting)
                return;

            _audioService = new AsakiAudioService(
                _poolService,
                _resService,
                frameworkSetting.AudioConfig
            );

            _audioService.OnInit();

            AsakiContext.Register(_audioService);
        }

        public async UniTask OnInitAsync()
        {
            await _audioService.OnInitAsync();
        }

        public void OnDispose()
        {
            _audioService.OnDispose();
        }
    }
}
