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

        public void EnableCommandProfiling(bool enable) => _enableCommandProfiling = enable;

        public void EnableCommandLogging(bool enable) => _enableCommandLogging = enable;

        public void SendCommand<TCommand>()
            where TCommand : class, IAsakiCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {typeof(TCommand).Name}");

                if (_enableCommandProfiling)
                {
                    var sw = Stopwatch.StartNew();
                    cmd.Execute();
                    sw.Stop();
                    ALog.Info($"[Command] {typeof(TCommand).Name} took {sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    cmd.Execute();
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[Command] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {typeof(TCommand).Name}");

                TResult result;
                if (_enableCommandProfiling)
                {
                    var sw = Stopwatch.StartNew();
                    result = cmd.Execute();
                    sw.Stop();
                    ALog.Info($"[Command] {typeof(TCommand).Name} took {sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    result = cmd.Execute();
                }

                return result;
            }
            catch (Exception ex)
            {
                ALog.Error($"[Command] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {typeof(TCommand).Name}");

                if (_enableCommandProfiling)
                {
                    var sw = Stopwatch.StartNew();
                    await cmd.ExecuteAsync();
                    sw.Stop();
                    ALog.Info(
                        $"[CommandAsync] {typeof(TCommand).Name} took {sw.ElapsedMilliseconds}ms"
                    );
                }
                else
                {
                    await cmd.ExecuteAsync();
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[CommandAsync] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {typeof(TCommand).Name}");

                TResult result;
                if (_enableCommandProfiling)
                {
                    var sw = Stopwatch.StartNew();
                    result = await cmd.ExecuteAsync();
                    sw.Stop();
                    ALog.Info(
                        $"[CommandAsync] {typeof(TCommand).Name} took {sw.ElapsedMilliseconds}ms"
                    );
                }
                else
                {
                    result = await cmd.ExecuteAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                ALog.Error($"[CommandAsync] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                configure?.Invoke(cmd); // 配置参数
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {typeof(TCommand).Name}");

                cmd.Execute();
            }
            catch (Exception ex)
            {
                ALog.Error($"[Command] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[Command] Executing {typeof(TCommand).Name}");

                return cmd.Execute();
            }
            catch (Exception ex)
            {
                ALog.Error($"[Command] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {typeof(TCommand).Name}");

                await cmd.ExecuteAsync();
            }
            catch (Exception ex)
            {
                ALog.Error($"[CommandAsync] {typeof(TCommand).Name} failed: {ex.Message}", ex);
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
            try
            {
                configure?.Invoke(cmd);
                cmd.Create(this);

                if (_enableCommandLogging)
                    ALog.Info($"[CommandAsync] Executing {typeof(TCommand).Name}");

                return await cmd.ExecuteAsync();
            }
            catch (Exception ex)
            {
                ALog.Error($"[CommandAsync] {typeof(TCommand).Name} failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }
    }
}
