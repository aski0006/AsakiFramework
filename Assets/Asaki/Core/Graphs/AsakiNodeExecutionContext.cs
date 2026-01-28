using System;
using System.Collections.Generic;

namespace Asaki.Core.Graphs
{
    public class AsakiNodeExecutionContext
    {
        public AsakiNodeBase Node;
        public AsakiGraphRuntimeContext GraphContext;
        public Dictionary<string, object> InputCache;
        public float StartTime;

        public void Reset()
        {
            Node = null;
            GraphContext = null;
            InputCache?.Clear();
            StartTime = 0f;
        }
    }

    public static class AsakiNodeExecutionPool
    {
        private static readonly Stack<AsakiNodeExecutionContext> _pool =
            new Stack<AsakiNodeExecutionContext>(32);
        private static readonly object _lock = new object();

        public static AsakiNodeExecutionContext Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }

            return new AsakiNodeExecutionContext { InputCache = new Dictionary<string, object>(4) };
        }

        public static void Return(AsakiNodeExecutionContext context)
        {
            if (context == null)
                return;

            context.Reset();

            lock (_lock)
            {
                if (_pool.Count < 128)
                {
                    _pool.Push(context);
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }
    }
}
