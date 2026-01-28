using Asaki.Core.Logging;
using Asaki.Core.Simulation;
using UnityEngine;

namespace Asaki.Unity.Bridge
{
    public class AsakiMonoDriver : MonoBehaviour
    {
        private IAsakiSimulationService _simulationService;

        public void Initialize(IAsakiSimulationService simService)
        {
            _simulationService = simService;
            if (_simulationService != null)
                return;
            ALog.Error("[AsakiSimulation] SimulationManager not found in context!");
            enabled = false;
        }

        private void Update()
        {
            _simulationService?.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _simulationService?.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _simulationService?.LateTick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _simulationService = null;
        }
    }
}
