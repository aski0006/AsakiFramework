using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Architecture.Extensions;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.UI;
using Game.Scripts.Examples.Architecture.Counter.Commands;
using Game.Scripts.Examples.Architecture.Counter.Events;
using Game.Scripts.Examples.Architecture.Counter.Queries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Examples.Architecture.Counter
{
    /// <summary>
    /// Counter 视图 - 使用最新架构
    /// - Command/Query 模式解耦业务逻辑
    /// - 对象池管理 Command，避免 GC
    /// - 支持 Undo/Redo
    /// </summary>
    public class CounterView
        : MonoBehaviour,
            IAsakiAutoInject,
            IAsakiInit<CounterArchitecture>,
            IAsakiHandler<AchievementUnlockedEvent>
    {
        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.Button)]
        private Button btnAdd;

        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.Button)]
        private Button btnReset;

        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.Button)]
        private Button btnUndo;

        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.Button)]
        private Button btnRedo;

        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.TextMeshPro)]
        private TMP_Text txtCount;

        [SerializeField]
        [AsakiUIBuilder(AsakiUIWidgetType.TextMeshPro)]
        private TMP_Text txtStatus;

        private CounterArchitecture _architecture;
        private CounterModel _model;

        // Undo/Redo 栈
        private readonly AsakiUndoRedoStack _undoRedoStack = new();

        [AsakiInject]
        public void Init(CounterArchitecture architecture)
        {
            _architecture = architecture;
            _model = architecture.GetModel<CounterModel>();

            BindEvents();
            UpdateView(_model.count.Value);
            UpdateUndoRedoButtons();

            ALog.Info("[CounterView] Initialized with new architecture!");
        }

        private void OnEnable()
        {
            this.AsakiRegister();
        }

        private void OnDisable()
        {
            this.AsakiUnregister();
        }

        private void BindEvents()
        {
            // 使用 Command 模式处理用户输入
            btnAdd?.onClick.AddListener(OnAddClicked);
            btnReset?.onClick.AddListener(OnResetClicked);
            btnUndo?.onClick.AddListener(OnUndoClicked);
            btnRedo?.onClick.AddListener(OnRedoClicked);

            // 响应式绑定 Model 变化
            _model.count.Subscribe(OnCountChanged);
        }

        /// <summary>
        /// 使用池化 Command 执行增加操作
        /// </summary>
        private void OnAddClicked()
        {
            // 使用可撤销的 Command（从 AsakiArchitecturePoolManager 租借）
            var cmd = AsakiArchitecturePoolManager.Rent<UndoableIncrementCommand>();
            try
            {
                cmd.Create(_architecture);
                cmd.Execute();
                _undoRedoStack.RecordCommand(cmd);
            }
            finally
            {
                // 记录后不归还，Undo/Redo 栈会管理它
                // 注意：这里 cmd 被栈引用，不需要 Return
            }

            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// 使用池化 Command 执行重置操作
        /// </summary>
        private void OnResetClicked()
        {
            // 使用 ExecutePooledCommand 便捷方法（自动租借和归还）
            _architecture.ExecutePooledCommand<ResetCounterCommand>();

            // 重置会清空历史
            _undoRedoStack.ClearHistory();
            UpdateUndoRedoButtons();
        }

        private void OnUndoClicked()
        {
            if (_undoRedoStack.CanUndo)
            {
                _undoRedoStack.Undo();
                UpdateUndoRedoButtons();
            }
        }

        private void OnRedoClicked()
        {
            if (_undoRedoStack.CanRedo)
            {
                _undoRedoStack.Redo();
                UpdateUndoRedoButtons();
            }
        }

        private void OnCountChanged(int count)
        {
            UpdateView(count);
        }

        private void UpdateView(int count)
        {
            if (txtCount)
                txtCount.text = $"Count: {count}";
        }

        private void UpdateUndoRedoButtons()
        {
            if (btnUndo != null)
                btnUndo.interactable = _undoRedoStack.CanUndo;

            if (btnRedo != null)
                btnRedo.interactable = _undoRedoStack.CanRedo;

            if (txtStatus != null)
                txtStatus.text =
                    $"History: {_undoRedoStack.UndoCount} | {_undoRedoStack.RedoCount}";
        }

        public void OnEvent(AchievementUnlockedEvent e)
        {
            Debug.Log(
                $"<color=yellow>[CounterView] Achievement Unlocked: {e.AchievementName}</color>"
            );
            if (txtCount != null)
            {
                txtCount.text += $"\n🏆 {e.AchievementName}!";
            }
        }

        private void OnDestroy()
        {
            btnAdd?.onClick.RemoveAllListeners();
            btnReset?.onClick.RemoveAllListeners();
            btnUndo?.onClick.RemoveAllListeners();
            btnRedo?.onClick.RemoveAllListeners();

            // 清理 Undo/Redo 历史
            _undoRedoStack.ClearHistory();
        }

        /// <summary>
        /// 示例：使用 Query 获取当前值
        /// </summary>
        public void ExampleQueryUsage()
        {
            // 使用池化 Query
            int currentValue = _architecture.QueryPooled<GetCounterValueQuery, int>();
            ALog.Info($"[CounterView] Queried value: {currentValue}");
        }
    }
}
