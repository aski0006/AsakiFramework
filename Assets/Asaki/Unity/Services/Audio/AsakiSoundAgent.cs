using Asaki.Core.Audio;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Asaki.Unity.Services.Audio
{
	[RequireComponent(typeof(AudioSource))]
	public class AsakiSoundAgent : MonoBehaviour, IAsakiPoolable
	{
		private AudioSource _source;
		private ResHandle<AudioClip> _clipHandle;
		private CancellationTokenSource _playCts;
		private Transform _transform;

		// 状态标记
		public bool IsPlaying { get; private set; }
		private bool _isPaused;
		public bool IsPaused => _isPaused;

		private void Awake()
		{
			_source = GetComponent<AudioSource>();
			_transform = transform;
			_source.playOnAwake = false;
		}

		// ==========================================================
		// 1. 对象池生命周期
		// ==========================================================

		public void OnSpawn()
		{
			IsPlaying = true;
			_isPaused = false;
			_playCts = new CancellationTokenSource();
		}

		public void OnDespawn()
		{
			// 1. 物理停止
			if (_source != null)
			{
				if (_source.isPlaying) _source.Stop();
				_source.clip = null;
			}

			// 2. 取消令牌清理
			if (_playCts != null)
			{
				_playCts.Cancel();
				_playCts.Dispose();
				_playCts = null;
			}

			// 3. 资源释放 (ResHandle Dispose -> Ref Count --)
			if (_clipHandle != null && _clipHandle.IsValid)
			{
				_clipHandle.Dispose();
				_clipHandle = null;
			}

			IsPlaying = false;
			_isPaused = false;
		}

		// ==========================================================
		// 2. 核心播放逻辑 (PlayAsync)
		// ==========================================================

		// 根据宏定义切换返回类型，保持强类型检查
		#if ASAKI_USE_UNITASK
		public async UniTask PlayAsync(
			#else
        public async UniTask PlayAsync(
			#endif
			string resourcePath,
			AsakiAudioParams p,
			IAsakiResourceService resourceService,
			IAsakiPoolService poolService, // [注入] 新版对象池服务
			CancellationToken serviceToken,
			string poolKey)
		{
			CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
				serviceToken,
				_playCts.Token,
				this.GetCancellationTokenOnDestroy()
			);

			try
			{
				// [Step 1] 异步加载音频资源 (利用 IAsakiResourceService 策略屏蔽路径差异)
				_clipHandle = await resourceService.LoadAsync<AudioClip>(resourcePath, linkedCts.Token);

				if (linkedCts.IsCancellationRequested) return;

				// 检查资源有效性 (Handle.IsValid && Asset != null)
				if (_clipHandle == null || !_clipHandle.IsValid)
				{
					ALog.Warn($"[AsakiAudio] Failed to load clip: {resourcePath}");
					return;
				}

				// [Step 2] 应用参数配置
				_transform.position = p.Position;
				_source.clip = _clipHandle.Asset; // 从 Handle 获取 Asset
				_source.volume = p.Volume;
				_source.pitch = p.Pitch;
				_source.spatialBlend = p.SpatialBlend;
				_source.loop = p.IsLoop;
				_source.priority = p.Priority;
				_source.mute = false;

				// [Step 3] 开始播放
				_source.Play();

				// [Step 4] 等待播放结束
				if (p.IsLoop)
				{
					// 循环模式：挂起直到被取消
					await AsakiAudioAsyncBridge.WaitUntilCanceled(linkedCts.Token);
				}
				else
				{
					// 单次模式：轮询播放状态
					while (_source.isPlaying || _isPaused)
					{
						if (linkedCts.IsCancellationRequested) break;
						await AsakiAudioAsyncBridge.Yield(); // 分帧等待
						if (_source == null) break;          // 防销毁
					}
				}
			}
			catch (OperationCanceledException)
			{ /* 正常取消 */
			}
			catch (Exception e)
			{
				ALog.Error($"[AsakiAudio] Play error: {e.Message}", e);
			}
			finally
			{
				// [Step 5] 播放结束，归还对象池
				linkedCts.Dispose();

				// 只有当 Unity 对象还存活且确实在播放状态时，才归还
				// 避免在场景卸载时重复 Despawn
				if (this && IsPlaying)
				{
					// 使用注入的 poolService 归还，不再依赖静态单例
					poolService.Despawn(gameObject, poolKey);
				}
			}
		}

		// ==========================================================
		// 3. 暂停与恢复 (新增)
		// ==========================================================

		public void Pause()
		{
			if (!IsPlaying || _isPaused) return; // 没在播，或者已经暂停了
			if (_source != null)
			{
				_source.Pause();
				_isPaused = true;
			}
		}

		public void Resume()
		{
			if (!IsPlaying || !_isPaused) return; // 没在播，或者本来就是播放状态
			if (_source != null)
			{
				_source.UnPause();
				_isPaused = false;
			}
		}

		// ==========================================================
		// 3. 动态控制 (Stop & Fade)
		// ==========================================================

		public void Stop(float fadeDuration)
		{
			if (!IsPlaying) return;

			// [Safe Call] 调用异步方法，并使用扩展方法 FireAndForget 处理异常
			// 此时 FadeOutAndStop 返回具体的 Task/UniTask，Bridge 中的重载会生效
			FadeOutAndStop(fadeDuration).Forget(ex =>
			{
				// 忽略 OperationCanceledException，那是我们自己取消的
				if (ex is not OperationCanceledException)
				{
					ALog.Error($"[AsakiAudio] FadeOut error: {ex.Message}", ex);
				}
			});
		}

		private async UniTask FadeOutAndStop(float duration)
		{
			if (_source == null) return;

			// 如果处于暂停状态，先恢复以便进行淡出效果，或者直接停止
			if (_isPaused)
			{
				_source.UnPause();
				_isPaused = false;
			}

			float startVol = _source.volume;
			float timer = 0f;
			while (timer < duration)
			{
				timer += UnityEngine.Time.unscaledDeltaTime;
				_source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
				await AsakiAudioAsyncBridge.Yield();
				if (!IsPlaying || _source == null) return;
			}
			_playCts?.Cancel();
		}

		// ==========================================================
		// 4. Setter API
		// ==========================================================
		public void SetVolume(float vol)
		{
			if (_source) _source.volume = vol;
		}
		public void SetPitch(float pitch)
		{
			if (_source) _source.pitch = pitch;
		}
		public void SetPosition(Vector3 pos)
		{
			if (_transform) _transform.position = pos;
		}
		public void SetLoop(bool loop)
		{
			if (_source) _source.loop = loop;
		}
		public void SetMuted(bool muted)
		{
			if (_source) _source.mute = muted;
		}
	}
}
