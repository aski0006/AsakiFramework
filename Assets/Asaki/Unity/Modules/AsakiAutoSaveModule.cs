using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Asaki.Core.Simulation;
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
        private IAsakiSimulationService _simulationService;

        [AsakiInject]
        public void Init(
            IAsakiSaveSlotManager slotManager,
            IAsakiEventService eventService,
            IAsakiSimulationService simulationService
        )
        {
            _slotManager = slotManager;
            _eventService = eventService;
            _simulationService = simulationService;
        }

        public void OnInit()
        {
            _autoSaveService = new AsakiAutoSaveService();
            ((AsakiAutoSaveService)_autoSaveService).Init(_slotManager, _eventService);
            ((AsakiAutoSaveService)_autoSaveService).SetSimulationService(_simulationService);
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
