using Asaki.Core.Architecture.Interfaces;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Game.Examples.Architecture.Counter
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
			_model.Count.Value++;
			ALog.Info($"Count incremented to: {_model.Count.Value}");
		}
		public void Tick(float deltaTime)
		{
			// 简单的测试：每 100 帧打印一次，证明 Tick 在运行
			if (UnityEngine.Time.frameCount % 100 == 0)
			{
				UnityEngine.Debug.Log($"[System Heartbeat] Count is {_model.Count.Value}");
			}
		}
		public void Dispose()
		{
			ALog.Info("CounterSystem Disposed.");
		}
	}
}
