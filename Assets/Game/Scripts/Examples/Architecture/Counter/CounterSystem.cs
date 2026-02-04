using Asaki.Core.Architecture;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Game.Scripts.Examples.Architecture.Counter
{
    public class CounterSystem : IAsakiSystem
    {
        private readonly CounterModel _model;

        public CounterSystem(CounterModel model)
        {
            _model = model;
        }

        public void Setup()
        {
            ALog.Info("CounterSystem Started.");
        }

        public void Increment()
        {
            _model.count.Value++;
            ALog.Info($"Count incremented to: {_model.count.Value}");
        }

        public void Dispose()
        {
            ALog.Info("CounterSystem Disposed.");
        }
    }
}
