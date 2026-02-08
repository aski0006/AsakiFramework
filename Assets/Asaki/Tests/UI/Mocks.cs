// File: Assets/Tests/UI/Mocks.cs
// 共享的 Mock 类，用于 UI 测试

using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Core.Simulation;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Tests.UI
{
    /// <summary>
    /// 模拟仿真服务
    /// </summary>
    public class MockSimulationService : IAsakiSimulationService
    {
        private IAsakiTickable _tickable;
        private float _currentTime = 0f;

        public void Register(IAsakiTickable tickable, int priority = (int)TickPriority.Normal)
        {
            _tickable = tickable;
        }

        public void Register(IAsakiFixedTickable tickable) { }

        public void Register(
            IAsakiLateTickable tickable,
            int priority = (int)TickPriority.Normal
        ) { }

        public void Unregister(IAsakiTickable tickable)
        {
            if (_tickable == tickable)
                _tickable = null;
        }

        public void Unregister(IAsakiFixedTickable tickable) { }

        public void Unregister(IAsakiLateTickable tickable) { }

        public void Tick(float deltaTime)
        {
            _currentTime += deltaTime;
            _tickable?.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime) { }

        public void LateTick(float lateDeltaTime) { }

        public void SimulateTicks(float totalTime, float deltaTime = 0.016f)
        {
            float elapsed = 0f;
            while (elapsed < totalTime)
            {
                float dt = Mathf.Min(deltaTime, totalTime - elapsed);
                Tick(dt);
                elapsed += dt;
            }
        }
    }

    /// <summary>
    /// 模拟资源服务
    /// </summary>
    public class MockResourceService : IAsakiResourceService
    {
        public int LoadCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }
        public string LastReleasedLocation { get; private set; }
        public Type LastReleasedType { get; private set; }

        private int _handleCounter = 0;
        private Dictionary<string, GameObject> _loadedAssets = new Dictionary<string, GameObject>();

        public UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            System.Threading.CancellationToken token
        )
            where T : class
        {
            return LoadAsync<T>(location, null, token);
        }

        public UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            Action<float> onProgress,
            System.Threading.CancellationToken token
        )
            where T : class
        {
            LoadCallCount++;

            if (!_loadedAssets.ContainsKey(location))
            {
                var go = new GameObject($"Asset_{location.Replace('/', '_')}");
                _loadedAssets[location] = go;
            }

            var handle = new ResHandle<T>(location, _loadedAssets[location] as T, this);
            return UniTask.FromResult(handle);
        }

        public void Release(string location, Type type)
        {
            ReleaseCallCount++;
            LastReleasedLocation = location;
            LastReleasedType = type;

            if (_loadedAssets.ContainsKey(location))
            {
                UnityEngine.Object.DestroyImmediate(_loadedAssets[location]);
                _loadedAssets.Remove(location);
            }
        }

        public bool HasAsset(string location)
        {
            return _loadedAssets.ContainsKey(location);
        }

        /// <summary>
        /// 注册外部创建的资源（用于测试）
        /// </summary>
        public void RegisterAsset(string location, GameObject asset)
        {
            _loadedAssets[location] = asset;
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            System.Threading.CancellationToken token
        )
            where T : class
        {
            throw new NotImplementedException();
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            System.Threading.CancellationToken token
        )
            where T : class
        {
            throw new NotImplementedException();
        }

        public void ReleaseBatch(IEnumerable<string> locations)
        {
            throw new NotImplementedException();
        }
        public void ReleaseBatch<T>(IEnumerable<string> locations) where T : class
        {
            throw new NotImplementedException();
        }

        public UniTask UnloadUnusedAssets(System.Threading.CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void SetTimeoutSeconds(int timeoutSeconds)
        {
            throw new NotImplementedException();
        }

        public void OnInit()
        {
            // Mock implementation - do nothing
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            // Mock implementation - clean up loaded assets
            foreach (var asset in _loadedAssets.Values)
            {
                if (asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
            _loadedAssets.Clear();
        }
    }

    /// <summary>
    /// 模拟对象池服务
    /// </summary>
    public class MockPoolService : IAsakiPoolService
    {
        public async UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : class
        {
            throw new NotImplementedException();
        }

        public IAsakiPool<T> GetPool<T>(string key)
            where T : class
        {
            return null;
        }

        public bool HasPool(string key)
        {
            return false;
        }

        public bool DestroyPool(string key)
        {
            return false;
        }

        public void RegisterLowMemoryHandler() { }

        public void UnregisterLowMemoryHandler() { }

        public int PerformManualGovernance(bool force = false)
        {
            return 0;
        }

        public string GetStatisticsSummary()
        {
            return "Mock Pool Service";
        }

        public void Dispose() { }

        public void Tick(float deltaTime) { }
    }

    /// <summary>
    /// 模拟UI窗口组件 - 继承AsakiUIWindow以支持延迟释放测试
    /// </summary>
    public class MockAsakiWindow : AsakiUIWindow
    {
        private string _assetPath;

        public void Setup(string assetPath, bool isPooled, MockResourceService resourceService)
        {
            _assetPath = assetPath;
            IsPooled = isPooled;
            PoolKey = isPooled ? assetPath : null;

            if (!isPooled)
            {
                // 非池化对象加载资源
                var asset = new GameObject($"Asset_{assetPath.Replace('/', '_')}");
                // 注册资源到MockResourceService，使其能够被追踪
                resourceService.RegisterAsset(assetPath, asset);
                var resHandle = new ResHandle<GameObject>(assetPath, asset, resourceService);
                ResHandle = new AsakiUIResourceHandleAdapter(resHandle);
            }
        }

        protected override void OnRefresh(object args) { }

        protected override UniTask PlayEntryAnimation(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask PlayExitAnimation(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
    }
}
