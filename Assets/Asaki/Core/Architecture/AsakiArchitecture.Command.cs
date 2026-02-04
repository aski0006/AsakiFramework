using System;
using System.Diagnostics;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture
{
    public abstract partial class AsakiArchitecture
    {
        private bool _enableCommandProfiling = false;
        private bool _enableCommandLogging = false;

        public void EnableCommandProfiling(bool enable)
        {
            _enableCommandProfiling = enable;
        }

        public void EnableCommandLogging(bool enable)
        {
            _enableCommandLogging = enable;
        }

        public void SendCommand<TCommand>()
            where TCommand : class, IAsakiCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, false, false);
            
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {commandType}");

                cmd.Execute();
                
                sw?.Stop();
                
                if (_enableCommandProfiling)
                {
                    ALog.Info($"[Command] {commandType} took {sw.ElapsedMilliseconds}ms");
                }
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[Command] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public TResult SendCommand<TCommand, TResult>()
            where TCommand : class, IAsakiCommand<TResult>, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, false, false);
            
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {commandType}");

                TResult result = cmd.Execute();
                
                sw?.Stop();
                
                if (_enableCommandProfiling)
                {
                    ALog.Info($"[Command] {commandType} took {sw.ElapsedMilliseconds}ms");
                }
                
                string resultValue = result?.ToString() ?? "null";
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    true,
                    typeof(TResult).Name,
                    resultValue.Length > 100 ? resultValue.Substring(0, 100) + "..." : resultValue,
                    false,
                    false,
                    false,
                    null
                ));

                return result;
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[Command] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public async UniTask SendCommandAsync<TCommand>()
            where TCommand : class, IAsakiCommandAsync, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, true, false);
            
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {commandType}");

                await cmd.ExecuteAsync();
                
                sw?.Stop();
                
                if (_enableCommandProfiling)
                {
                    ALog.Info($"[CommandAsync] {commandType} took {sw.ElapsedMilliseconds}ms");
                }
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[CommandAsync] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public async UniTask<TResult> SendCommandAsync<TCommand, TResult>()
            where TCommand : class, IAsakiCommandAsync<TResult>, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, true, false);
            
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {commandType}");

                TResult result = await cmd.ExecuteAsync();
                
                sw?.Stop();
                
                if (_enableCommandProfiling)
                {
                    ALog.Info($"[CommandAsync] {commandType} took {sw.ElapsedMilliseconds}ms");
                }
                
                string resultValue = result?.ToString() ?? "null";
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    true,
                    typeof(TResult).Name,
                    resultValue.Length > 100 ? resultValue.Substring(0, 100) + "..." : resultValue,
                    true,
                    false,
                    false,
                    null
                ));

                return result;
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[CommandAsync] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public void SendCommand<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, false, false);
            
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {commandType}");

                cmd.Execute();
                
                sw?.Stop();
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[Command] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public TResult SendCommand<TCommand, TResult>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommand<TResult>, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, false, false);
            
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {commandType}");

                TResult result = cmd.Execute();
                
                sw?.Stop();
                
                string resultValue = result?.ToString() ?? "null";
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    true,
                    typeof(TResult).Name,
                    resultValue.Length > 100 ? resultValue.Substring(0, 100) + "..." : resultValue,
                    false,
                    false,
                    false,
                    null
                ));

                return result;
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[Command] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    false,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public async UniTask SendCommandAsync<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommandAsync, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, true, false);
            
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {commandType}");

                await cmd.ExecuteAsync();
                
                sw?.Stop();
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[CommandAsync] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        public async UniTask<TResult> SendCommandAsync<TCommand, TResult>(
            Action<TCommand> configure
        )
            where TCommand : class, IAsakiCommandAsync<TResult>, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            long timestamp = DateTime.Now.Ticks;
            Stopwatch sw = _enableCommandProfiling || AsakiCommandDebugger.IsEnabled 
                ? Stopwatch.StartNew() 
                : null;
            
            AsakiCommandDebugger.NotifyExecuting(commandType, true, false);
            
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {commandType}");

                TResult result = await cmd.ExecuteAsync();
                
                sw?.Stop();
                
                string resultValue = result?.ToString() ?? "null";
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    true,
                    typeof(TResult).Name,
                    resultValue.Length > 100 ? resultValue.Substring(0, 100) + "..." : resultValue,
                    true,
                    false,
                    false,
                    null
                ));

                return result;
            }
            catch (Exception ex)
            {
                sw?.Stop();
                ALog.Error($"[CommandAsync] {commandType} failed: {ex.Message}", ex);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    timestamp,
                    sw?.Elapsed.TotalMilliseconds ?? 0,
                    false,
                    null,
                    null,
                    true,
                    false,
                    true,
                    ex.Message
                ));
                
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }
    }
}
