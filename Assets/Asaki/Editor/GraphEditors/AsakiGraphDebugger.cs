using System;
using System.Reflection;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.GraphEditors
{
    /// <summary>
    /// 负责连接 Editor Window 和 Runtime Runner
    /// </summary>
    public class AsakiGraphDebugger
    {
        private readonly AsakiGraphView _graphView;

        // 当前连接的 Runner 实例
        private MonoBehaviour _currentRunner;
        private EventInfo _eventInfo;
        private Delegate _handler;

        public AsakiGraphDebugger(AsakiGraphView graphView)
        {
            _graphView = graphView;

            // 监听编辑器选区变化
            Selection.selectionChanged += OnSelectionChanged;

            // 尝试初始化
            OnSelectionChanged();
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Disconnect();
        }

        private void OnSelectionChanged()
        {
            // 1. 如果没在运行，不需要连接
            if (!Application.isPlaying)
            {
                Disconnect();
                return;
            }

            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;

            // 2. 查找 GameObject 上是否有对应的 Runner
            // 注意：我们不知道具体的 Runner 类型 (AsakiGraphRunner<T>)，所以需要反射查找
            // 或者我们可以让所有 Runner 实现一个非泛型接口 IAsakiGraphRunner

            var runners = go.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour runner in runners)
            {
                // 简单的鸭子类型匹配：看它有没有 "GraphAsset" 字段且资产匹配
                Type runnerType = runner.GetType();
                FieldInfo assetField = runnerType.GetField("GraphAsset");

                if (assetField != null)
                {
                    AsakiGraphAsset asset = assetField.GetValue(runner) as AsakiGraphAsset;

                    // 3. 只有当选中的 Runner 运行的是当前编辑器打开的 Graph 时，才连接
                    // (对比 Asset 引用是否一致，或者 GUID 是否一致)
                    if (asset == _graphView.GraphAsset) // 需要在 GraphView 公开 GraphAsset
                    {
                        Connect(runner);
                        return;
                    }
                }
            }
        }

        private void Connect(MonoBehaviour runner)
        {
            if (_currentRunner == runner)
                return;

            Disconnect();
            _currentRunner = runner;

            // 4. 反射订阅 OnNodeExecuted 事件
            // 因为 Runner 是泛型的，我们在 Editor 层不知道具体的 T
            EventInfo eventInfo = runner.GetType().GetEvent("OnNodeExecuted");
            if (eventInfo != null)
            {
                // 创建委托： (AsakiNodeBase node) => OnNodeExecuted(node)
                Action<AsakiNodeBase> action = OnNodeExecuted;
                _handler = Delegate.CreateDelegate(
                    eventInfo.EventHandlerType,
                    action.Target,
                    action.Method
                );
                eventInfo.AddEventHandler(runner, _handler);

                _eventInfo = eventInfo;
                Debug.Log($"[AsakiDebugger] Connected to {runner.name}");
            }
        }

        private void Disconnect()
        {
            if (_currentRunner && _handler != null && _eventInfo != null)
            {
                _eventInfo.RemoveEventHandler(_currentRunner, _handler);
            }
            _currentRunner = null;
            _handler = null;
            _eventInfo = null;
        }

        // 5. 收到运行时事件 -> 更新 UI
        private void OnNodeExecuted(AsakiNodeBase nodeData)
        {
            // 必须在主线程更新 UI (Unity 事件本来就是主线程，安全)
            AsakiNodeView nodeView = _graphView.GetNodeByGUID(nodeData.GUID);
            if (nodeView != null)
            {
                nodeView.Highlight();
            }
        }
    }
}
