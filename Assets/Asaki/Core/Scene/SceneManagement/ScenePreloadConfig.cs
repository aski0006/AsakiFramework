using System;
using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Resources;
using UnityEngine;

namespace Asaki.Core.Scene.SceneManagement
{
    /// <summary>
    /// 场景预加载资源配置项
    /// </summary>
    [Serializable]
    public class ScenePreloadResourceEntry
    {
        [Tooltip("资源加载路径")]
        public string Location;

        [Tooltip("资源类型")]
        [SerializeReference]
        [AsakiResourceType]
        public SerializableResourceType ResourceType = new GameObjectResourceType();
    }

    /// <summary>
    /// 场景预加载配置
    /// </summary>
    [CreateAssetMenu(fileName = "ScenePreloadConfig", menuName = "Asaki/Scene/Scene Preload Config")]
    public class ScenePreloadConfig : ScriptableObject
    {
        [Tooltip("目标场景名称")]
        [SerializeField]
        private string _targetSceneName;

        [Tooltip("该场景需要预加载的资源列表")]
        [SerializeField]
        private List<ScenePreloadResourceEntry> _resources = new();

        [Tooltip("预加载完成后是否自动切换到目标场景")]
        [SerializeField]
        private bool _autoTransition = true;

        [Tooltip("加载超时时间(秒)，0表示无限制")]
        [SerializeField]
        private int _timeoutSeconds = 30;

        public string TargetSceneName => _targetSceneName;
        public IReadOnlyList<ScenePreloadResourceEntry> Resources => _resources;
        public bool AutoTransition => _autoTransition;
        public int TimeoutSeconds => _timeoutSeconds;

        public void SetTargetSceneName(string sceneName)
        {
            _targetSceneName = sceneName;
        }

        public void AddResource(string location, SerializableResourceType resourceType)
        {
            _resources.Add(new ScenePreloadResourceEntry
            {
                Location = location,
                ResourceType = resourceType
            });
        }

        public void ClearResources()
        {
            _resources.Clear();
        }
    }
}
