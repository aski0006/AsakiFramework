using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Simulation;
using Asaki.Core.Time;
using Asaki.Unity.Services.Time;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(200, typeof(AsakiSimulationModule))]
    public class AsakiTimeModule : IAsakiModule
    {
        private IAsakiTimerService _asakiTimerService;
        private IAsakiSimulationService _simulation;

        [AsakiInject]
        public void Init(IAsakiSimulationService simulation)
        {
            _simulation = simulation;
        }

        public void OnInit()
        {
            _asakiTimerService = new AsakiTimerService();
            _simulation.Register(_asakiTimerService);
            AsakiContext.Register<IAsakiTimerService>(_asakiTimerService);
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            _simulation?.Unregister(_asakiTimerService);
            _asakiTimerService.Dispose();
            _simulation = null;
            _asakiTimerService = null;
        }
    }
}
