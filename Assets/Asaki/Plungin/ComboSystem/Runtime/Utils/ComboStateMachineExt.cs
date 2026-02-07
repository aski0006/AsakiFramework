using System;
using Asaki.Core.FSM;

namespace Asaki.Plungin.ComboSystem.States
{
    /// <summary>
    /// AsakiStateMachine扩展方法
    /// </summary>
    public static class ComboStateMachineExt
    {
        /// <summary>
        /// 获取状态实例（用于设置数据）
        /// </summary>
        public static TState GetState<TState>(this AsakiStateMachine<AsakiComboController> machine)
            where TState : AsakiState<AsakiComboController>, new()
        {
            // 使用反射获取私有字段 _stateCache
            var cacheField = typeof(AsakiStateMachine<AsakiComboController>).GetField(
                "_stateCache",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            if (cacheField == null)
            {
                // 如果无法获取缓存，创建新实例
                var newState = new TState();
                newState.Initialize(machine, machine.Context);
                return newState;
            }

            var cache = cacheField.GetValue(machine) as System.Collections.Generic.Dictionary<Type, AsakiState<AsakiComboController>>;
            var type = typeof(TState);

            if (!cache.TryGetValue(type, out var state))
            {
                state = new TState();
                state.Initialize(machine, machine.Context);
                cache.Add(type, state);
            }

            return state as TState;
        }

        /// <summary>
        /// 获取当前状态类型
        /// </summary>
        public static ComboStateType GetCurrentStateType(this AsakiStateMachine<AsakiComboController> machine)
        {
            var currentState = machine.CurrentState;
            if (currentState is ComboIdleState) return ComboStateType.Idle;
            if (currentState is ComboStartupState) return ComboStateType.Startup;
            if (currentState is ComboActiveState) return ComboStateType.Active;
            if (currentState is ComboRecoveryState) return ComboStateType.Recovery;
            if (currentState is ComboWindowState) return ComboStateType.ComboWindow;
            if (currentState is ComboInterruptedState) return ComboStateType.Interrupted;
            return ComboStateType.Idle;
        }
    }
}
