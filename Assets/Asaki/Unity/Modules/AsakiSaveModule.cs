using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Configuration;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(100, typeof(AsakiEventBusModule), typeof(AsakiConfigModule))]
    public class AsakiSaveModule : IAsakiModule
    {
        private IAsakiSaveService _asakiSaveService;
        private IAsakiEventService _eventService;
        private AsakiSaveConfig _saveConfig;

        [AsakiInject]
        public void Init(IAsakiEventService eventService)
        {
            _eventService = eventService;
            // 从配置服务获取存档配置，如果不存在则使用默认配置
            if (AsakiContext.TryGet(out AsakiConfig asakiConfig))
            {
                _saveConfig = asakiConfig.SaveConfig;
            }
            _saveConfig ??= new AsakiSaveConfig();
        }

        public void OnInit()
        {
            _asakiSaveService = new AsakiSaveService(_eventService, _saveConfig);
            _asakiSaveService.OnInit();
            AsakiContext.Register(_asakiSaveService);
        }

        public async UniTask OnInitAsync()
        {
            await _asakiSaveService.OnInitAsync();
        }

        public void OnDispose()
        {
            _asakiSaveService.OnDispose();
        }
    }
}
