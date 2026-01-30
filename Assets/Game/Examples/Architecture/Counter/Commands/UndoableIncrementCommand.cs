using Asaki.Core.Architecture.Command;
using Asaki.Core.Logging;

namespace Game.Examples.Architecture.Counter.Commands
{
    /// <summary>
    /// 可撤销的增加计数器命令
    /// 演示 Undo/Redo 功能
    /// </summary>
    public class UndoableIncrementCommand : AsakiUndoCommand
    {
        private int _previousValue;

        public override void Execute()
        {
            var model = GetModel<CounterModel>();

            if (model != null)
            {
                _previousValue = model.count.Value;
                model.count.Value++;
                ALog.Info(
                    $"[UndoableIncrementCommand] Executed: {_previousValue} -> {model.count.Value}"
                );
            }
        }

        public override void Undo()
        {
            var model = GetModel<CounterModel>();

            if (model != null)
            {
                model.count.Value = _previousValue;
                ALog.Info(
                    $"[UndoableIncrementCommand] Undone: {model.count.Value} -> {_previousValue}"
                );
            }
        }
    }
}
