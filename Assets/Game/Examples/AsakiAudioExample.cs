using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Generated;
using System.Collections;
using UnityEngine;

namespace Game.Examples
{
	public class AsakiAudioExample : MonoBehaviour, IAsakiInit<IAsakiAudioService>, IAsakiAutoInject
	{
		// 添加一个公开的标志，用于在Inspector中查看是否被初始化
		public bool IsInitialized { get; private set; } = false;

		private IAsakiAudioService _asakiAudioService;
		
		[AsakiInject]
		public void Init(IAsakiAudioService args)
		{
			IsInitialized = true;
			ALog.Info("AsakiAudioExample initialized");
			
			_asakiAudioService = args;
			ALog.Info("Audio service obtained: " + (_asakiAudioService != null));
			
			try
			{
				var handle = _asakiAudioService.Play(AudioAssetID.Sihan_三Z_STUDIO_HOYO_MiX___DAMIDAMI);
				ALog.Info("Playing audio", handle);
				ALog.Info("Handle is valid: " + handle.IsValid);
				StartCoroutine(StopAudioAfter(handle, 30f));
			}
			catch (System.Exception e)
			{
				ALog.Error("Error playing audio: " + e.Message, e);
			}
		}

		private IEnumerator StopAudioAfter(AsakiAudioHandle handle, float time){
			yield return new WaitForSeconds(time);
			try
			{
				_asakiAudioService.Stop(handle);
				ALog.Info("Stopped audio", handle);
			}
			catch (System.Exception e)
			{
				ALog.Error("Error stopping audio: " + e.Message, e);
			}
		}
		
		// 添加一个手动测试方法，可在Inspector中调用
		[ContextMenu("Test Audio Play")]
		public void TestAudioPlay()
		{
			ALog.Info("TestAudioPlay called");
			if (_asakiAudioService != null)
			{
				var handle = _asakiAudioService.Play(AudioAssetID.Sihan_三Z_STUDIO_HOYO_MiX___DAMIDAMI);
				ALog.Info("Test playing audio", handle);
			}
			else
			{
				ALog.Error("Audio service not initialized");
			}
		}
	}
}
