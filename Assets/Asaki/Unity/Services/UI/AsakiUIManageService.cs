using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Core.Simulation;
using Asaki.Core.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.UI
{
    /// <summary>
    /// UI管理服务，协调窗口生命周期、导航和资源管理。
    /// </summary>
    public class AsakiUIManageService : IAsakiUIService, IAsakiTickable
    {
        private AsakiUIRoot _uiRoot;
        private IAsakiResourceService _resourceService;
        private IAsakiPoolService _poolService;
        private IAsakiSimulationService _simulationService;
        private AsakiUIConfig _uiConfig;
        private readonly Vector2 _refRes;
        private readonly float _matchMode;

        private readonly UINavigationStack _navigationStack = new UINavigationStack();
        private UIInputBlocker _inputBlocker;
        private UIResourceManager _resourceManager;

        private readonly ConcurrentDictionary<IAsakiWindow, AsakiUILayer> _windowLayerMap =
            new ConcurrentDictionary<IAsakiWindow, AsakiUILayer>();
        private readonly ConcurrentDictionary<int, IAsakiWindow> _windowInstanceMap =
            new ConcurrentDictionary<int, IAsakiWindow>();
        private readonly ConcurrentDictionary<Type, int> _typeToIdCache =
            new ConcurrentDictionary<Type, int>();
        private readonly HashSet<string> _pooledAssets = new HashSet<string>();

        private readonly ConcurrentQueue<IAsakiWindow> _pendingDestroyQueue =
            new ConcurrentQueue<IAsakiWindow>();

        // [Phase1] 防止同一窗口重复进入关闭流程（Close/Back/ClearStack/Replace 竞争）
        private readonly ConcurrentDictionary<IAsakiWindow, byte> _closingWindows =
            new ConcurrentDictionary<IAsakiWindow, byte>();

        private readonly ConcurrentDictionary<string, int> _uiErrorCounters =
            new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<int, float> _uiLastOpenDurationMs =
            new ConcurrentDictionary<int, float>();

        private readonly ConcurrentDictionary<AsakiUILayer, int> _layerOpenCounters =
            new ConcurrentDictionary<AsakiUILayer, int>();

        private readonly ConcurrentDictionary<int, OpenPerfStats> _openPerfByUiId =
            new ConcurrentDictionary<int, OpenPerfStats>();

        private readonly object _recentErrorsLock = new object();
        private readonly Queue<UIErrorEvent> _recentErrors = new Queue<UIErrorEvent>(64);
        private const int MaxRecentErrors = 64;

        // 诊断开关（默认编辑器/开发开，发布关）
        private bool _diagnosticsEnabled =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        public bool DiagnosticsEnabled
        {
            get => _diagnosticsEnabled;
            set => _diagnosticsEnabled = value;
        }

        [Serializable]
        private class UIDiagnosticsSnapshot
        {
            public string generatedAtUtc;
            public int openedWindowCount;
            public int navigationStackCount;
            public bool hasPopup;

            public List<LayerCounterItem> layerCounters = new();
            public List<OpenPerfItem> openPerf = new();
            public List<ErrorCounterItem> errorCounters = new();
            public List<RecentErrorItem> recentErrors = new();
        }

        [Serializable]
        private class LayerCounterItem
        {
            public string layer;
            public int count;
        }

        [Serializable]
        private class OpenPerfItem
        {
            public int uiId;
            public int count;
            public float avgMs;
            public float maxMs;
            public float lastMs;
        }

        [Serializable]
        private class ErrorCounterItem
        {
            public string code;
            public int count;
        }

        [Serializable]
        private class RecentErrorItem
        {
            public string timeUtc;
            public int uiId;
            public string code;
            public string message;
        }

        private struct OpenPerfStats
        {
            public int Count;
            public float TotalMs;
            public float MaxMs;
            public float LastMs;
        }

        private struct UIErrorEvent
        {
            public string TimeUtc;
            public int UiId;
            public string Code;
            public string Message;
        }

        private void RecordOpenPerf(int uiId, float ms)
        {
            if (!_diagnosticsEnabled)
                return;
            _openPerfByUiId.AddOrUpdate(
                uiId,
                _ => new OpenPerfStats
                {
                    Count = 1,
                    TotalMs = ms,
                    MaxMs = ms,
                    LastMs = ms,
                },
                (_, old) =>
                {
                    old.Count++;
                    old.TotalMs += ms;
                    if (ms > old.MaxMs)
                        old.MaxMs = ms;
                    old.LastMs = ms;
                    return old;
                }
            );
        }

        private void IncLayerCounter(AsakiUILayer layer)
        {
            if (!_diagnosticsEnabled)
                return;
            _layerOpenCounters.AddOrUpdate(layer, 1, (_, old) => old + 1);
        }

        private void DecLayerCounter(AsakiUILayer layer)
        {
            if (!_diagnosticsEnabled)
                return;
            _layerOpenCounters.AddOrUpdate(layer, 0, (_, old) => Mathf.Max(0, old - 1));
        }

        private void PushRecentError(int uiId, string code, string message)
        {
            if (!_diagnosticsEnabled)
                return;
            lock (_recentErrorsLock)
            {
                if (_recentErrors.Count >= MaxRecentErrors)
                    _recentErrors.Dequeue();

                _recentErrors.Enqueue(
                    new UIErrorEvent
                    {
                        TimeUtc = DateTime.UtcNow.ToString("o"),
                        UiId = uiId,
                        Code = code,
                        Message = message,
                    }
                );
            }
        }

        private void CountAndRecordError(int uiId, string code, string message)
        {
            CountUiError(code);
            PushRecentError(uiId, code, message);
        }

        private void CountUiError(string code)
        {
            if (!_diagnosticsEnabled)
                return;
            _uiErrorCounters.AddOrUpdate(code, 1, (_, old) => old + 1);
        }

        private static float ElapsedMs(long startTicks)
        {
            long end = DateTime.UtcNow.Ticks;
            return (end - startTicks) / 10000f; // ticks -> ms
        }

        // 可公开给调试台调用
        private UIDiagnosticsSnapshot BuildSnapshot()
        {
            var snap = new UIDiagnosticsSnapshot
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                openedWindowCount = _windowInstanceMap.Count,
                navigationStackCount = _navigationStack.Count,
                hasPopup = _inputBlocker?.HasActivePopup ?? false,
            };

            foreach (AsakiUILayer layer in Enum.GetValues(typeof(AsakiUILayer)))
            {
                _layerOpenCounters.TryGetValue(layer, out int c);
                snap.layerCounters.Add(
                    new LayerCounterItem { layer = layer.ToString(), count = c }
                );
            }

            foreach (var kv in _openPerfByUiId.OrderBy(x => x.Key))
            {
                var s = kv.Value;
                float avg = s.Count > 0 ? s.TotalMs / s.Count : 0f;
                snap.openPerf.Add(
                    new OpenPerfItem
                    {
                        uiId = kv.Key,
                        count = s.Count,
                        avgMs = avg,
                        maxMs = s.MaxMs,
                        lastMs = s.LastMs,
                    }
                );
            }

            foreach (var kv in _uiErrorCounters.OrderByDescending(x => x.Value))
            {
                snap.errorCounters.Add(new ErrorCounterItem { code = kv.Key, count = kv.Value });
            }

            lock (_recentErrorsLock)
            {
                foreach (var e in _recentErrors)
                {
                    snap.recentErrors.Add(
                        new RecentErrorItem
                        {
                            timeUtc = e.TimeUtc,
                            uiId = e.UiId,
                            code = e.Code,
                            message = e.Message,
                        }
                    );
                }
            }

            return snap;
        }

        public string DumpDiagnosticsJson(bool pretty = true)
        {
            if (!_diagnosticsEnabled)
                return "{\"disabled\":true}";

            var snap = BuildSnapshot();
            return JsonUtility.ToJson(snap, pretty);
        }

        public void LogDiagnosticsJson(bool pretty = false)
        {
            if (!_diagnosticsEnabled)
                return;
            ALog.Info(DumpDiagnosticsJson(pretty));
        }

        public bool TryWriteDiagnosticsJsonToFile(string absolutePath, bool pretty = true)
        {
            if (!_diagnosticsEnabled)
                return false;

            try
            {
                var json = DumpDiagnosticsJson(pretty);
                File.WriteAllText(absolutePath, json);
                return true;
            }
            catch (Exception e)
            {
                ALog.Warn($"[AsakiUI][Diagnostics] Write file failed: {e.Message}");
                return false;
            }
        }

        public AsakiUIManageService(
            AsakiUIConfig configAsset,
            Vector2 refRes,
            float matchMode,
            IAsakiEventService eventService,
            IAsakiResourceService resourceService,
            IAsakiPoolService poolService
        )
        {
            if (eventService == null)
                throw new ArgumentNullException(nameof(eventService));
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _uiConfig = configAsset;
            _refRes = refRes;
            _matchMode = matchMode;
        }

        public void OnInit()
        {
            _simulationService = AsakiContext.Get<IAsakiSimulationService>();
            _simulationService.Register(this);

            if (_uiRoot == null)
            {
                GameObject rootGo = new GameObject("Asaki_UIRoot");
                Object.DontDestroyOnLoad(rootGo);
                _uiRoot = rootGo.AddComponent<AsakiUIRoot>();
                _uiRoot.Initialize(_refRes, _matchMode);
            }

            _inputBlocker = new UIInputBlocker(_uiRoot);
            _resourceManager = new UIResourceManager(_uiConfig?.ResourceReleaseDelaySeconds ?? 0f);
        }

        public UniTask OnInitAsync()
        {
            if (_uiConfig == null)
            {
                ALog.Warn("[AsakiUI][Init] No UIConfig assigned in AsakiFrameworkSetting!");
                return UniTask.CompletedTask;
            }

            if (_uiConfig.UIList == null || _uiConfig.UIList.Count == 0)
            {
                ALog.Warn(
                    "[AsakiUI][Init] UIConfig.UIList is empty. "
                        + "This may be caused by AsakiUIGeneratorWindow not syncing correctly. "
                        + "Please use Asaki/Window/UI Asset Generator to regenerate."
                );
                return UniTask.CompletedTask;
            }

            _uiConfig.InitializeLookup();
            return UniTask.CompletedTask;
        }

        public async UniTask<T> OpenAsync<T>(
            int uiId,
            object args = null,
            CancellationToken token = default
        )
            where T : class, IAsakiWindow
        {
            long beginTicks = DateTime.UtcNow.Ticks;

            if (token.IsCancellationRequested)
            {
                CountAndRecordError(
                    uiId,
                    "OpenCanceledBeforeStart",
                    "Token canceled before open started."
                );
                ALog.Warn($"[AsakiUI][OpenCanceled] uiId={uiId}, reason=TokenCanceledBeforeStart");
                return null;
            }

            if (_uiConfig == null || !_uiConfig.TryGet(uiId, out UIInfo info))
            {
                CountAndRecordError(uiId, "ConfigNotFound", "UI config entry not found.");
                ALog.Warn($"[AsakiUI][OpenFailed] uiId={uiId}, reason=ConfigNotFound");
                return null;
            }

            ResHandle<GameObject> rawHandle = null;
            GameObject instance = null;
            T window;

            try
            {
                Transform parent = _uiRoot.GetLayerNode(info.Layer);

                if (info.UsePool)
                {
                    window = await CreatePooledWindowAsync<T>(info, parent, token);
                    if (window == null)
                    {
                        CountAndRecordError(
                            uiId,
                            "CreatePooledWindowFailed",
                            $"CreatePooledWindow failed. assetPath={info.AssetPath}"
                        );
                        ALog.Warn(
                            $"[AsakiUI][OpenFailed] uiId={uiId}, assetPath={info.AssetPath}, reason=CreatePooledWindowFailed"
                        );
                        return null;
                    }

                    instance = (window as Component)?.gameObject;
                }
                else
                {
                    (window, rawHandle, instance) = await CreateWindowAsync<T>(info, parent, token);
                    if (window == null)
                    {
                        CountAndRecordError(
                            uiId,
                            "CreateWindowFailed",
                            $"CreateWindow failed. assetPath={info.AssetPath}"
                        );
                        ALog.Warn(
                            $"[AsakiUI][OpenFailed] uiId={uiId}, assetPath={info.AssetPath}, reason=CreateWindowFailed"
                        );
                        return null;
                    }
                }

                _windowLayerMap[window] = info.Layer;
                _inputBlocker.OnWindowOpened(info.Layer);

                await window.OnOpenAsync(args, token);

                if (info.Layer == AsakiUILayer.Normal)
                {
                    _navigationStack.Push(window);
                }

                float totalMs = ElapsedMs(beginTicks);
                _uiLastOpenDurationMs[uiId] = totalMs;
                RecordOpenPerf(uiId, totalMs);
                IncLayerCounter(info.Layer);
                ALog.Trace(
                    $"[AsakiUI][OpenSuccess] uiId={uiId}, layer={info.Layer}, ms={totalMs:F2}"
                );
                return window;
            }
            catch (Exception e)
            {
                HandleOpenFailure(instance, info, rawHandle);
                CountAndRecordError(uiId, "OpenException", e.Message);
                float totalMs = ElapsedMs(beginTicks);
                ALog.Error(
                    $"[AsakiUI][OpenFailed] uiId={uiId}, assetPath={info.AssetPath}, reason=Exception, ms={totalMs:F2}, message={e.Message}",
                    e
                );
                return null;
            }
        }

        private async UniTask<T> CreatePooledWindowAsync<T>(
            UIInfo info,
            Transform parent,
            CancellationToken token
        )
            where T : class, IAsakiWindow
        {
            if (!_poolService.HasPool(info.AssetPath))
            {
                var prefabHandle = await _resourceService.LoadAsync<GameObject>(
                    info.AssetPath,
                    token
                );
                if (!prefabHandle.IsValid)
                    return null;

                var factory = new GameObjectFactory(prefabHandle.Asset, parent);
                await _poolService.CreatePoolAsync(info.AssetPath, factory, token: token);
            }

            var pool = _poolService.GetPool<GameObject>(info.AssetPath);
            if (pool == null)
                return null;

            var instance = await pool.GetAsync(token);
            if (token.IsCancellationRequested)
                return null;

            _pooledAssets.Add(info.AssetPath);

            var window = instance.GetComponent<T>();
            if (window is AsakiUIWindow baseWindow)
            {
                baseWindow.IsPooled = true;
                baseWindow.PoolKey = info.AssetPath;
                baseWindow.ResHandle = null;
            }

            return window;
        }

        private async UniTask<(
            T window,
            ResHandle<GameObject> handle,
            GameObject instance
        )> CreateWindowAsync<T>(UIInfo info, Transform parent, CancellationToken token)
            where T : class, IAsakiWindow
        {
            ResHandle<GameObject> rawHandle = null;
            GameObject instance = null;

            if (_resourceManager.TryGetReusableHandle(info.AssetPath, out var reusableHandle))
            {
                instance = Object.Instantiate(reusableHandle.Asset, parent);
                var window = instance.GetComponent<T>();
                if (window is AsakiUIWindow baseWindow)
                {
                    baseWindow.IsPooled = false;
                    baseWindow.ResHandle = reusableHandle;
                }

                return (window, null, instance);
            }

            rawHandle = await _resourceService.LoadAsync<GameObject>(info.AssetPath, token);
            if (!rawHandle.IsValid)
                return (null, rawHandle, null);

            if (token.IsCancellationRequested)
            {
                rawHandle.Dispose();
                return (null, null, null);
            }

            instance = Object.Instantiate(rawHandle.Asset, parent);
            var result = instance.GetComponent<T>();
            if (result is AsakiUIWindow uiWindow)
            {
                uiWindow.IsPooled = false;
                uiWindow.ResHandle = new AsakiUIResourceHandleAdapter(rawHandle);
                rawHandle = null;
            }

            return (result, rawHandle, instance);
        }

        private void HandleOpenFailure(
            GameObject instance,
            UIInfo info,
            ResHandle<GameObject> handle
        )
        {
            if (instance != null)
            {
                if (info.UsePool)
                {
                    var pool = _poolService.GetPool<GameObject>(info.AssetPath);
                    if (pool != null)
                        pool.Return(instance);
                    else
                        Object.Destroy(instance);
                }
                else
                {
                    Object.Destroy(instance);
                }
            }

            handle?.Dispose();
        }

        public void Close<T>()
            where T : class, IAsakiWindow
        {
            if (_navigationStack.Peek() is T)
            {
                Close(_navigationStack.Peek());
                return;
            }

            var target = _navigationStack.FindWindow<T>();
            if (target != null)
            {
                Close(target);
            }
            else
            {
                ALog.Warn($"[AsakiUI] Window {typeof(T).Name} not found in stack.");
            }
        }

        public void Close(IAsakiWindow window)
        {
            if (window == null)
                return;

            // [Phase1] 去重，防止重复 close 入队
            if (_closingWindows.TryAdd(window, 0))
            {
                _pendingDestroyQueue.Enqueue(window);
            }
        }

        public void Back()
        {
            if (_navigationStack.Count > 0)
            {
                Close(_navigationStack.Peek());
            }
        }

        public void Tick(float deltaTime)
        {
            while (_pendingDestroyQueue.TryDequeue(out var window))
            {
                CloseInternalAsync(window, CancellationToken.None).Forget();
            }

            _resourceManager.ProcessDelayedRelease(deltaTime);
        }

        // [Phase1] 统一关闭管线
        private async UniTask CloseInternalAsync(IAsakiWindow window, CancellationToken token)
        {
            try
            {
                if (window == null)
                    return;

                if (_navigationStack.Peek() == window)
                {
                    _navigationStack.Pop();
                }
                else if (_navigationStack.Contains(window))
                {
                    _navigationStack.RemoveFromMiddle(window);
                }

                if (_windowLayerMap.TryGetValue(window, out var layer))
                {
                    _inputBlocker.OnWindowClosed(layer);
                    _windowLayerMap.TryRemove(window, out _);
                    DecLayerCounter(layer);
                }

                await HandleCloseAsync(window, token);

                foreach (var pair in _windowInstanceMap)
                {
                    if (pair.Value == window)
                    {
                        _windowInstanceMap.TryRemove(pair.Key, out _);
                        _typeToIdCache.Clear();
                        break;
                    }
                }
            }
            finally
            {
                _closingWindows.TryRemove(window, out _);
            }
        }

        private async UniTask HandleCloseAsync(IAsakiWindow window, CancellationToken token)
        {
            AsakiUIResourceHandleAdapter handle = default;
            string assetPath = null;
            bool isPooled = false;

            if (window is AsakiUIWindow uiWindow && uiWindow != null)
            {
                if (uiWindow.ResHandle is AsakiUIResourceHandleAdapter adapter)
                {
                    handle = adapter;
                    assetPath = handle.Location;
                }

                isPooled = uiWindow.IsPooled;
                uiWindow.ResHandle = null;
            }

            await window.OnCloseAsync(token);

            if (!isPooled && handle.HasResource && !string.IsNullOrEmpty(assetPath))
            {
                _resourceManager.ScheduleRelease(assetPath, handle);
            }
        }

        #region 查询接口实现

        public bool IsOpened(int uiId) => _windowInstanceMap.ContainsKey(uiId);

        public T GetWindow<T>()
            where T : class, IAsakiWindow
        {
            foreach (var pair in _windowInstanceMap)
            {
                if (pair.Value is T target)
                    return target;
            }

            return null;
        }

        public IAsakiWindow GetWindow(int uiId)
        {
            return _windowInstanceMap.TryGetValue(uiId, out var window) ? window : null;
        }

        public IReadOnlyList<IAsakiWindow> GetOpenedWindows(AsakiUILayer? layer = null)
        {
            if (layer == null)
                return new List<IAsakiWindow>(_windowInstanceMap.Values);

            return _windowLayerMap
                .Where(kvp => kvp.Value == layer.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public bool HasPopup() => _inputBlocker.HasActivePopup;

        public int GetActiveWindowCount(AsakiUILayer layer)
        {
            return _windowLayerMap.Count(kvp => kvp.Value == layer);
        }

        #endregion

        #region 导航控制实现

        public void BackTo<T>()
            where T : class, IAsakiWindow
        {
            var target = _navigationStack.FindWindow<T>();
            if (target == null)
            {
                ALog.Warn($"[AsakiUI] Target window {typeof(T).Name} not in stack.");
                return;
            }

            BackTo(target);
        }

        public void BackTo(int uiId)
        {
            var target = GetWindow(uiId);
            if (target == null || !_navigationStack.Contains(target))
            {
                ALog.Warn($"[AsakiUI] UI ID {uiId} not in navigation stack.");
                return;
            }

            BackTo(target);
        }

        private void BackTo(IAsakiWindow target)
        {
            while (_navigationStack.Count > 0 && _navigationStack.Peek() != target)
            {
                Close(_navigationStack.Peek());
            }
        }

        // [Phase1] 修复：Back(returnValue) 统一走服务关闭管线，避免只调用 OnCloseAsync
        public async UniTask Back(object returnValue)
        {
            if (_navigationStack.Count == 0)
                return;

            var topWindow = _navigationStack.Peek();
            _navigationStack.PushReturnValue(returnValue);

            if (_closingWindows.TryAdd(topWindow, 0))
            {
                await CloseInternalAsync(topWindow, CancellationToken.None);
            }

            if (_navigationStack.Count > 0)
            {
                var nextWindow = _navigationStack.Peek();
                (nextWindow as IAsakiWindowWithResult)?.OnReturnValue(returnValue);
            }

            _navigationStack.PopReturnValue();
        }

        public void ClearStack(bool includePopup = false)
        {
            while (_navigationStack.Count > 0)
            {
                var window = _navigationStack.Pop();
                if (_closingWindows.TryAdd(window, 0))
                {
                    _pendingDestroyQueue.Enqueue(window);
                }
            }

            if (includePopup)
            {
                var popupWindows = _windowLayerMap
                    .Where(kvp => kvp.Value == AsakiUILayer.Popup)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var popup in popupWindows)
                {
                    if (_closingWindows.TryAdd(popup, 0))
                    {
                        _pendingDestroyQueue.Enqueue(popup);
                    }
                }
            }
        }

        public async UniTask<T> ReplaceAsync<T>(
            int uiId,
            object args = null,
            CancellationToken token = default
        )
            where T : class, IAsakiWindow
        {
            if (_navigationStack.Count > 0)
            {
                var oldWindow = _navigationStack.Peek();
                if (_closingWindows.TryAdd(oldWindow, 0))
                {
                    await CloseInternalAsync(oldWindow, token);
                }
            }

            return await OpenAsync<T>(uiId, args, token);
        }

        #endregion

        public void OnDispose()
        {
            while (_navigationStack.Count > 0)
            {
                var window = _navigationStack.Pop();
                if (window is AsakiUIWindow uiWindow && uiWindow != null && !uiWindow.Equals(null))
                {
                    uiWindow.DisposeImmediately();
                }
            }

            _navigationStack.Clear();

            while (_pendingDestroyQueue.TryDequeue(out var window))
            {
                if (window is AsakiUIWindow uiWindow && uiWindow != null && !uiWindow.Equals(null))
                {
                    uiWindow.DisposeImmediately();
                }
            }

            _resourceManager.ReleaseAll();

            if (_poolService != null)
            {
                foreach (string assetPath in _pooledAssets)
                {
                    _poolService.DestroyPool(assetPath);
                }
            }
            else
            {
                ALog.Warn("[AsakiUI] PoolService is null during disposal, pooled assets may leak.");
            }

            _pooledAssets.Clear();
            _windowLayerMap.Clear();
            _windowInstanceMap.Clear();
            _typeToIdCache.Clear();
            _closingWindows.Clear();
            _layerOpenCounters.Clear();
            _openPerfByUiId.Clear();
            lock (_recentErrorsLock)
                _recentErrors.Clear();
            _inputBlocker?.Reset();

            _simulationService?.Unregister(this);

            if (_uiRoot != null && !_uiRoot.Equals(null))
            {
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }
        }

        public UniTask<TWindow> OpenAsync<TWindow, TArg>(
            int uiId,
            TArg args,
            CancellationToken token = default
        )
            where TWindow : class, IAsakiWindow
        {
            return OpenAsync<TWindow>(uiId, args, token);
        }

        public UniTask Back<TResult>(TResult returnValue)
        {
            return Back((object)returnValue);
        }
    }
}
