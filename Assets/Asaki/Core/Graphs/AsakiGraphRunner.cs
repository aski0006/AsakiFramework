using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Graphs
{
    public abstract class AsakiGraphRunner<TGraph> : MonoBehaviour
        where TGraph : AsakiGraphAsset
    {
        [Header("Graphs Data")]
        public TGraph GraphAsset;

        [Header("Runtime Settings")]
        [Tooltip("If true, creates a runtime copy of the graph asset")]
        public bool UseInstancedGraph = true;

        private TGraph _runtimeGraph;
        protected AsakiGraphRuntimeContext _context;

        [NonSerialized]
        private Dictionary<string, AsakiBlackboardKey> _keyHashCache;

        [NonSerialized]
        private Dictionary<Type, Action<AsakiBlackboardKey, object>> _setterCache;

        protected virtual void Start()
        {
            if (GraphAsset == null)
            {
                ALog.Warn($"[AsakiGraphRunner] GraphAsset is null on {name}");
                return;
            }

            _keyHashCache = new Dictionary<string, AsakiBlackboardKey>(32);
            _setterCache = new Dictionary<Type, Action<AsakiBlackboardKey, object>>(16);

            if (UseInstancedGraph)
            {
                _runtimeGraph = GraphAsset.Clone<TGraph>();
                if (_runtimeGraph == null)
                {
                    ALog.Error($"[AsakiGraphRunner] Failed to clone graph on {name}");
                    return;
                }
            }
            else
            {
                _runtimeGraph = GraphAsset;
            }

            _runtimeGraph.InitializeRuntime();
            InitializeContext();
            InitializeBlackboardValues();
            OnGraphInitialized();
        }

        protected virtual void OnDestroy()
        {
            StopToken();

            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            if (UseInstancedGraph && _runtimeGraph != null)
            {
                Destroy(_runtimeGraph);
                _runtimeGraph = null;
            }

            _keyHashCache?.Clear();
            _keyHashCache = null;

            _setterCache?.Clear();
            _setterCache = null;

            OnNodeExecuted = null;
        }

        private void InitializeContext()
        {
            _context = new AsakiGraphRuntimeContext();
            _context.Owner = gameObject;

            IAsakiBlackboard globalScope = null;
            if (AsakiContext.TryGet(out IAsakiBlackboard globalBB))
            {
                globalScope = globalBB;
            }

            _context.Blackboard = new AsakiBlackboard(globalScope);
        }

        private void InitializeBlackboardValues()
        {
            if (_runtimeGraph == null || _context.Blackboard == null)
                return;

            using (_context.Blackboard.BeginBatch())
            {
                string globalAssetPath = "GlobalBlackboard";
                AsakiGlobalBlackboardAsset globalAsset =
                    UnityEngine.Resources.Load<AsakiGlobalBlackboardAsset>(globalAssetPath);

                if (globalAsset != null)
                {
                    foreach (AsakiVariableDef globalVar in globalAsset.GlobalVariables)
                    {
                        if (!_runtimeGraph.Variables.Exists(v => v.Name == globalVar.Name))
                        {
                            WriteVariableToRuntime(globalVar.Name, globalVar);
                        }
                    }
                }

                foreach (AsakiVariableDef variable in _runtimeGraph.Variables)
                {
                    WriteVariableToRuntime(variable.Name, variable);
                }
            }
        }

        public void BatchUpdateVariables(Action<IAsakiBlackboard> updates)
        {
            if (_context?.Blackboard == null)
                return;

            using (_context.Blackboard.BeginBatch())
            {
                updates(_context.Blackboard);
            }
        }

        private void WriteVariableToRuntime(string name, AsakiVariableDef variable)
        {
            if (variable.ValueData != null)
            {
                variable.ValueData.ApplyTo(_context.Blackboard, name);
            }
            else
            {
                ALog.Warn($"[AsakiRunner] Variable '{name}' has no data!");
            }
        }

        protected virtual void OnGraphInitialized() { }

        private AsakiBlackboardKey GetOrCreateHashKey(string key)
        {
            if (_keyHashCache.TryGetValue(key, out AsakiBlackboardKey hashKey))
            {
                return hashKey;
            }

            hashKey = new AsakiBlackboardKey(key);
            _keyHashCache[key] = hashKey;
            return hashKey;
        }

        public T GetVariable<T>(string key)
        {
            if (_context?.Blackboard == null)
                return default(T);

            AsakiBlackboardKey hashKey = GetOrCreateHashKey(key);
            return _context.Blackboard.GetValue<T>(hashKey);
        }

        public void SetVariable<T>(string key, T value)
        {
            if (_context?.Blackboard == null)
                return;

            AsakiVariableDef varDef = _runtimeGraph?.Variables.Find(v => v.Name == key);
            if (varDef != null && !varDef.Validate(value))
            {
                ALog.Warn(
                    $"[AsakiRunner] Variable '{key}' validation failed: {varDef.Constraint?.GetErrorMessage(value)}"
                );
                return;
            }

            AsakiBlackboardKey hashKey = GetOrCreateHashKey(key);
            _context.Blackboard.SetValue<T>(hashKey, value);
        }

        public void ResetAllVariablesToDefault()
        {
            if (_runtimeGraph == null || _context?.Blackboard == null)
                return;

            using (_context.Blackboard.BeginBatch())
            {
                foreach (AsakiVariableDef varDef in _runtimeGraph.Variables)
                {
                    varDef.ResetToDefault();
                    if (varDef.ValueData != null)
                    {
                        varDef.ValueData.ApplyTo(_context.Blackboard, varDef.Name);
                    }
                }
            }
        }

        public void ResetVariable(string key)
        {
            if (_runtimeGraph == null)
                return;

            AsakiVariableDef varDef = _runtimeGraph.Variables.Find(v => v.Name == key);
            if (varDef != null)
            {
                varDef.ResetToDefault();
                if (varDef.ValueData != null)
                {
                    varDef.ValueData.ApplyTo(_context.Blackboard, key);
                }
            }
        }

        protected T GetInputValue<T>(
            AsakiNodeBase currentNode,
            string inputPortName,
            T fallback = default(T)
        )
        {
            AsakiEdgeData edge = _runtimeGraph.GetInputConnection(currentNode, inputPortName);
            if (edge == null)
                return fallback;

            AsakiNodeBase sourceNode = _runtimeGraph.GetNodeByGUID(edge.BaseNodeGUID);
            if (sourceNode == null)
                return fallback;

            return ResolveNodeValue<T>(sourceNode, edge.BasePortName);
        }

        protected virtual T ResolveNodeValue<T>(AsakiNodeBase node, string outputPortName)
        {
            if (node is AsakiGetVariableNode getVarNode)
            {
                if (_context.Blackboard == null)
                    return default(T);

                AsakiBlackboardKey hashKey = GetOrCreateHashKey(getVarNode.VariableName);
                T value = _context.Blackboard.GetValue<T>(hashKey);

                return value;
            }

            ALog.Warn($"[AsakiRunner] Cannot resolve value from node type: {node.GetType().Name}");
            return default(T);
        }

        public event Action<AsakiNodeBase> OnNodeExecuted;

        protected virtual void ExecuteNode(AsakiNodeBase node)
        {
            if (node == null)
                return;

#if UNITY_EDITOR
            OnNodeExecuted?.Invoke(node);
#endif

            if (node is AsakiSetVariableNode setVarNode)
            {
                if (_context.Blackboard == null)
                    return;

                object valueToWrite = GetInputValue<object>(setVarNode, "Value");
                WriteToBlackboard(
                    setVarNode.VariableName,
                    valueToWrite,
                    setVarNode.VariableTypeName
                );

                AsakiNodeBase nextNode = _runtimeGraph.GetNextNode(node, "Out");
                if (nextNode != null)
                    ExecuteNode(nextNode);
                return;
            }

            OnExecuteCustomNode(node);
        }

        private void WriteToBlackboard(string key, object value, string typeName)
        {
            if (value == null)
            {
                ALog.Warn($"[AsakiRunner] Trying to set null value for key '{key}'. Ignore.");
                return;
            }

            AsakiBlackboardKey hashKey = GetOrCreateHashKey(key);

            switch (value)
            {
                case int i:
                    _context.Blackboard.SetValue(hashKey, i);
                    break;
                case float f:
                    _context.Blackboard.SetValue(hashKey, f);
                    break;
                case bool b:
                    _context.Blackboard.SetValue(hashKey, b);
                    break;
                case string s:
                    _context.Blackboard.SetValue(hashKey, s);
                    break;
                case Vector3 v3:
                    _context.Blackboard.SetValue(hashKey, v3);
                    break;
                case Vector2 v2:
                    _context.Blackboard.SetValue(hashKey, v2);
                    break;
                case Vector3Int v3i:
                    _context.Blackboard.SetValue(hashKey, v3i);
                    break;
                case Vector2Int v2i:
                    _context.Blackboard.SetValue(hashKey, v2i);
                    break;
                case Color c:
                    _context.Blackboard.SetValue(hashKey, c);
                    break;
                default:
                    if (
                        !string.IsNullOrEmpty(typeName)
                        && AsakiTypeBridge.TrySetValue(
                            _context.Blackboard,
                            hashKey,
                            typeName,
                            value
                        )
                    )
                    {
                        return;
                    }
                    try
                    {
                        WriteGenericToBlackboard(hashKey, value);
                    }
                    catch (Exception e)
                    {
                        ALog.Error($"[AsakiRunner] SetVariable Failed:  {e}");
                    }
                    break;
            }
        }

        private void WriteGenericToBlackboard(AsakiBlackboardKey key, object value)
        {
            Type valueType = value.GetType();

            if (!_setterCache.TryGetValue(valueType, out var setter))
            {
                MethodInfo setValueMethod = typeof(IAsakiBlackboard).GetMethod("SetValue");

                if (setValueMethod != null)
                {
                    MethodInfo genericMethod = setValueMethod.MakeGenericMethod(valueType);
                    setter = (k, v) =>
                        genericMethod.Invoke(_context.Blackboard, new object[] { k, v });
                    _setterCache[valueType] = setter;
                }
                else
                {
                    throw new MissingMethodException(
                        "[AsakiRunner] Could not find SetValue<T> on Blackboard instance."
                    );
                }
            }

            setter(key, value);
        }

        protected virtual void OnExecuteCustomNode(AsakiNodeBase node) { }

        public TGraph GetRuntimeGraph()
        {
            return _runtimeGraph;
        }

        #region Async

        private CancellationTokenSource _executionCts;

        protected async Task ExecuteNodeAsync(
            AsakiNodeBase node,
            CancellationToken ct = default(CancellationToken)
        )
        {
            if (node == null)
                return;

            AsakiNodeExecutionContext execContext = AsakiNodeExecutionPool.Rent();
            execContext.Node = node;
            execContext.GraphContext = _context;
            execContext.StartTime = UnityEngine.Time.time;

            try
            {
#if UNITY_EDITOR
                OnNodeExecuted?.Invoke(node);
#endif

                if (node is AsakiSetVariableNode setVarNode)
                {
                    object valueToWrite = GetInputValue<object>(setVarNode, "Value");
                    WriteToBlackboard(
                        setVarNode.VariableName,
                        valueToWrite,
                        setVarNode.VariableTypeName
                    );

                    AsakiNodeBase nextNode = _runtimeGraph.GetNextNode(node, "Out");
                    if (nextNode != null)
                    {
                        await ExecuteNodeAsync(nextNode, ct);
                    }
                    return;
                }

                if (node is AsakiAsyncNodeBase asyncNode)
                {
                    NodeExecutionResult result;
                    try
                    {
                        result = await asyncNode.ExecuteAsync(_context, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        asyncNode.OnCancelled();
                        return;
                    }
                    catch (Exception e)
                    {
                        ALog.Error($"[AsakiRunner] Async node failed: {e}");
                        result = NodeExecutionResult.Fail(e.Message);
                    }

                    string portName = result.Success ? result.OutputPortName : "Error";
                    AsakiNodeBase nextNode = _runtimeGraph.GetNextNode(node, portName);

                    if (nextNode != null)
                    {
                        await ExecuteNodeAsync(nextNode, ct);
                    }
                    return;
                }

                OnExecuteCustomNode(node);
            }
            catch (Exception e)
            {
                ALog.Error($"[AsakiRunner] Failed to execute node: {e}", e);
                throw;
            }
            finally
            {
                AsakiNodeExecutionPool.Return(execContext);
            }
        }

        public async Task StartGraphAsync()
        {
            _executionCts = new CancellationTokenSource();

            AsakiNodeBase entryNode = _runtimeGraph.GetEntryNode<AsakiNodeBase>();
            if (entryNode != null)
            {
                await ExecuteNodeAsync(entryNode, _executionCts.Token);
            }
        }

        public void StopToken()
        {
            _executionCts?.Cancel();
            _executionCts?.Dispose();
            _executionCts = null;
        }

        #endregion
    }
}
