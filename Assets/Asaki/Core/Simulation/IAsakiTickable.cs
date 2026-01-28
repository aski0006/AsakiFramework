using Asaki.Core.Context;

namespace Asaki.Core.Simulation
{
    public interface IAsakiTickable
    {
        // 注入非托管的时间增量
        void Tick(float deltaTime);
    }

    public interface IAsakiFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }

    public interface IAsakiLateTickable
    {
        void LateTick(float lateDeltaTime);
    }

    // [新增] 优先级定义 (数值越小越先执行)
    public enum TickPriority
    {
        High = 0, // Input, Sensors
        Normal = 1000, // Game Logic, FSM
        Low = 2000, // UI, Audio, View Sync
    }
}
