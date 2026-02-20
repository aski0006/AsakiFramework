using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Attributes;
using Asaki.Core.Resources;
using UnityEngine;

namespace Asaki.Unity.Services.Resources.Preloader
{
    /// <summary>
    /// 预加载资源配置项
    /// <para>定义单个预加载资源的路径和类型。</para>
    /// </summary>
    [Serializable]
    public class PreloadResourceEntry
    {
        [Tooltip("资源加载路径")]
        public string Location;

        [Tooltip("资源类型")]
        [SerializeReference]
        [AsakiResourceType]
        public SerializableResourceType ResourceType = new GameObjectResourceType();

        /// <summary>
        /// 获取实际的资源类型
        /// </summary>
        public Type GetActualType() => ResourceType?.GetResourceType() ?? typeof(UnityEngine.Object);
    }

    /// <summary>
    /// 资源组配置
    /// <para>定义一组相关资源的集合，支持按组加载和释放。</para>
    /// </summary>
    [Serializable]
    public class ResourceGroup
    {
        [Tooltip("资源组名称")]
        public string GroupName = "New Group";

        [Tooltip("该组包含的资源")]
        public List<PreloadResourceEntry> Resources = new();

        /// <summary>
        /// 获取有效的资源条目（过滤空路径）
        /// </summary>
        public IEnumerable<PreloadResourceEntry> GetValidEntries()
        {
            return Resources.Where(r => !string.IsNullOrEmpty(r.Location));
        }
    }

    /// <summary>
    /// 预加载配置提供者
    /// <para>负责管理预加载资源配置数据，提供配置查询接口。</para>
    /// <para>遵循单一职责原则，仅处理配置相关逻辑。</para>
    /// </summary>
    [Serializable]
    public class PreloadConfigProvider
    {
        [SerializeField]
        private List<ResourceGroup> _resourceGroups = new();

        /// <summary>
        /// 获取所有资源组配置（只读）
        /// </summary>
        public IReadOnlyList<ResourceGroup> ResourceGroups => _resourceGroups;

        /// <summary>
        /// 资源组总数
        /// </summary>
        public int GroupCount => _resourceGroups.Count;

        /// <summary>
        /// 总资源数量（仅计算有效条目）
        /// </summary>
        public int TotalResourceCount => _resourceGroups.Sum(g => g.GetValidEntries().Count());

        /// <summary>
        /// 是否有配置需要加载的资源
        /// </summary>
        public bool HasResourcesToLoad => _resourceGroups.Any(g => g.Resources.Count > 0);

        /// <summary>
        /// 设置资源组配置
        /// </summary>
        public void SetResourceGroups(List<ResourceGroup> groups)
        {
            _resourceGroups = groups ?? new List<ResourceGroup>();
        }

        /// <summary>
        /// 添加资源组
        /// </summary>
        public void AddGroup(ResourceGroup group)
        {
            if (group != null && !_resourceGroups.Contains(group))
            {
                _resourceGroups.Add(group);
            }
        }

        /// <summary>
        /// 移除资源组
        /// </summary>
        public bool RemoveGroup(string groupName)
        {
            var group = _resourceGroups.FirstOrDefault(g => g.GroupName == groupName);
            return group != null && _resourceGroups.Remove(group);
        }

        /// <summary>
        /// 根据名称获取资源组
        /// </summary>
        public ResourceGroup GetGroup(string groupName)
        {
            return _resourceGroups.FirstOrDefault(g => g.GroupName == groupName);
        }

        /// <summary>
        /// 检查资源组是否存在
        /// </summary>
        public bool HasGroup(string groupName)
        {
            return _resourceGroups.Any(g => g.GroupName == groupName);
        }

        /// <summary>
        /// 获取指定组的所有资源路径
        /// </summary>
        public IReadOnlyList<string> GetGroupResourceLocations(string groupName)
        {
            var group = GetGroup(groupName);
            return group?.GetValidEntries().Select(r => r.Location).ToList()
                ?? new List<string>();
        }

        /// <summary>
        /// 清空所有配置
        /// </summary>
        public void Clear()
        {
            _resourceGroups.Clear();
        }
    }
}
