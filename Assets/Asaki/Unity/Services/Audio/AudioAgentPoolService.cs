using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// 音频代理池服务实现
    /// <para>负责音频代理对象的生命周期管理，支持3D音效的借出-归还机制。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    internal sealed class AudioAgentPoolService : IAudioAgentPoolService
    {
        private const string PoolKey = "AsakiSoundAgentPool";

        private readonly IAsakiPoolService _poolService;
        private readonly AsakiAudioConfig _config;
        private readonly Transform _rootTransform;

        private IAsakiPool<AsakiSoundAgent> _agentPool;

        public AudioAgentPoolService(
            IAsakiPoolService poolService,
            AsakiAudioConfig config,
            Transform rootTransform
        )
        {
            _poolService =
                poolService ?? throw new System.ArgumentNullException(nameof(poolService));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _rootTransform = rootTransform;
        }

        public async System.Threading.Tasks.Task InitializeAsync(
            CancellationToken cancellationToken
        )
        {
            if (_config.AsakiSoundAgentPrefab == null)
            {
                throw new System.InvalidOperationException(
                    "AsakiSoundAgentPrefab is not configured"
                );
            }

            var factory = new GameObjectFactory(
                _config.AsakiSoundAgentPrefab,
                _rootTransform,
                false
            );

            _agentPool = await _poolService.CreatePoolAsync(
                PoolKey,
                new AgentFactoryWrapper(factory),
                new AsakiPoolConfig
                {
                    InitialSize =
                        _config.InitialPoolSize > 0
                            ? _config.InitialPoolSize
                            : AudioConstants.DefaultInitialPoolSize,
                    MaxSize =
                        _config.MaxPoolSize > 0
                            ? _config.MaxPoolSize
                            : AudioConstants.DefaultMaxPoolSize,
                    EnableValidation = true,
                    EnableCollectionCheck = true,
                    AllowSyncCreation = false,
                },
                cancellationToken
            );
        }

        public async UniTask<IAudioAgent> BorrowAsync(
            Transform parent,
            CancellationToken cancellationToken
        )
        {
            if (_agentPool == null)
            {
                throw new System.InvalidOperationException("Agent pool is not initialized");
            }

            var agent = await _agentPool.GetAsync(cancellationToken);

            if (agent == null)
            {
                throw new System.InvalidOperationException("Failed to get agent from pool");
            }

            // 3D音效定位：如果提供了父节点，将代理挂载到该节点下
            if (parent != null)
            {
                agent.SetParent(parent);
            }

            return agent;
        }

        public void Return(IAudioAgent agent)
        {
            if (agent == null || _agentPool == null)
                return;

            var soundAgent = agent as AsakiSoundAgent;
            if (soundAgent == null)
                return;

            // 归还前恢复父节点
            soundAgent.RestoreParent();
            _agentPool.Return(soundAgent);
        }

        public void Dispose()
        {
            _agentPool?.Dispose();
            _agentPool = null;
        }

        public string GetStatistics()
        {
            return _agentPool?.Statistics?.ToString() ?? "Pool not initialized";
        }

        private sealed class AgentFactoryWrapper : IAsakiPoolObjectFactory<AsakiSoundAgent>
        {
            private readonly GameObjectFactory _gameObjectFactory;

            public AgentFactoryWrapper(GameObjectFactory gameObjectFactory)
            {
                _gameObjectFactory = gameObjectFactory;
            }

            public async UniTask<AsakiSoundAgent> CreateAsync(CancellationToken token = default)
            {
                var gameObject = await _gameObjectFactory.CreateAsync(token);
                return ExtractAgent(gameObject);
            }

            public AsakiSoundAgent CreateSync()
            {
                var gameObject = _gameObjectFactory.CreateSync();
                return ExtractAgent(gameObject);
            }

            private static AsakiSoundAgent ExtractAgent(GameObject gameObject)
            {
                if (gameObject == null)
                    return null;

                var agent = gameObject.GetComponent<AsakiSoundAgent>();
                if (agent != null)
                    return agent;

                Object.Destroy(gameObject);
                throw new System.InvalidOperationException(
                    "AsakiSoundAgent component missing on prefab"
                );
            }

            public void OnGet(AsakiSoundAgent obj)
            {
                if (obj != null && obj.gameObject != null)
                {
                    obj.gameObject.SetActive(true);
                    (obj as IAsakiPoolable)?.OnSpawn();
                }
            }

            public void OnReturn(AsakiSoundAgent obj)
            {
                if (obj != null && obj.gameObject != null)
                {
                    (obj as IAsakiPoolable)?.OnDespawn();
                    obj.gameObject.SetActive(false);
                }
            }

            public void OnDestroy(AsakiSoundAgent obj)
            {
                if (obj != null && obj.gameObject != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }

            public bool Validate(AsakiSoundAgent obj)
            {
                return obj != null && obj.gameObject != null;
            }
        }
    }
}
