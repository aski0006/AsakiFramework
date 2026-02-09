using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement
{
    /// <summary>
    /// 场景预加载配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "ScenePreloadDatabase", menuName = "Asaki/Scene/Scene Preload Database")]
    public class ScenePreloadDatabase : ScriptableObject
    {
        [Tooltip("所有场景预加载配置")]
        [SerializeField]
        private List<ScenePreloadConfig> _configs = new();

        private readonly Dictionary<string, ScenePreloadConfig> _configMap = new();
        private bool _isInitialized;

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _configMap.Clear();
            foreach (var config in _configs)
            {
                if (config == null || string.IsNullOrEmpty(config.TargetSceneName))
                    continue;

                _configMap[config.TargetSceneName] = config;
            }
            _isInitialized = true;
        }

        /// <summary>
        /// 获取指定场景的预加载配置
        /// </summary>
        public ScenePreloadConfig GetConfig(string sceneName)
        {
            Initialize();
            _configMap.TryGetValue(sceneName, out var config);
            return config;
        }

        /// <summary>
        /// 检查场景是否有预加载配置
        /// </summary>
        public bool HasConfig(string sceneName)
        {
            Initialize();
            return _configMap.ContainsKey(sceneName);
        }

        /// <summary>
        /// 获取所有已注册的场景名称
        /// </summary>
        public IEnumerable<string> GetRegisteredSceneNames()
        {
            Initialize();
            return _configMap.Keys.ToList();
        }

        /// <summary>
        /// 注册配置
        /// </summary>
        public void RegisterConfig(ScenePreloadConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.TargetSceneName))
                return;

            if (!_configs.Contains(config))
            {
                _configs.Add(config);
            }

            _configMap[config.TargetSceneName] = config;
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        public void UnregisterConfig(string sceneName)
        {
            var config = GetConfig(sceneName);
            if (config != null)
            {
                _configs.Remove(config);
                _configMap.Remove(sceneName);
            }
        }
    }
}
