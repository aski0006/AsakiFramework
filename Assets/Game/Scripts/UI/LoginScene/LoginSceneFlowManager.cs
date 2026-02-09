using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Scene;
using Asaki.Core.UI;
using Asaki.Generated;
using Asaki.Unity;
using Asaki.Unity.Bootstrapper;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.UI.LoginScene
{
    /// <summary>
    /// 登录场景流程管理器
    /// <para>负责管理登录场景的UI流程和场景切换。</para>
    /// <para>演示了AsakiMono的正确使用方式，包括：</para>
    /// <list type="bullet">
    /// <item><description>依赖注入（通过IAsakiAutoInject）</description></item>
    /// <item><description>事件处理（通过IAsakiHandler）</description></item>
    /// <item><description>场景管理（通过IAsakiSceneManagerService）</description></item>
    /// <item><description>UI管理（通过IAsakiUIService）</description></item>
    /// </list>
    /// </summary>
    public class LoginSceneFlowManager
        : AsakiMono,
            IAsakiAutoInject,
            IAsakiInit<IAsakiUIService, IAsakiSceneManagerService>,
            IAsakiHandler<UserLoginEvent>
    {
        #region Services

        private IAsakiUIService _asakiUIService;
        private IAsakiSceneManagerService _asakiSceneService;

        #endregion

        #region Initialization

        /// <summary>
        /// 依赖注入点 - 由框架自动调用
        /// </summary>
        [AsakiInject]
        public void Init(IAsakiUIService uiService, IAsakiSceneManagerService sceneService)
        {
            _asakiUIService = uiService;
            _asakiSceneService = sceneService;

            ALog.Info($"[{nameof(LoginSceneFlowManager)}] Services injected successfully.");
        }

        /// <summary>
        /// Awake阶段初始化 - 不依赖框架服务
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            // 本地初始化逻辑（如果有）
        }

        /// <summary>
        /// 框架就绪后的初始化
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();

            // 验证服务是否已注入
            if (_asakiUIService == null)
            {
                ALog.Error(
                    $"[{nameof(LoginSceneFlowManager)}] UIService not injected! "
                        + "Make sure the manager is in a scene that gets properly initialized."
                );
                return;
            }

            // 打开注册面板作为初始界面
            OpenInitialWindow().Forget();
        }

        /// <summary>
        /// 打开初始窗口
        /// </summary>
        private async UniTaskVoid OpenInitialWindow()
        {
            try
            {
                await _asakiUIService.OpenAsync<LoginPanelWindow>((int)WindowAssetId.LoginPanel);
                ALog.Info($"[{nameof(LoginSceneFlowManager)}] Initial window opened.");
            }
            catch (System.Exception ex)
            {
                ALog.Error(
                    $"[{nameof(LoginSceneFlowManager)}] Failed to open initial window: {ex}"
                );
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 组件启用时 - 注册事件监听
        /// </summary>
        protected override void EnableComponent()
        {
            base.EnableComponent();

            // 注册用户登录事件监听
            this.AsakiRegister();
        }

        /// <summary>
        /// 组件禁用时 - 注销事件监听
        /// </summary>
        protected override void DisableComponent()
        {
            base.DisableComponent();

            // 注销用户登录事件监听
            this.AsakiUnregister();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 用户登录事件处理
        /// </summary>
        public void OnEvent(UserLoginEvent e)
        {
            ALog.Info(
                $"[{nameof(LoginSceneFlowManager)}] User logged in: {e.UserData.UserNickname}"
            );

            // 切换到主场景
            LoadMainScene().Forget();
        }

        /// <summary>
        /// 异步加载主场景
        /// </summary>
        private async UniTaskVoid LoadMainScene()
        {
            if (_asakiSceneService == null)
            {
                ALog.Error($"[{nameof(LoginSceneFlowManager)}] SceneService not available.");
                return;
            }

            try
            {
                ALog.Info($"[{nameof(LoginSceneFlowManager)}] Loading MainScene...");
                _asakiUIService.Close<LoginPanelWindow>();
                await _asakiSceneService.LoadSceneWithPreloadAsync("MainScene");
                ALog.Info($"[{nameof(LoginSceneFlowManager)}] MainScene loaded successfully.");
            }
            catch (System.Exception ex)
            {
                ALog.Error($"[{nameof(LoginSceneFlowManager)}] Failed to load MainScene: {ex}");
            }
        }

        #endregion

        #region Static Factory

        /// <summary>
        /// 创建场景流程管理器实例
        /// </summary>
        public static LoginSceneFlowManager Create(GameObject host)
        {
            if (host == null)
            {
                ALog.Error(
                    "[{nameof(LoginSceneFlowManager)}] Cannot create - host GameObject is null."
                );
                return null;
            }

            var manager = host.AddComponent<LoginSceneFlowManager>();

            // 如果框架已就绪，手动触发初始化
            if (AsakiBootstrapper.IsReady)
            {
                AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(manager);
            }

            return manager;
        }

        #endregion
    }
}
