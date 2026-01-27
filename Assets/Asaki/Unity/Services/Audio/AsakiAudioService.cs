using Asaki.Core.Audio;
using Asaki.Core.Configs;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Audio
{
	/// <summary>
	/// [Pure C#] 音频服务管理器。
	/// 不继承 MonoBehaviour，由 AsakiContext 托管生命周期。
	/// </summary>
	public class AsakiAudioService : IAsakiAudioService
	{
		// ==========================================================
		// 1. 数据与依赖 (通过构造函数注入)
		// ==========================================================
		private readonly AsakiAudioConfig _config;
		private readonly string _agentAssetKey;
		private readonly int _initialPoolSize;

		public const string AGENT_POOL_KEY = "Asaki_SoundAgent";

		// ==========================================================
		// 2. 运行时状态
		// ==========================================================
		private IAsakiResourceService _resourceService;
		private IAsakiPoolService _poolService;
		private CancellationTokenSource _serviceCts;
		private int _handleCounter = 0;

		// 场景中的根节点，用于挂载所有 SoundAgent，保持 Hierarchy 整洁
		private GameObject _root;
		private Transform _rootTransform;

		// 活跃代理追踪 (Handle -> Agent)
		private readonly Dictionary<AsakiAudioHandle, AsakiSoundAgent> _activeAgents = new Dictionary<AsakiAudioHandle, AsakiSoundAgent>(32);
		/// <summary>
		/// 构造函数注入配置。
		/// 由于不再是 MonoBehaviour，无法使用 Inspector 赋值，必须由 ModuleSystem 传入。
		/// </summary>
		public AsakiAudioService(
			IAsakiResourceService resourceService,
			IAsakiPoolService poolService,
			AsakiAudioConfig config,
			string agentAssetKey = "Asaki/SoundAgent",
			int initialPoolSize = 16)
		{
			_resourceService = resourceService;
			_poolService = poolService;
			_config = config;
			_agentAssetKey = agentAssetKey;
			_initialPoolSize = initialPoolSize;
		}

		// ==========================================================
		// 3. IAsakiModule 生命周期实现
		// ==========================================================

		public void OnInit()
		{
			_serviceCts = new CancellationTokenSource();

			_root = new GameObject("[AsakiAudioSystem]");
			Object.DontDestroyOnLoad(_root);
			_rootTransform = _root.transform;

			if (_config != null) _config.InitializeLookup();
		}

		public async UniTask OnInitAsync()
		{
			await _poolService.PrewarmAsync(_agentAssetKey, _initialPoolSize);
		}

		public void OnDispose()
		{
			StopAll(0f);

			if (_serviceCts != null)
			{
				_serviceCts.Cancel();
				_serviceCts.Dispose();
				_serviceCts = null;
			}

			// 释放整个 Agent 池 (这会归还所有 Handle)
			_poolService?.ReleasePool(_agentAssetKey);

			if (_root != null)
			{
				Object.Destroy(_root);
				_root = null;
				_rootTransform = null;
			}

			_activeAgents.Clear();
		}

		// ==========================================================
		// 4. 核心功能实现
		// ==========================================================
		public AsakiAudioHandle Play(int assetId, AsakiAudioParams p = default(AsakiAudioParams), CancellationToken token = default(CancellationToken))
		{
			// 1. 状态检查
			if (_resourceService == null || _poolService == null || _rootTransform == null) return AsakiAudioHandle.Invalid;

			// 2. 获取资源路径
			if (!_config.TryGet(assetId, out AudioItem item))
			{
				ALog.Warn($"[AsakiAudio] AudioID {assetId} not registered in Config.");
				return AsakiAudioHandle.Invalid;
			}
			string path = item.AssetPath;
			if (string.IsNullOrEmpty(path)) return AsakiAudioHandle.Invalid;
			float finalVolume = item.Volume * p.Volume;
			float finalPitch = item.Pitch * p.Pitch;
			if (item.RandomPitch)
			{
				finalPitch += UnityEngine.Random.Range(-0.1f, 0.1f);
			}
			bool finalLoop = item.Loop || p.IsLoop;
			AsakiAudioParams finalParams = p
			                               .SetVolume(finalVolume)
			                               .SetPitch(finalPitch)
			                               .SetLoop(finalLoop);
			// 3. 从池中生成 Agent
			// 使用 _agentAssetKey 作为 Pool Key
			GameObject go = _poolService.Spawn(_agentAssetKey, Vector3.zero, Quaternion.identity, _rootTransform);
			if (go == null) return AsakiAudioHandle.Invalid;

			AsakiSoundAgent agent = go.GetComponent<AsakiSoundAgent>();
			if (agent == null)
			{
				_poolService.Despawn(go, _agentAssetKey);
				return AsakiAudioHandle.Invalid;
			}

			// 4. 生成 Handle 并记录
			AsakiAudioHandle handle = new AsakiAudioHandle(++_handleCounter, UnityEngine.Time.frameCount);
			_activeAgents.Add(handle, agent);

			// 5. 启动异步播放流程
			CancellationToken linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_serviceCts.Token, token).Token;

			// 这里传入 _poolService 和 _agentAssetKey，以便 Agent 播放完后能归还自己
			PlayInternal(agent, handle, path, finalParams, linkedToken, _agentAssetKey).FireAndForget(ex =>
			{
				if (ex is not OperationCanceledException)
					ALog.Error($"[AsakiAudio] Loop Error: {ex.Message}", ex);
			});

			return handle;
		}

		#if ASAKI_USE_UNITASK
		private async UniTask PlayInternal(
		#else
		private async Task PlayInternal(
			#endif
			AsakiSoundAgent agent,
			AsakiAudioHandle handle,
			string path,
			AsakiAudioParams p,
			CancellationToken token,
			string poolKey)
		{
			try
			{
				// 将 poolService 传给 Agent
				await agent.PlayAsync(path, p, _resourceService, _poolService, token, poolKey);
			}
			finally
			{
				// 无论播放是否成功，都要从活跃列表中移除
				// 注意：Agent 的 Despawn 由 PlayAsync 内部的 finally 块处理
				_activeAgents.Remove(handle);
			}
		}

		public void Pause(AsakiAudioHandle handle)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent))
			{
				agent.Pause();
			}
		}

		public void Resume(AsakiAudioHandle handle)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent))
			{
				agent.Resume();
			}
		}

		public void Stop(AsakiAudioHandle handle, float fadeDuration = 0.2f)
		{
			if (_activeAgents.TryGetValue(handle, out AsakiSoundAgent agent))
			{
				agent.Stop(fadeDuration);
				_activeAgents.Remove(handle);
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
		}

		// ==========================================================
		// 5. 其他接口 (Setters / Getters)
		// ==========================================================

		public void SetGlobalVolume(float volume)
		{
			AudioListener.volume = volume;
		}
		public void PauseAll()
		{
			AudioListener.pause = true;
		}
		public void ResumeAll()
		{
			AudioListener.pause = false;
		}

		public void SetVolume(AsakiAudioHandle handle, float volume)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent)) agent.SetVolume(volume);
		}

		public void SetPitch(AsakiAudioHandle handle, float pitch)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent)) agent.SetPitch(pitch);
		}

		public void SetSpatialBlend(AsakiAudioHandle handle, float spatialBlend)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent))
				agent.GetComponent<AudioSource>().spatialBlend = spatialBlend;
		}

		public void SetPosition(AsakiAudioHandle handle, Vector3 position)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent)) agent.SetPosition(position);
		}

		public void SetLoop(AsakiAudioHandle handle, bool isLoop)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent)) agent.SetLoop(isLoop);
		}

		public void SetMuted(AsakiAudioHandle handle, bool isMuted)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent)) agent.SetMuted(isMuted);
		}

		public void SetPriority(AsakiAudioHandle handle, int priority)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent))
				agent.GetComponent<AudioSource>().priority = priority;
		}

		// Group 接口暂留空
		public void SetAudioGroup(AsakiAudioHandle handle, int groupId) { }
		public void SetGroupVolume(int groupId, float volume) { }
		public void SetGroupMuted(int groupId, bool isMuted) { }
		public void PauseGroup(int groupId) { }
		public void ResumeGroup(int groupId) { }
		public void StopGroup(int groupId, float fadeDuration = 0.2f) { }

		// 查询接口
		public bool IsPlaying(AsakiAudioHandle handle)
		{
			return _activeAgents.ContainsKey(handle);
		}
		public bool IsPaused(AsakiAudioHandle handle)
		{
			if (TryGetAgent(handle, out AsakiSoundAgent agent))
			{
				return agent.IsPaused;
			}
			return false;
		}

		public float GetCurrentVolume(AsakiAudioHandle handle)
		{
			return TryGetAgent(handle, out AsakiSoundAgent agent) ? agent.GetComponent<AudioSource>().volume : 0f;
		}
		public float GetCurrentPitch(AsakiAudioHandle handle)
		{
			return TryGetAgent(handle, out AsakiSoundAgent agent) ? agent.GetComponent<AudioSource>().pitch : 1f;
		}
		public Vector3 GetPosition(AsakiAudioHandle handle)
		{
			return TryGetAgent(handle, out AsakiSoundAgent agent) ? agent.transform.position : Vector3.zero;
		}

		private bool TryGetAgent(AsakiAudioHandle handle, out AsakiSoundAgent agent)
		{
			return _activeAgents.TryGetValue(handle, out agent) && agent;
		}
	}
}
