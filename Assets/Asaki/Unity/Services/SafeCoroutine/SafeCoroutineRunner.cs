// ============================================================================
// SafeCoroutine - 安全的Unity协程系统
// ============================================================================
// 功能特性：
// 1. 异常捕获与回调处理 - 解决标准协程无法有效捕获异常的问题
// 2. 非MonoBehaviour类支持 - 突破原生协程必须依附MonoBehaviour的限制
// 3. 协程生命周期管理 - 支持启动、停止、暂停、恢复等操作
// 4. 全局协程调度器 - 通过AsakiContext管理，由Bootstrapper启动
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Unity.Services.SafeCoroutine
{
    /// <summary>
    /// 协程执行状态
    /// </summary>
    public enum SafeCoroutineState
    {
        Pending, // 等待执行
        Running, // 运行中
        Paused, // 已暂停
        Completed, // 正常完成
        Failed, // 执行失败（发生异常）
        Cancelled, // 被取消
    }

    /// <summary>
    /// 协程执行结果
    /// </summary>
    public readonly struct SafeCoroutineResult
    {
        public readonly bool Success;
        public readonly Exception Exception;
        public readonly object Result;
        public readonly SafeCoroutineState State;

        public SafeCoroutineResult(
            bool success,
            Exception exception,
            object result,
            SafeCoroutineState state
        )
        {
            Success = success;
            Exception = exception;
            Result = result;
            State = state;
        }

        public static SafeCoroutineResult Ok(
            object result = null,
            SafeCoroutineState state = SafeCoroutineState.Completed
        )
        {
            return new SafeCoroutineResult(true, null, result, state);
        }

        public static SafeCoroutineResult Error(Exception ex)
        {
            return new SafeCoroutineResult(false, ex, null, SafeCoroutineState.Failed);
        }

        public static SafeCoroutineResult Cancelled()
        {
            return new SafeCoroutineResult(false, null, null, SafeCoroutineState.Cancelled);
        }
    }

    /// <summary>
    /// 协程异常回调委托
    /// </summary>
    public delegate void SafeCoroutineExceptionHandler(string coroutineId, Exception exception);

    /// <summary>
    /// 协程完成回调委托
    /// </summary>
    public delegate void SafeCoroutineCompletedHandler(
        string coroutineId,
        SafeCoroutineResult result
    );

    /// <summary>
    /// 安全协程句柄 - 用于控制和管理协程实例
    /// </summary>
    public sealed class SafeCoroutineHandle
    {
        private readonly string _id;
        private readonly IEnumerator _enumerator;
        private readonly SafeCoroutineExceptionHandler _exceptionHandler;
        private readonly SafeCoroutineCompletedHandler _completedHandler;
        private SafeCoroutineState _state;
        private object _currentYield;
        private bool _shouldStop;
        private bool _isPaused;

        public string Id => _id;
        public SafeCoroutineState State => _state;
        public bool IsRunning => _state == SafeCoroutineState.Running;
        public bool IsCompleted =>
            _state == SafeCoroutineState.Completed
            || _state == SafeCoroutineState.Failed
            || _state == SafeCoroutineState.Cancelled;

        internal SafeCoroutineHandle(
            string id,
            IEnumerator enumerator,
            SafeCoroutineExceptionHandler exceptionHandler = null,
            SafeCoroutineCompletedHandler completedHandler = null
        )
        {
            _id = id;
            _enumerator = enumerator;
            _exceptionHandler = exceptionHandler;
            _completedHandler = completedHandler;
            _state = SafeCoroutineState.Pending;
            _shouldStop = false;
            _isPaused = false;
        }

        /// <summary>
        /// 暂停协程执行
        /// </summary>
        public void Pause()
        {
            if (_state == SafeCoroutineState.Running)
            {
                _isPaused = true;
                _state = SafeCoroutineState.Paused;
            }
        }

        /// <summary>
        /// 恢复协程执行
        /// </summary>
        public void Resume()
        {
            if (_state == SafeCoroutineState.Paused)
            {
                _isPaused = false;
                _state = SafeCoroutineState.Running;
            }
        }

        /// <summary>
        /// 停止协程执行
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

        /// <summary>
        /// 内部执行方法 - 由SafeCoroutineRunner调用
        /// </summary>
        internal SafeCoroutineResult ExecuteStep()
        {
            if (_shouldStop)
            {
                _state = SafeCoroutineState.Cancelled;
                return SafeCoroutineResult.Cancelled();
            }

            if (_isPaused)
            {
                return SafeCoroutineResult.Ok(_currentYield, SafeCoroutineState.Paused);
            }

            try
            {
                if (_enumerator.MoveNext())
                {
                    _currentYield = _enumerator.Current;
                    _state = SafeCoroutineState.Running;
                    return SafeCoroutineResult.Ok(_currentYield, SafeCoroutineState.Running);
                }
                else
                {
                    _state = SafeCoroutineState.Completed;
                    var result = SafeCoroutineResult.Ok(_currentYield);

                    try
                    {
                        _completedHandler?.Invoke(_id, result);
                    }
                    catch (Exception handlerEx)
                    {
                        ALog.Error($"[SafeCoroutine] 完成处理器抛出异常: {handlerEx}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _state = SafeCoroutineState.Failed;
                var result = SafeCoroutineResult.Error(ex);

                try
                {
                    _exceptionHandler?.Invoke(_id, ex);
                }
                catch (Exception handlerEx)
                {
                    ALog.Error($"[SafeCoroutine] 异常处理器抛出异常: {handlerEx}");
                }

                try
                {
                    _completedHandler?.Invoke(_id, result);
                }
                catch (Exception handlerEx)
                {
                    ALog.Error($"[SafeCoroutine] 完成处理器抛出异常: {handlerEx}");
                }

                return result;
            }
        }

        internal void SetRunning()
        {
            _state = SafeCoroutineState.Running;
        }
    }

    /// <summary>
    /// 全局协程运行器 - 通过AsakiContext管理生命周期
    /// 由Bootstrapper在启动时创建并注册到AsakiContext
    /// </summary>
    public sealed class SafeCoroutineRunner : MonoBehaviour, IAsakiGlobalService
    {
        private readonly Dictionary<string, SafeCoroutineHandle> _coroutines =
            new Dictionary<string, SafeCoroutineHandle>();
        private readonly Queue<SafeCoroutineHandle> _pendingCoroutines =
            new Queue<SafeCoroutineHandle>();
        private readonly List<string> _completedCoroutines = new List<string>();
        private SafeCoroutineExceptionHandler _globalExceptionHandler;
        private int _coroutineIdCounter = 0;
#pragma warning disable CS0414
        private bool _isQuitting = false;
#pragma warning restore CS0414

        /// <summary>
        /// 获取实例 - 从AsakiContext获取（由Bootstrapper注册）
        /// </summary>
        public static SafeCoroutineRunner Instance
        {
            get
            {
                if (AsakiContext.TryGet(out SafeCoroutineRunner runner))
                {
                    return runner;
                }
                ALog.Error(
                    "[SafeCoroutine] SafeCoroutineRunner not registered in AsakiContext. Ensure Bootstrapper is running."
                );
                return null;
            }
        }

        /// <summary>
        /// Bootstrapper初始化时调用
        /// </summary>
        public void OnBootstrapInit()
        {
            ALog.Info("[SafeCoroutine] Runner initialized.");
        }

        /// <summary>
        /// 设置全局异常处理器
        /// </summary>
        public void SetGlobalExceptionHandler(SafeCoroutineExceptionHandler handler)
        {
            _globalExceptionHandler = handler;
        }

        /// <summary>
        /// 启动一个新协程
        /// </summary>
        /// <param name="enumerator">协程迭代器</param>
        /// <param name="exceptionHandler">异常回调（可选）</param>
        /// <param name="completedHandler">完成回调（可选）</param>
        /// <returns>协程句柄</returns>
        public SafeCoroutineHandle StartSafeCoroutine(
            IEnumerator enumerator,
            SafeCoroutineExceptionHandler exceptionHandler = null,
            SafeCoroutineCompletedHandler completedHandler = null
        )
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator));

            string id = GenerateCoroutineId();
            var handle = new SafeCoroutineHandle(
                id,
                enumerator,
                exceptionHandler,
                completedHandler
            );

            lock (_coroutines)
            {
                _pendingCoroutines.Enqueue(handle);
            }

            return handle;
        }

        /// <summary>
        /// 通过委托启动协程（简化调用方式）
        /// </summary>
        public SafeCoroutineHandle StartSafeCoroutine(
            Func<IEnumerator> coroutineFunc,
            SafeCoroutineExceptionHandler exceptionHandler = null,
            SafeCoroutineCompletedHandler completedHandler = null
        )
        {
            return StartSafeCoroutine(coroutineFunc(), exceptionHandler, completedHandler);
        }

        /// <summary>
        /// 停止指定协程
        /// </summary>
        public void StopSafeCoroutine(SafeCoroutineHandle handle)
        {
            if (handle != null)
            {
                handle.Stop();
            }
        }

        /// <summary>
        /// 停止指定ID的协程
        /// </summary>
        public void StopSafeCoroutine(string coroutineId)
        {
            lock (_coroutines)
            {
                if (_coroutines.TryGetValue(coroutineId, out var handle))
                {
                    handle.Stop();
                }
            }
        }

        /// <summary>
        /// 停止所有协程
        /// </summary>
        public void StopAllSafeCoroutines()
        {
            lock (_coroutines)
            {
                foreach (var handle in _coroutines.Values)
                {
                    handle.Stop();
                }
                _pendingCoroutines.Clear();
            }
        }

        /// <summary>
        /// 获取协程句柄
        /// </summary>
        public SafeCoroutineHandle GetCoroutine(string coroutineId)
        {
            lock (_coroutines)
            {
                _coroutines.TryGetValue(coroutineId, out var handle);
                return handle;
            }
        }

        /// <summary>
        /// 获取所有运行中的协程
        /// </summary>
        public List<SafeCoroutineHandle> GetRunningCoroutines()
        {
            lock (_coroutines)
            {
                var result = new List<SafeCoroutineHandle>();
                foreach (var handle in _coroutines.Values)
                {
                    if (handle.IsRunning)
                        result.Add(handle);
                }
                return result;
            }
        }

        private string GenerateCoroutineId()
        {
            return $"SafeCoroutine_{System.Threading.Interlocked.Increment(ref _coroutineIdCounter)}";
        }

        private void Update()
        {
            // 处理待启动的协程
            lock (_coroutines)
            {
                while (_pendingCoroutines.Count > 0)
                {
                    var handle = _pendingCoroutines.Dequeue();
                    handle.SetRunning();
                    _coroutines[handle.Id] = handle;
                }
            }

            // 执行所有活跃协程
            _completedCoroutines.Clear();

            lock (_coroutines)
            {
                foreach (var kvp in _coroutines)
                {
                    var handle = kvp.Value;
                    if (handle.IsCompleted)
                    {
                        _completedCoroutines.Add(kvp.Key);
                        continue;
                    }

                    var result = handle.ExecuteStep();

                    if (!result.Success && result.State == SafeCoroutineState.Failed)
                    {
                        // 异常已被ExecuteStep内部处理，这里记录日志
                        ALog.Error(
                            $"[SafeCoroutine] 协程 {handle.Id} 执行异常: {result.Exception}"
                        );
                        _completedCoroutines.Add(kvp.Key);
                    }
                    else if (result.State == SafeCoroutineState.Completed)
                    {
                        _completedCoroutines.Add(kvp.Key);
                    }
                    else if (result.State == SafeCoroutineState.Cancelled)
                    {
                        _completedCoroutines.Add(kvp.Key);
                    }
                    // Running 或 Paused 状态的协程继续保留
                }

                // 清理已完成的协程
                foreach (var id in _completedCoroutines)
                {
                    _coroutines.Remove(id);
                }
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            StopAllSafeCoroutines();
        }
    }

    /// <summary>
    /// 静态扩展类 - 提供便捷的协程启动方法
    /// </summary>
    public static class SafeCoroutineExtensions
    {
        /// <summary>
        /// 在任意类中启动安全协程
        /// </summary>
        public static SafeCoroutineHandle StartSafeCoroutine(
            this object _,
            IEnumerator enumerator,
            SafeCoroutineExceptionHandler exceptionHandler = null,
            SafeCoroutineCompletedHandler completedHandler = null
        )
        {
            return SafeCoroutineRunner.Instance?.StartSafeCoroutine(
                enumerator,
                exceptionHandler,
                completedHandler
            );
        }

        /// <summary>
        /// 在任意类中启动安全协程（委托方式）
        /// </summary>
        public static SafeCoroutineHandle StartSafeCoroutine(
            this object _,
            Func<IEnumerator> coroutineFunc,
            SafeCoroutineExceptionHandler exceptionHandler = null,
            SafeCoroutineCompletedHandler completedHandler = null
        )
        {
            return SafeCoroutineRunner.Instance?.StartSafeCoroutine(
                coroutineFunc,
                exceptionHandler,
                completedHandler
            );
        }
    }
}
