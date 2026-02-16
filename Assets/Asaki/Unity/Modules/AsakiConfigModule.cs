using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.DataTable;
using Asaki.Unity.Services.DataTable;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(150, typeof(AsakiEventBusModule))]
    public class AsakiConfigModule : IAsakiModule
    {
        private IAsakiConfigService _configService;
        private IAsakiEventService _eventService;

        [AsakiInject]
        public void Inject(IAsakiEventService eventService)
        {
            _eventService = eventService;
        }

        public void OnInit()
        {
            _configService = new AsakiConfigService(_eventService);
            AsakiContext.Register(_configService);
            _configService.OnInit();
        }

        public async UniTask OnInitAsync()
        {
            await _configService.OnInitAsync();
        }

        public void OnDispose()
        {
            _configService.OnDispose();
        }
    }
}
