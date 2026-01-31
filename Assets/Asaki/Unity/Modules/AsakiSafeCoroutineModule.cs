using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Unity.Services.SafeCoroutine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Modules
{
    /// <summary>
    /// SafeCoroutine模块 - 注册安全协程服务到AsakiContext
    /// 优先级: 150 (在Async模块之后，其他可能依赖协程的模块之前)
    /// </summary>
    [AsakiModule(150)]
    public class AsakiSafeCoroutineModule : IAsakiModule
    {
        private const string RunnerGameObjectName = "[SafeCoroutineRunner]";

        private SafeCoroutineRunner _runner;
        private GameObject _runnerGameObject;

        public void OnInit()
        {
            // 创建SafeCoroutineRunner的GameObject
            _runnerGameObject = new GameObject(RunnerGameObjectName);
            _runnerGameObject.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_runnerGameObject);

            // 添加SafeCoroutineRunner组件
            _runner = _runnerGameObject.AddComponent<SafeCoroutineRunner>();

            // 注册到AsakiContext
            AsakiContext.Register(_runner);
        }

        public UniTask OnInitAsync()
        {
            // SafeCoroutineRunner无需异步初始化
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            // 清理协程
            _runner?.StopAllSafeCoroutines();

            // 销毁GameObject
            if (_runnerGameObject != null)
            {
                Object.Destroy(_runnerGameObject);
                _runnerGameObject = null;
            }

            _runner = null;
        }
    }
}
