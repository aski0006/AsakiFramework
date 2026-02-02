using Asaki.Core.Architecture;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Game.Scripts.Examples.Architecture.Counter
{
    public class CounterSystem : IAsakiSystem, IAsakiTickable
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

        public void Tick(float deltaTime)
        {
            // 简单的测试：每 100 帧打印一次，证明 Tick 在运行
            if (UnityEngine.Time.frameCount % 100 == 0)
            {
                ALog.Trace($"[System Heartbeat] Count is {_model.count.Value}");
            }
        }

        public void Dispose()
        {
            ALog.Info("CounterSystem Disposed.");
        }
    }
}
