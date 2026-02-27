using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        /// <summary>
        /// 异步初始化UI配置查找表，并进行运行时验证。
        /// </summary>
        /// <returns>完成的UniTask。</returns>
        public UniTask OnInitAsync()
        {
            if (_uiConfig == null)
            {
                ALog.Warn("[AsakiUI] No UIConfig assigned in AsakiFrameworkSetting!");
                return UniTask.CompletedTask;
            }

            if (_uiConfig.UIList == null || _uiConfig.UIList.Count == 0)
            {
                ALog.Warn("[AsakiUI] UIConfig.UIList is empty. " +
                    "This may be caused by AsakiUIGeneratorWindow not syncing correctly. " +
                    "Please use Asaki/Window/UI Asset Generator to regenerate.");
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
            if (token.IsCancellationRequested)
                return null;

            if (_uiConfig == null || !_uiConfig.TryGet(uiId, out UIInfo info))
            {
                ALog.Warn($"[AsakiUI] UI ID {uiId} not found.");
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
                        return null;
                    instance = (window as Component)?.gameObject;
                }
                else
                {
                    (window, rawHandle, instance) = await CreateWindowAsync<T>(info, parent, token);
                    if (window == null)
                        return null;
                }

                _windowLayerMap[window] = info.Layer;

                _inputBlocker.OnWindowOpened(info.Layer);

                await window.OnOpenAsync(args, token);

                if (info.Layer == AsakiUILayer.Normal)
                {
                    _navigationStack.Push(window);
                }
                _windowInstanceMap[uiId] = window;
                return window;
            }
            catch (Exception e)
            {
                HandleOpenFailure(instance, info, rawHandle);
                ALog.Error($"[AsakiUI] OpenUI Failed: {e.Message}", e);
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
                    Object.Destroy(instance);
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
            _pendingDestroyQueue.Enqueue(window);
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
                ProcessCloseRequest(window);
            }

            _resourceManager.ProcessDelayedRelease(deltaTime);
        }

        private void ProcessCloseRequest(IAsakiWindow window)
        {
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
            }

            HandleCloseAsync(window).Forget();

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

        private async UniTask HandleCloseAsync(IAsakiWindow window)
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

            await window.OnCloseAsync(CancellationToken.None);

            if (!isPooled && handle.HasResource && !string.IsNullOrEmpty(assetPath))
            {
                _resourceManager.ScheduleRelease(assetPath, handle);
            }
        }

        #region 查询接口实现

        public bool IsOpened(int uiId)
        {
            return _windowInstanceMap.ContainsKey(uiId);
        }

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

        public bool HasPopup()
        {
            return _inputBlocker.HasActivePopup;
        }

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

        public async UniTask Back(object returnValue)
        {
            if (_navigationStack.Count == 0)
                return;

            _navigationStack.PushReturnValue(returnValue);

            var topWindow = _navigationStack.Peek();
            await topWindow.OnCloseAsync(CancellationToken.None);

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
                _pendingDestroyQueue.Enqueue(window);
            }

            if (includePopup)
            {
                var popupWindows = _windowLayerMap
                    .Where(kvp => kvp.Value == AsakiUILayer.Popup)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var popup in popupWindows)
                {
                    _pendingDestroyQueue.Enqueue(popup);
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
                await oldWindow.OnCloseAsync(token);
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

            if (_inputBlocker != null)
            {
                _inputBlocker.Reset();
            }

            if (_simulationService != null)
            {
                _simulationService.Unregister(this);
            }

            if (_uiRoot != null && !_uiRoot.Equals(null))
            {
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }
        }
    }
}
