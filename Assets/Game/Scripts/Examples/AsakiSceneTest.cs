using System;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Scene;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    public class AsakiSceneTest : MonoBehaviour
    {
        public string SceneName_1 = "Scene_1";
        public string SceneName_2 = "Scene_2";
        private IAsakiSceneManagerService _asakiSceneManagerService;

        private void Start()
        {
            _asakiSceneManagerService = AsakiContext.Get<IAsakiSceneManagerService>();
        }

        [ContextMenu("LoadScene_1_Add")]
        private async UniTaskVoid LoadScene_1_Add()
        {
            var sceneResult = await _asakiSceneManagerService.LoadSceneAsync(
                SceneName_1,
                AsakiLoadSceneMode.Additive
            );
            ALog.Info(
                sceneResult.Success
                    ? $"LoadScene_1_Add Success, SceneName: {SceneName_1}"
                    : $"LoadScene_1_Add Failed, SceneName: {SceneName_1}"
            );
        }

        [ContextMenu("LoadScene_2_Single")]
        private async UniTaskVoid LoadScene_2_Single()
        {
            var sceneResult = await _asakiSceneManagerService.LoadSceneAsync(SceneName_2);
            ALog.Info(
                sceneResult.Success
                    ? $"LoadScene_2_Single Success, SceneName: {SceneName_2}"
                    : $"LoadScene_2_Single Failed, SceneName: {SceneName_2}"
            );
        }
    }
}
