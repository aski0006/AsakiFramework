using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    /// <summary>
    /// 保存槽位管理模块
    /// </summary>
    [AsakiModule(priority: 160, typeof(AsakiSaveModule))]
    public class AsakiSaveSlotModule : IAsakiModule
    {
        private IAsakiSaveSlotManager _slotManager;
        private IAsakiSaveService _saveService;
        private IAsakiEventService _eventService;

        [AsakiInject]
        public void Init(IAsakiSaveService saveService, IAsakiEventService eventService)
        {
            _saveService = saveService;
            _eventService = eventService;
        }

        public void OnInit()
        {
            _slotManager = new AsakiSaveSlotManager();
            ((AsakiSaveSlotManager)_slotManager).Init(_saveService, _eventService);
            _slotManager.OnInit();

            AsakiContext.Register<IAsakiSaveSlotManager>(_slotManager);
        }

        public async UniTask OnInitAsync()
        {
            await _slotManager.OnInitAsync();
        }

        public void OnDispose()
        {
            _slotManager?.OnDispose();
        }
    }
}
