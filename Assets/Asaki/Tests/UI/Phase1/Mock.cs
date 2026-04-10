using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Broker;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Core.Simulation;
using Asaki.Core.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Asaki.Tests.UI.Phase1
{
    // ---------- Mock Simulation ----------
    public class MockSimulationService : IAsakiSimulationService
    {
        private readonly List<IAsakiTickable> _tickables = new();
        private readonly List<IAsakiFixedTickable> _fixedTickables = new();
        private readonly List<IAsakiLateTickable> _lateTickables = new();

        private bool _isPaused;
        private float _timeScale = 1f;

        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = value;
        }

        public int TickableCount => _tickables.Count;
        public int FixedTickableCount => _fixedTickables.Count;
        public int LateTickableCount => _lateTickables.Count;

        public void Pause() => _isPaused = true;

        public void Resume() => _isPaused = false;

        public void Register(IAsakiTickable tickable) => Register(tickable, 1000);

        public void Register(IAsakiTickable tickable, int priority = 1000)
        {
            if (tickable == null)
                return;
            if (!_tickables.Contains(tickable))
                _tickables.Add(tickable);
        }

        public void Register(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;
            if (!_fixedTickables.Contains(tickable))
                _fixedTickables.Add(tickable);
        }

        public void Register(IAsakiLateTickable tickable, int priority = 1000)
        {
            if (tickable == null)
                return;
            if (!_lateTickables.Contains(tickable))
                _lateTickables.Add(tickable);
        }

        public void Unregister(IAsakiTickable tickable)
        {
            if (tickable == null)
                return;
            _tickables.Remove(tickable);
        }

        public void Unregister(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;
            _fixedTickables.Remove(tickable);
        }

        public void Unregister(IAsakiLateTickable tickable)
        {
            if (tickable == null)
                return;
            _lateTickables.Remove(tickable);
        }

        public void Tick(float deltaTime)
        {
            if (_isPaused)
                return;
            float dt = deltaTime * _timeScale;
            foreach (var t in _tickables.ToArray())
                t?.Tick(dt);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_isPaused)
                return;
            float dt = fixedDeltaTime * _timeScale;
            foreach (var t in _fixedTickables.ToArray())
                t?.FixedTick(dt);
        }

        public void LateTick(float lateDeltaTime)
        {
            if (_isPaused)
                return;
            float dt = lateDeltaTime * _timeScale;
            foreach (var t in _lateTickables.ToArray())
                t?.LateTick(dt);
        }

        public void SimulateTicks(float totalSeconds, float dt = 0.02f)
        {
            int count = Mathf.CeilToInt(totalSeconds / dt);
            for (int i = 0; i < count; i++)
                Tick(dt);
        }
    }

    // ---------- Mock Resource ----------
    public class MockResourceService : IAsakiResourceService
    {
        private readonly Dictionary<string, UnityEngine.Object> _assets = new();

        public int ReleaseCallCount { get; private set; }

        public void RegisterPrefab(string path, GameObject prefab) => _assets[path] = prefab;

        public UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            CancellationToken token = default
        )
            where T : class
        {
            if (!_assets.TryGetValue(location, out var obj))
                return UniTask.FromResult<ResHandle<T>>(null);

            return UniTask.FromResult<ResHandle<T>>(new MockResHandle<T>(location, obj as T, this));
        }

        public UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            onProgress?.Invoke(1f);
            return LoadAsync<T>(location, token);
        }

        public UniTask<ResHandle<UnityEngine.Object>> LoadAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            onProgress?.Invoke(1f);
            if (!_assets.TryGetValue(location, out var obj))
                return UniTask.FromResult<ResHandle<UnityEngine.Object>>(null);

            return UniTask.FromResult<ResHandle<UnityEngine.Object>>(
                new MockResHandle<UnityEngine.Object>(location, obj, this)
            );
        }

        public void Release(string location, Type type)
        {
            ReleaseCallCount++;
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            var result = new List<ResHandle<T>>();
            var list = new List<string>(locations);
            int total = Mathf.Max(1, list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var h = LoadAsync<T>(list[i], token).GetAwaiter().GetResult();
                if (h != null)
                    result.Add(h);
                onProgress?.Invoke((i + 1f) / total);
            }

            return UniTask.FromResult(result);
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            CancellationToken token
        )
            where T : class
        {
            return LoadBatchAsync<T>(locations, null, token);
        }

        public void ReleaseBatch<T>(IEnumerable<string> locations)
            where T : class
        {
            foreach (var _ in locations)
                ReleaseCallCount++;
        }

        public UniTask UnloadUnusedAssets(CancellationToken token = default) =>
            UniTask.CompletedTask;

        public void SetTimeoutSeconds(int timeoutSeconds) { }

        public void OnInit() { }

        public UniTask OnInitAsync() => UniTask.CompletedTask;

        public void OnDispose() { }

        internal void OnHandleDisposed()
        {
            ReleaseCallCount++;
        }
    }

    // ---------- Mock Pool ----------
    public class MockGameObjectPool : IAsakiPool<GameObject>
    {
        private readonly Queue<GameObject> _items = new();
        private readonly Func<GameObject> _factory;

        public string Key { get; }
        public AsakiPoolConfig Config { get; }
        public IAsakiPoolStatistics Statistics => null;
        public Type ObjectType => typeof(GameObject);

        public MockGameObjectPool(Func<GameObject> factory, string key = "mock_pool")
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Key = key;
            Config = new AsakiPoolConfig();
        }

        public GameObject Get()
        {
            var go = _items.Count > 0 ? _items.Dequeue() : _factory();
            if (go != null)
                go.SetActive(true);
            return go;
        }

        public UniTask<GameObject> GetAsync(CancellationToken token = default) =>
            UniTask.FromResult(Get());

        public bool Return(GameObject item)
        {
            if (item == null)
                return false;
            item.SetActive(false);
            _items.Enqueue(item);
            return true;
        }

        public UniTask PrewarmAsync(
            int count,
            int itemsPerFrame = -1,
            CancellationToken token = default
        )
        {
            for (int i = 0; i < count; i++)
            {
                var go = _factory();
                if (go != null)
                {
                    go.SetActive(false);
                    _items.Enqueue(go);
                }
            }
            return UniTask.CompletedTask;
        }

        public void Clear()
        {
            while (_items.Count > 0)
            {
                var go = _items.Dequeue();
                if (go)
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public void Shrink(int targetSize)
        {
            while (_items.Count > targetSize && _items.Count > 0)
            {
                var go = _items.Dequeue();
                if (go)
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public void Dispose() => Clear();
    }

    public class MockPoolService : IAsakiPoolService
    {
        private readonly Dictionary<string, IAsakiPool<GameObject>> _pools = new();

        public bool HasPool(string key) => _pools.ContainsKey(key);

        public UniTask CreatePoolAsync<T>(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            int initialSize = 0,
            int maxSize = 0,
            bool collectionCheck = false,
            CancellationToken token = default
        )
            where T : class
        {
            if (typeof(T) != typeof(GameObject))
                return UniTask.CompletedTask;

            var goFactory = factory as IAsakiPoolObjectFactory<GameObject>;
            _pools[key] = new MockGameObjectPool(goFactory.CreateSync, key);
            return UniTask.CompletedTask;
        }

        public UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            AsakiPoolConfig config = null,
            CancellationToken token = default
        )
            where T : class
        {
            if (typeof(T) != typeof(GameObject))
                return UniTask.FromResult<IAsakiPool<T>>(null);

            var goFactory = factory as IAsakiPoolObjectFactory<GameObject>;
            _pools[key] = new MockGameObjectPool(goFactory.CreateSync, key);

            return UniTask.FromResult(GetPool<T>(key));
        }

        public IAsakiPool<T> GetPool<T>(string key)
            where T : class
        {
            if (typeof(T) != typeof(GameObject))
                return null;
            if (!_pools.TryGetValue(key, out var pool))
                return null;
            return pool as IAsakiPool<T>;
        }

        public bool DestroyPool(string key)
        {
            if (!_pools.TryGetValue(key, out var pool))
                return false;
            pool.Dispose();
            _pools.Remove(key);
            return true;
        }

        public string GetStatisticsSummary() => $"PoolCount={_pools.Count}";

        public IEnumerable<string> GetAllPoolKeys() => _pools.Keys;

        public void Dispose()
        {
            foreach (var p in _pools.Values)
                p.Dispose();
            _pools.Clear();
        }
    }

    // ---------- Mock Event ----------
    public class MockEventService : IAsakiEventService
    {
        public void Publish<T>(T evt)
            where T : class { }

        public void Subscribe<T>(Action<T> handler)
            where T : class { }

        public void Unsubscribe<T>(Action<T> handler)
            where T : class { }

        public void Clear() { }

        public void Subscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent { }

        public void SubscribeWeak<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent { }

        public void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent { }

        public void Publish<T>(in T e)
            where T : IAsakiEvent { }

        public void Dispose() { }
    }

    // ---------- ResHandle ----------
    public class MockResHandle<T> : ResHandle<T>
        where T : class
    {
        private bool _disposed;
        private readonly MockResourceService _owner;

        public MockResHandle(string location, T asset, MockResourceService owner)
            : base(location, asset, owner)
        {
            _owner = owner;
        }

        public override bool IsValid => !_disposed && Asset != null;

        public override void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _owner?.OnHandleDisposed();
        }
    }

    // ---------- Test Window ----------
    [RequireComponent(typeof(CanvasGroup))]
    public class TestWindow : Asaki.Unity.Services.UI.AsakiUIWindow, IAsakiWindowWithResult
    {
        public int OpenCount { get; private set; }
        public int CloseCount { get; private set; }
        public object LastReturnValue { get; private set; }

        protected override void OnRefresh(object args) => OpenCount++;

        public override async UniTask OnCloseAsync(CancellationToken token)
        {
            CloseCount++;
            await base.OnCloseAsync(token);
        }

        public void OnReturnValue(object value) => LastReturnValue = value;
    }

    public static class Phase1TestUtils
    {
        public static GameObject CreateWindowPrefab(string name = "TestWindowPrefab")
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasGroup>();
            go.AddComponent<TestWindow>();
            return go;
        }

        public static AsakiUIConfig BuildConfig(params UIInfo[] infos)
        {
            var cfg = new AsakiUIConfig { ResourceReleaseDelaySeconds = 0.2f };
            cfg.UIList = new List<UIInfo>(infos);
            cfg.InitializeLookup();
            return cfg;
        }
    }
}
