using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Graphs
{
    [Serializable]
    public abstract class AsakiAsyncNodeBase : AsakiNodeBase
    {
        public abstract Task<NodeExecutionResult> ExecuteAsync(
            AsakiGraphRuntimeContext context,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        public virtual void OnCancelled()
        {
            Debug.Log($"[{GetType().Name}] Execution cancelled");
        }
    }

    public struct NodeExecutionResult
    {
        public bool Success;
        public string ErrorMessage;
        public string OutputPortName;

        public static NodeExecutionResult Succeed(string outputPort = "Out")
        {
            return new NodeExecutionResult { Success = true, OutputPortName = outputPort };
        }

        public static NodeExecutionResult Fail(string error)
        {
            return new NodeExecutionResult
            {
                Success = false,
                ErrorMessage = error,
                OutputPortName = "Error",
            };
        }
    }
}
