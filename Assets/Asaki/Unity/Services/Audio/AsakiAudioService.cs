// 文件: Assets/Asaki/Unity/Services/Audio/AsakiAudioService.cs

using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.Configs;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// Audio service manager - Pure C# implementation
    /// Uses PoolV2 with prefab agent + resource loading for audio clips
    /// </summary>
    public class AsakiAudioService : IAsakiAudioService
    {
        // ==========================================================
        // 1. Dependencies & Configuration
        // ==========================================================
        private readonly AsakiAudioConfig _config;
        private readonly IAsakiPoolService _poolService;
        private readonly IAsakiResourceService _resourceService;

        public const string AGENT_POOL_KEY = "AsakiSoundAgentPool";

        // ==========================================================
        // 2. Runtime State
        // ==========================================================
        private IAsakiPool<AsakiSoundAgent> _agentPool;
        private CancellationTokenSource _serviceCts;
        private int _handleCounter;

        // Root node for hierarchy organization
        private GameObject _root;
        private Transform _rootTransform;

        // Active agent tracking (Handle -> Agent)
        private readonly Dictionary<AsakiAudioHandle, AsakiSoundAgent> _activeAgents =
            new Dictionary<AsakiAudioHandle, AsakiSoundAgent>(32);

        // ==========================================================
        // 3. Constructor
        // ==========================================================
        public AsakiAudioService(
            IAsakiPoolService poolService,
            IAsakiResourceService resourceService,
            AsakiAudioConfig config
        )
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ==========================================================
        // 4. Lifecycle Management
        // ==========================================================
        public void OnInit()
        {
            _serviceCts = new CancellationTokenSource();

            _root = new GameObject("[AsakiAudioSystem]");
            Object.DontDestroyOnLoad(_root);
            _rootTransform = _root.transform;

            _config?.InitializeLookup();

            ALog.Info("[AsakiAudioService] Service initialized (prefab agent + resource loading)");
        }

        public async UniTask OnInitAsync()
        {
            if (_config.AsakiSoundAgentPrefab == null)
            {
                ALog.Error("[AsakiAudioService] AsakiSoundAgentPrefab is null in config");
                return;
            }

            // ✅ Create pool using prefab reference for agent
            GameObjectFactory agentFactory = new GameObjectFactory(
                _config.AsakiSoundAgentPrefab,
                _rootTransform,
                false
            );

            // Create component pool
            _agentPool = await _poolService.CreatePoolAsync(
                AGENT_POOL_KEY,
                new ComponentFactoryWrapper(agentFactory),
                new AsakiPoolConfig
                {
                    InitialSize = _config.InitialPoolSize,
                    MaxSize = _config.MaxPoolSize > 0 ? _config.MaxPoolSize : 100,
                    EnableValidation = true,
                    EnableCollectionCheck = true,
                    AllowSyncCreation = false,
                },
                _serviceCts.Token
            );

            ALog.Info(
                $"[AsakiAudioService] Agent pool initialized, stats: {_agentPool.Statistics}"
            );
        }

        public void OnDispose()
        {
            ALog.Info("[AsakiAudioService] Starting disposal");

            StopAll(0f);

            if (_serviceCts != null)
            {
                _serviceCts.Cancel();
                _serviceCts.Dispose();
                _serviceCts = null;
            }

            _agentPool?.Dispose();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _rootTransform = null;
            }

            _activeAgents.Clear();

            ALog.Info("[AsakiAudioService] Disposal completed");
        }

        // ==========================================================
        // 5. Core Play Functionality
        // ==========================================================
        public AsakiAudioHandle Play(
            int assetId,
            AsakiAudioParams p = default(AsakiAudioParams),
            CancellationToken token = default(CancellationToken)
        )
        {
            // State checks
            if (_poolService == null)
            {
                ALog.Error("[AsakiAudioService] PoolService is null");
                return AsakiAudioHandle.Invalid;
            }

            if (_resourceService == null)
            {
                ALog.Error("[AsakiAudioService] ResourceService is null");
                return AsakiAudioHandle.Invalid;
            }

            if (_rootTransform == null)
            {
                ALog.Error("[AsakiAudioService] RootTransform is null");
                return AsakiAudioHandle.Invalid;
            }

            if (_agentPool == null)
            {
                ALog.Error("[AsakiAudioService] AgentPool is null, ensure OnInitAsync is called");
                return AsakiAudioHandle.Invalid;
            }

            // Get audio item from config
            if (!_config.TryGet(assetId, out AudioItem item))
            {
                ALog.Warn($"[AsakiAudioService] AudioID {assetId} not registered in config");
                return AsakiAudioHandle.Invalid;
            }

            // ✅ Use asset path for resource loading
            string path = item.AssetPath;
            if (string.IsNullOrEmpty(path))
            {
                ALog.Error($"[AsakiAudioService] AssetPath is null or empty for ID {assetId}");
                return AsakiAudioHandle.Invalid;
            }

            ALog.Info($"[AsakiAudioService] Play requested for audio: {path}");

            AsakiAudioParams baseParams = item.ToParams();
            AsakiAudioParams finalParams;
            if (p.Volume == 0 && p.Pitch == 0 && p.Priority == 0)
            {
                // 用户传入的是 default，直接使用配置参数
                finalParams = baseParams;

                // 处理随机音高
                if (item.RandomPitch)
                {
                    float randomPitch = baseParams.Pitch + UnityEngine.Random.Range(-0.1f, 0.1f);
                    finalParams = finalParams.SetPitch(randomPitch);
                }
            }
            else
            {
                // 用户传入了自定义参数，合并配置
                finalParams = new AsakiAudioParams()
                    .SetVolume(baseParams.Volume * p.Volume) // 相乘
                    .SetPitch(baseParams.Pitch * p.Pitch) // 相乘
                    .SetLoop(baseParams.IsLoop || p.IsLoop) // 逻辑或
                    .SetPriority(p.Priority > 0 ? p.Priority : baseParams.Priority) // 优先使用用户值
                    .SetSpatialBlend(p.SpatialBlend > 0 ? p.SpatialBlend : baseParams.SpatialBlend)
                    .SetPosition(p.Position != Vector3.zero ? p.Position : baseParams.Position);

                // 处理随机音高
                if (item.RandomPitch)
                {
                    float randomPitch = finalParams.Pitch + UnityEngine.Random.Range(-0.1f, 0.1f);
                    finalParams = finalParams.SetPitch(randomPitch);
                }
            }
            // Generate handle
            AsakiAudioHandle handle = new AsakiAudioHandle(
                ++_handleCounter,
                UnityEngine.Time.frameCount
            );

            ALog.Info($"[AsakiAudioService] Created handle: {handle.Id}");

            // Start async playback
            PlayInternalAsync(handle, path, finalParams, token).Forget();

            return handle;
        }

        private async UniTaskVoid PlayInternalAsync(
            AsakiAudioHandle handle,
            string clipPath,
            AsakiAudioParams p,
            CancellationToken userToken
        )
        {
            ALog.Info($"[AsakiAudioService] PlayInternalAsync started for handle: {handle.Id}");
            AsakiSoundAgent agent = null;

            try
            {
                // ✅ Get agent from pool (strongly typed)
                ALog.Info("[AsakiAudioService] Requesting agent from pool");
                agent = await _agentPool.GetAsync(_serviceCts.Token);

                if (agent == null)
                {
                    ALog.Error("[AsakiAudioService] Failed to get agent from pool");
                    return;
                }

                ALog.Info($"[AsakiAudioService] Agent obtained: {agent.GetInstanceID()}");

                // Register to active list
                _activeAgents[handle] = agent;
                ALog.Info(
                    $"[AsakiAudioService] Agent registered, active count: {_activeAgents.Count}"
                );

                // Create linked cancellation token
                CancellationToken linkedToken = CancellationTokenSource
                    .CreateLinkedTokenSource(_serviceCts.Token, userToken)
                    .Token;

                // ✅ Play audio (async load clip from resource path)
                ALog.Info($"[AsakiAudioService] Playing audio from path: {clipPath}");
                await agent.PlayAsync(clipPath, p, _resourceService, linkedToken);
                ALog.Info($"[AsakiAudioService] Playback completed for handle: {handle.Id}");
            }
            catch (OperationCanceledException)
            {
                ALog.Info($"[AsakiAudioService] Playback canceled for handle: {handle.Id}");
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiAudioService] Playback error: {ex.Message}", ex);
            }
            finally
            {
                // Cleanup: remove from active list
                _activeAgents.Remove(handle);
                ALog.Info(
                    $"[AsakiAudioService] Agent removed from active list, count: {_activeAgents.Count}"
                );

                // ✅ Return to pool
                if (agent != null)
                {
                    _agentPool.Return(agent);
                    ALog.Info(
                        $"[AsakiAudioService] Agent returned to pool: {agent.GetInstanceID()}"
                    );
                }
            }
        }

        // ==========================================================
        // 6. Control Methods
        // ==========================================================
        public void Pause(AsakiAudioHandle handle)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
            {
                agent.Pause();
                ALog.Info($"[AsakiAudioService] Audio paused: {handle.Id}");
            }
        }

        public void Resume(AsakiAudioHandle handle)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
            {
                agent.Resume();
                ALog.Info($"[AsakiAudioService] Audio resumed: {handle.Id}");
            }
        }

        public void Stop(AsakiAudioHandle handle, float fadeDuration = 0.2f)
        {
            if (_activeAgents.TryGetValue(handle, out AsakiSoundAgent agent))
            {
                agent.Stop(fadeDuration);
                _activeAgents.Remove(handle);
                ALog.Info($"[AsakiAudioService] Audio stopped: {handle.Id}");
            }
        }

        public void StopAll(float fadeDuration = 0.5f)
        {
            var agents = new List<AsakiSoundAgent>(_activeAgents.Values);
            _activeAgents.Clear();

            foreach (AsakiSoundAgent agent in agents)
            {
                if (agent != null && agent.IsPlaying)
                {
                    agent.Stop(fadeDuration);
                }
            }

            ALog.Info($"[AsakiAudioService] All audio stopped, count: {agents.Count}");
        }

        // ==========================================================
        // 7. Global Settings
        // ==========================================================
        public void SetGlobalVolume(float volume)
        {
            AudioListener.volume = volume;
            ALog.Info($"[AsakiAudioService] Global volume set to: {volume}");
        }

        public void PauseAll()
        {
            AudioListener.pause = true;
            ALog.Info("[AsakiAudioService] All audio paused globally");
        }

        public void ResumeAll()
        {
            AudioListener.pause = false;
            ALog.Info("[AsakiAudioService] All audio resumed globally");
        }

        // ==========================================================
        // 8. Per-Handle Settings
        // ==========================================================
        public void SetVolume(AsakiAudioHandle handle, float volume)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.SetVolume(volume);
        }

        public void SetPitch(AsakiAudioHandle handle, float pitch)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.SetPitch(pitch);
        }

        public void SetSpatialBlend(AsakiAudioHandle handle, float spatialBlend)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.GetComponent<AudioSource>().spatialBlend = spatialBlend;
        }

        public void SetPosition(AsakiAudioHandle handle, Vector3 position)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.SetPosition(position);
        }

        public void SetLoop(AsakiAudioHandle handle, bool isLoop)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.SetLoop(isLoop);
        }

        public void SetMuted(AsakiAudioHandle handle, bool isMuted)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.SetMuted(isMuted);
        }

        public void SetPriority(AsakiAudioHandle handle, int priority)
        {
            if (TryGetAgent(handle, out AsakiSoundAgent agent))
                agent.GetComponent<AudioSource>().priority = priority;
        }

        // ==========================================================
        // 9. Group Methods (Placeholder)
        // ==========================================================
        public void SetAudioGroup(AsakiAudioHandle handle, int groupId) { }

        public void SetGroupVolume(int groupId, float volume) { }

        public void SetGroupMuted(int groupId, bool isMuted) { }

        public void PauseGroup(int groupId) { }

        public void ResumeGroup(int groupId) { }

        public void StopGroup(int groupId, float fadeDuration = 0.2f) { }

        // ==========================================================
        // 10. Query Methods
        // ==========================================================
        public bool IsPlaying(AsakiAudioHandle handle)
        {
            return _activeAgents.ContainsKey(handle);
        }

        public bool IsPaused(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out AsakiSoundAgent agent) && agent.IsPaused;
        }

        public float GetCurrentVolume(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out AsakiSoundAgent agent)
                ? agent.GetComponent<AudioSource>().volume
                : 0f;
        }

        public float GetCurrentPitch(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out AsakiSoundAgent agent)
                ? agent.GetComponent<AudioSource>().pitch
                : 1f;
        }

        public Vector3 GetPosition(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out AsakiSoundAgent agent)
                ? agent.transform.position
                : Vector3.zero;
        }

        private bool TryGetAgent(AsakiAudioHandle handle, out AsakiSoundAgent agent)
        {
            return _activeAgents.TryGetValue(handle, out agent) && agent;
        }

        // ==========================================================
        // 11. Statistics
        // ==========================================================
        public string GetPoolStatistics()
        {
            if (_agentPool == null)
                return "[AsakiAudioService] Pool not initialized";

            return $"[AsakiAudioService] {_agentPool.Statistics}, Active: {_activeAgents.Count}";
        }
    }

    // ==========================================================
    // Helper: Component Factory Wrapper
    // ==========================================================
    internal class ComponentFactoryWrapper : IAsakiPoolObjectFactory<AsakiSoundAgent>
    {
        private readonly GameObjectFactory _goFactory;

        public ComponentFactoryWrapper(GameObjectFactory goFactory)
        {
            _goFactory = goFactory;
        }

        public async UniTask<AsakiSoundAgent> CreateAsync(
            CancellationToken token = default(CancellationToken)
        )
        {
            GameObject go = await _goFactory.CreateAsync(token);
            return ExtractAgent(go);
        }

        public AsakiSoundAgent CreateSync()
        {
            GameObject go = _goFactory.CreateSync();
            return ExtractAgent(go);
        }

        private AsakiSoundAgent ExtractAgent(GameObject go)
        {
            if (!go)
                return null;

            AsakiSoundAgent agent = go.GetComponent<AsakiSoundAgent>();
            if (agent != null)
                return agent;

            ALog.Error(
                "[AsakiPool] ComponentFactoryWrapper: AsakiSoundAgent component missing on prefab"
            );
            Object.Destroy(go);
            return null;
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
