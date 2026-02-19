using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Audio;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    /// <summary>
    /// 音频模块
    /// <para>负责音频服务的初始化和生命周期管理。</para>
    /// <para>优先级400，依赖资源模块和对象池模块。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    [AsakiModule(
        400,
        typeof(AsakiResourcesModule),
        typeof(AsakiPoolModule),
        typeof(AsakiEventBusModule)
    )]
    public class AsakiAudioModule
        : IAsakiModule,
            IAsakiInject<IAsakiResourceService, IAsakiPoolService>
    {
        private IAsakiAudioService _audioService;
        private IAsakiResourceService _resourceService;
        private IAsakiPoolService _poolService;

        [AsakiInject]
        public void Inject(IAsakiResourceService resource, IAsakiPoolService poolService)
        {
            _resourceService = resource;
            _poolService = poolService;
        }

        public void OnInit()
        {
            var frameworkSetting = AsakiContext.Get<AsakiFrameworkSetting>();
            if (frameworkSetting == null)
                return;

            _audioService = new AsakiAudioService(
                _poolService,
                _resourceService,
                frameworkSetting.AudioConfig
            );

            _audioService.OnInit();
            AsakiContext.Register(_audioService);
        }

        public async UniTask OnInitAsync()
        {
            if (_audioService != null)
            {
                await _audioService.OnInitAsync();
            }
        }

        public void OnDispose()
        {
            _audioService?.OnDispose();
        }
    }
}
