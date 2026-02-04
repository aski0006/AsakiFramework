using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    /// <summary>
    /// 自动保存服务模块
    /// </summary>
    [AsakiModule(priority: 210, typeof(AsakiSaveSlotModule))]
    public class AsakiAutoSaveModule : IAsakiModule
    {
        private IAsakiAutoSaveService _autoSaveService;
        private IAsakiSaveSlotManager _slotManager;
        private IAsakiEventService _eventService;

        [AsakiInject]
        public void Init(IAsakiSaveSlotManager slotManager, IAsakiEventService eventService)
        {
            _slotManager = slotManager;
            _eventService = eventService;
        }

        public void OnInit()
        {
            _autoSaveService = new AsakiAutoSaveService();
            ((AsakiAutoSaveService)_autoSaveService).Init(_slotManager, _eventService);
            _autoSaveService.OnInit();
            
            AsakiContext.Register<IAsakiAutoSaveService>(_autoSaveService);
        }

        public async UniTask OnInitAsync()
        {
            await _autoSaveService.OnInitAsync();
        }

        public void OnDispose()
        {
            _autoSaveService?.OnDispose();
        }
    }
}
