using Asaki.Core.Context;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Scene
{
	public interface IAsakiSceneManagerService : IAsakiService, IDisposable
	{
		string LastLoadedSceneName { get; }
		void PerBuildScene();
		UniTask<AsakiSceneResult> LoadSceneAsync(
			string sceneName,
			AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
			AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
			IAsakiSceneTransition transition = null,
			CancellationToken token = default
		);

		void ActivateScene();
	}
}
