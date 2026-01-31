using Asaki.Core.Architecture.Command;
using Asaki.Core.Logging;

namespace Game.Scripts.Examples.Architecture.Counter.Commands
{
    /// <summary>
    /// 重置计数器命令
    /// 不可撤销的命令示例
    /// </summary>
    public class ResetCounterCommand : AsakiCommand
    {
        public override void Execute()
        {
            var model = GetModel<CounterModel>();

            if (model != null)
            {
                int oldValue = model.count.Value;
                model.count.Value = 0;
                ALog.Info($"[ResetCounterCommand] Counter reset from {oldValue} to 0");
            }
            else
            {
                ALog.Error("[ResetCounterCommand] Failed to get model");
            }
        }
    }
}
