using System;
using System.Diagnostics;
using System.Threading;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// 自动保存服务实现
    /// </summary>
    public class AsakiAutoSaveService : IAsakiAutoSaveService
    {
        private IAsakiSaveSlotManager _slotManager;
        private IAsakiEventService _eventService;
        private IAsakiAutoSaveConfig _config;
        private Func<IAsakiSavable> _dataProvider;

        private bool _isRunning;
        private bool _isPaused;
        private float _timer;
        private long _lastSaveTime;
        private int _saveCount;
        private bool _isAutoSaving;
        private CancellationTokenSource _countdownCts;

        /// <inheritdoc />
        public IAsakiAutoSaveConfig Config => _config;

        /// <inheritdoc />
        public bool IsAutoSaving => _isAutoSaving;

        /// <inheritdoc />
        public float TimeUntilNextAutoSave => _config?.Enabled == true && _config.Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval)
            ? Mathf.Max(0, _config.TimeIntervalSeconds - _timer)
            : -1;

        /// <inheritdoc />
        public long LastAutoSaveTime => _lastSaveTime;

        /// <inheritdoc />
        public int AutoSaveCount => _saveCount;

        /// <inheritdoc />
        public event Action<IAsakiAutoSaveConfig> OnConfigChanged;

        /// <inheritdoc />
        public event Action<AsakiAutoSaveEventArgs> OnAutoSaveBegin;

        /// <inheritdoc />
        public event Action<AsakiAutoSaveEventArgs> OnAutoSaveComplete;

        /// <inheritdoc />
        public event Action<float> OnCountdownBegin;

        /// <inheritdoc />
        public event Action<float> OnCountdownUpdate;

        /// <inheritdoc />
        public event Action OnCountdownCancelled;

        public AsakiAutoSaveService(
            IAsakiSaveSlotManager slotManager,
            IAsakiEventService eventService)
        {
            _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _config = AsakiAutoSaveConfig.CreateDefault();
        }

        // 无参构造函数供框架使用
        public AsakiAutoSaveService()
        {
            _config = AsakiAutoSaveConfig.CreateDefault();
        }

        public void Init(IAsakiSaveSlotManager slotManager, IAsakiEventService eventService)
        {
            _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        public void OnInit()
        {
            // 注册应用生命周期事件
            Application.focusChanged += OnApplicationFocusChanged;
            Application.quitting += OnApplicationQuitting;
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            StopService();
            Application.focusChanged -= OnApplicationFocusChanged;
            Application.quitting -= OnApplicationQuitting;
        }

        /// <inheritdoc />
        public void SetConfig(IAsakiAutoSaveConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.Validate(out var errorMessage))
            {
                ALog.Error($"[AsakiAutoSaveService] Invalid config: {errorMessage}");
                return;
            }

            _config = config;
            OnConfigChanged?.Invoke(_config);

            ALog.Info($"[AsakiAutoSaveService] Config updated: Enabled={_config.Enabled}, Triggers={_config.Triggers}");
        }

        /// <inheritdoc />
        public void RegisterDataProvider<TData>(Func<TData> provider) where TData : IAsakiSavable
        {
            _dataProvider = () => provider();
            ALog.Info($"[AsakiAutoSaveService] Data provider registered for {typeof(TData).Name}");
        }

        /// <inheritdoc />
        public void StartService()
        {
            if (_isRunning)
                return;

            if (!_config.Enabled)
            {
                ALog.Warn("[AsakiAutoSaveService] Cannot start: AutoSave is disabled");
                return;
            }

            if (_dataProvider == null)
            {
                ALog.Warn("[AsakiAutoSaveService] Cannot start: No data provider registered");
                return;
            }

            _isRunning = true;
            _timer = 0f;

            ALog.Info("[AsakiAutoSaveService] Service started");
        }

        /// <inheritdoc />
        public void StopService()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = null;

            ALog.Info("[AsakiAutoSaveService] Service stopped");
        }

        /// <inheritdoc />
        public void Pause()
        {
            if (_isPaused)
                return;

            _isPaused = true;
            ALog.Info("[AsakiAutoSaveService] Service paused");
        }

        /// <inheritdoc />
        public void Resume()
        {
            if (!_isPaused)
                return;

            _isPaused = false;
            ALog.Info("[AsakiAutoSaveService] Service resumed");
        }

        /// <inheritdoc />
        public async UniTask<bool> ForceAutoSaveAsync(
            AsakiAutoSaveTrigger trigger = AsakiAutoSaveTrigger.Manual,
            CancellationToken token = default)
        {
            return await ExecuteAutoSaveAsync(trigger, true, token);
        }

        /// <inheritdoc />
        public async UniTask<bool> TriggerCheckpointSaveAsync(
            string checkpointName = null,
            CancellationToken token = default)
        {
            if (!_config.Triggers.HasFlag(AsakiAutoSaveTrigger.Checkpoint))
                return false;

            return await ExecuteAutoSaveAsync(AsakiAutoSaveTrigger.Checkpoint, false, token);
        }

        /// <inheritdoc />
        public async UniTask<bool> TriggerSceneSaveAsync(
            string sceneName,
            bool isEnter,
            CancellationToken token = default)
        {
            if (!_config.Triggers.HasFlag(AsakiAutoSaveTrigger.SceneChange))
                return false;

            if (isEnter && !_config.SaveOnSceneEnter)
                return false;

            if (!isEnter && !_config.SaveOnSceneExit)
                return false;

            return await ExecuteAutoSaveAsync(AsakiAutoSaveTrigger.SceneChange, false, token);
        }

        /// <inheritdoc />
        public void CancelCountdown()
        {
            _countdownCts?.Cancel();
        }

        /// <inheritdoc />
        public void ResetTimer()
        {
            _timer = 0f;
        }

        /// <inheritdoc />
        public bool CanAutoSave()
        {
            if (!_config.Enabled || !_isRunning || _isPaused)
                return false;

            if (_dataProvider == null)
                return false;

            // 检查最小间隔
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - _lastSaveTime < _config.MinIntervalBetweenSaves)
                return false;

            return true;
        }

        /// <inheritdoc />
        public DateTime? GetNextAutoSaveTime()
        {
            if (!_config.Enabled || !_isRunning)
                return null;

            if (!_config.Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval))
                return null;

            var secondsRemaining = _config.TimeIntervalSeconds - _timer;
            return DateTime.Now.AddSeconds(secondsRemaining);
        }

        // =========================================================
        // 私有方法
        // =========================================================

        private void Update()
        {
            if (!_isRunning || _isPaused)
            {
                return;
            }

            if (!_config.Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval))
            {
                return;
            }

            _timer += UnityEngine.Time.unscaledDeltaTime;

            if (_timer >= _config.TimeIntervalSeconds)
            {
                _timer = 0f;
                ExecuteAutoSaveWithCountdown(AsakiAutoSaveTrigger.TimeInterval).Forget();
            }
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                Resume();
            }
            else
            {
                // 应用失去焦点时尝试自动保存
                if (_config.Triggers.HasFlag(AsakiAutoSaveTrigger.ApplicationPause))
                {
                    ExecuteAutoSaveAsync(AsakiAutoSaveTrigger.ApplicationPause, false).Forget();
                }
                Pause();
            }
        }

        private void OnApplicationQuitting()
        {
            // 应用退出前尝试自动保存
            if (_config.Triggers.HasFlag(AsakiAutoSaveTrigger.ApplicationPause))
            {
                ExecuteAutoSaveAsync(AsakiAutoSaveTrigger.ApplicationPause, true).Forget();
            }
        }

        private async UniTaskVoid ExecuteAutoSaveWithCountdown(AsakiAutoSaveTrigger trigger)
        {
            if (!CanAutoSave())
                return;

            // 如果没有倒计时或倒计时已禁用，直接保存
            if (_config.CountdownSeconds <= 0)
            {
                await ExecuteAutoSaveAsync(trigger, false);
                return;
            }

            // 取消之前的倒计时
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = new CancellationTokenSource();
            var token = _countdownCts.Token;

            try
            {
                OnCountdownBegin?.Invoke(_config.CountdownSeconds);

                var startTime = UnityEngine.Time.unscaledTime;
                while (UnityEngine.Time.unscaledTime - startTime < _config.CountdownSeconds)
                {
                    token.ThrowIfCancellationRequested();

                    var remaining = _config.CountdownSeconds - (UnityEngine.Time.unscaledTime - startTime);
                    OnCountdownUpdate?.Invoke(remaining);

                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
                }

                // 倒计时结束，执行保存
                await ExecuteAutoSaveAsync(trigger, false);
            }
            catch (OperationCanceledException)
            {
                OnCountdownCancelled?.Invoke();
                ALog.Info("[AsakiAutoSaveService] AutoSave countdown cancelled");
            }
            finally
            {
                _countdownCts?.Dispose();
                _countdownCts = null;
            }
        }

        private async UniTask<bool> ExecuteAutoSaveAsync(
            AsakiAutoSaveTrigger trigger,
            bool skipChecks,
            CancellationToken externalToken = default)
        {
            if (!skipChecks && !CanAutoSave())
                return false;

            if (_isAutoSaving)
            {
                ALog.Warn("[AsakiAutoSaveService] AutoSave already in progress");
                return false;
            }

            _isAutoSaving = true;
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            string errorMessage = null;
            AsakiSaveSlot slot = null;

            try
            {
                // 检查存储空间
                if (_config.CheckStorageSpace)
                {
                    var freeSpace = GetAvailableStorageSpaceMB();
                    if (freeSpace < _config.MinFreeSpaceMB)
                    {
                        throw new InsufficientStorageException($"Not enough storage space. Required: {_config.MinFreeSpaceMB}MB, Available: {freeSpace}MB");
                    }
                }

                // 获取存档数据
                var data = _dataProvider?.Invoke();
                if (data == null)
                {
                    throw new InvalidOperationException("Data provider returned null");
                }

                // 发布开始事件
                var beginArgs = new AsakiAutoSaveEventArgs
                {
                    Trigger = trigger,
                    Success = false
                };
                OnAutoSaveBegin?.Invoke(beginArgs);

                // 显示通知
                if (_config.ShowNotification)
                {
                    _eventService.Publish(new AsakiAutoSaveNotificationEvent
                    {
                        Message = _config.NotificationText,
                        Duration = 2f
                    });
                }

                // 执行保存
                slot = await _slotManager.AutoSaveAsync(data, externalToken) as AsakiSaveSlot;
                success = true;
                _saveCount++;
                _lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _timer = 0f;

                ALog.Info($"[AsakiAutoSaveService] AutoSave completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                ALog.Error($"[AsakiAutoSaveService] AutoSave failed: {errorMessage}");
            }
            finally
            {
                stopwatch.Stop();
                _isAutoSaving = false;

                // 发布完成事件
                var completeArgs = new AsakiAutoSaveEventArgs
                {
                    Slot = slot,
                    Trigger = trigger,
                    Success = success,
                    ErrorMessage = errorMessage,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
                OnAutoSaveComplete?.Invoke(completeArgs);
            }

            return success;
        }

        private long GetAvailableStorageSpaceMB()
        {
            try
            {
                var path = Application.persistentDataPath;
                var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(path));
                return driveInfo.AvailableFreeSpace / (1024 * 1024);
            }
            catch
            {
                // 如果无法获取，返回一个安全值
                return 1000;
            }
        }
    }

    /// <summary>
    /// 存储空间不足异常
    /// </summary>
    public class InsufficientStorageException : Exception
    {
        public InsufficientStorageException(string message) : base(message) { }
        public InsufficientStorageException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 自动保存通知事件
    /// </summary>
    public struct AsakiAutoSaveNotificationEvent : IAsakiEvent
    {
        /// <summary>
        /// 通知消息
        /// </summary>
        public string Message;

        /// <summary>
        /// 显示持续时间
        /// </summary>
        public float Duration;
    }
}
