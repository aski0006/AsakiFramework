using Asaki.Core.Architecture.Command;
using Asaki.Core.Logging;

namespace Game.Scripts.Examples.Architecture.Counter.Commands
{
    /// <summary>
    /// 增加计数器命令
    /// 使用对象池管理，避免 GC 分配
    /// </summary>
    public class IncrementCounterCommand : AsakiCommand
    {
        public override void Execute()
        {
            var model = GetModel<CounterModel>();
            var system = GetSystem<CounterSystem>();

            if (model != null && system != null)
            {
                system.Increment();
                ALog.Info("[IncrementCounterCommand] Executed");
            }
            else
            {
                ALog.Error("[IncrementCounterCommand] Failed to get model or system");
            }
        }
    }
}
