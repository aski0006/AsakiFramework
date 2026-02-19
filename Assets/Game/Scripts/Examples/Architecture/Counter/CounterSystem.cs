using Asaki.Core.Architecture;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples.Architecture.Counter
{
    public class CounterSystem : IAsakiSystem
    {
        private readonly CounterModel _model;

        public CounterSystem(CounterModel model)
        {
            _model = model;
        }

        public void Create() { }

        public void Increment()
        {
            _model.count.Value++;
            ALog.Info($"Count incremented to: {_model.count.Value}");
        }

        public void Dispose()
        {
            ALog.Info("CounterSystem Disposed.");
        }

        public void Start()
        {
            ALog.Info("CounterSystem Started.");
        }
    }
}
