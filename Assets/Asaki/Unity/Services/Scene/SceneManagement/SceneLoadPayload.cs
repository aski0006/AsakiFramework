using System;
using Asaki.Core.Scene;

namespace Asaki.Unity.Services.Scene.SceneManagement
{
    /// <summary>
    /// 场景加载参数
    /// </summary>
    [Serializable]
    public class SceneLoadPayload
    {
        /// <summary>
        /// 目标场景名称
        /// </summary>
        public string TargetSceneName { get; set; }

        /// <summary>
        /// 过渡场景名称（可选，默认使用LoadingScene）
        /// </summary>
        public string LoadingSceneName { get; set; } = "LoadingScene";

        /// <summary>
        /// 场景加载模式
        /// </summary>
        public AsakiLoadSceneMode LoadMode { get; set; } = AsakiLoadSceneMode.Single;

        /// <summary>
        /// 场景激活方式
        /// </summary>
        public AsakiSceneActivation Activation { get; set; } = AsakiSceneActivation.Immediate;

        /// <summary>
        /// 自定义数据（可用于传递额外参数）
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// 是否使用预加载
        /// </summary>
        public bool UsePreload { get; set; } = true;

        /// <summary>
        /// 加载超时时间(秒)，0表示无限制
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 创建基础场景加载参数
        /// </summary>
        public static SceneLoadPayload Create(
            string targetSceneName,
            string loadingSceneName = "LoadingScene"
        )
        {
            return new SceneLoadPayload
            {
                TargetSceneName = targetSceneName,
                LoadingSceneName = loadingSceneName,
                UsePreload = true,
            };
        }

        /// <summary>
        /// 创建不带预加载的场景加载参数
        /// </summary>
        public static SceneLoadPayload CreateWithoutPreload(string targetSceneName)
        {
            return new SceneLoadPayload { TargetSceneName = targetSceneName, UsePreload = false };
        }
    }
}
