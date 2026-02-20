using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources.Preloader
{
    /// <summary>
    /// 预加载资源注册表
    /// <para>负责管理已加载资源的句柄，提供资源访问和释放功能。</para>
    /// <para>遵循单一职责原则，仅处理资源存储和访问相关逻辑。</para>
    /// </summary>
    public class PreloadResourceRegistry : IDisposable
    {
        private readonly List<ResHandle<Object>> _loadedHandles = new();
        private readonly Dictionary<(string Location, Type Type), ResHandle<Object>> _resourceMap =
            new();
        private readonly Dictionary<string, List<ResHandle<Object>>> _groupHandlesMap = new();

        /// <summary>
        /// 已加载的资源数量
        /// </summary>
        public int LoadedResourceCount => _loadedHandles.Count;

        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 注册已加载的资源句柄
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <param name="handle">资源句柄</param>
        /// <param name="groupName">所属资源组名称（可选）</param>
        public void Register(
            string location,
            Type type,
            ResHandle<Object> handle,
            string groupName = null
        )
        {
            if (IsDisposed || handle == null)
                return;

            var key = (location, type);
            _resourceMap[key] = handle;
            _loadedHandles.Add(handle);

            if (!string.IsNullOrEmpty(groupName))
            {
                if (!_groupHandlesMap.TryGetValue(groupName, out var groupHandles))
                {
                    groupHandles = new List<ResHandle<Object>>();
                    _groupHandlesMap[groupName] = groupHandles;
                }
                groupHandles.Add(handle);
            }
        }

        /// <summary>
        /// 获取已加载的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源路径</param>
        /// <returns>资源实例，如果未找到则返回null</returns>
        public T GetResource<T>(string location)
            where T : class
        {
            ThrowIfDisposed();

            var key = (location, typeof(T));
            if (!_resourceMap.TryGetValue(key, out var handle))
            {
                return null;
            }

            return handle.IsValid ? handle.Asset as T : null;
        }

        /// <summary>
        /// 尝试获取已加载的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源路径</param>
        /// <param name="resource">输出资源实例</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetResource<T>(string location, out T resource)
            where T : class
        {
            ThrowIfDisposed();

            var key = (location, typeof(T));
            if (_resourceMap.TryGetValue(key, out var handle) && handle.IsValid)
            {
                resource = handle.Asset as T;
                return resource != null;
            }

            resource = null;
            return false;
        }

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型（可选，默认检查任意类型）</param>
        /// <returns>是否已加载</returns>
        public bool IsResourceLoaded(string location, Type type = null)
        {
            ThrowIfDisposed();

            if (type != null)
            {
                var key = (location, type);
                return _resourceMap.TryGetValue(key, out var handle) && handle.IsValid;
            }

            foreach (var kvp in _resourceMap)
            {
                if (kvp.Key.Location == location && kvp.Value.IsValid)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取指定组的所有资源句柄
        /// </summary>
        public IReadOnlyList<ResHandle<Object>> GetGroupHandles(string groupName)
        {
            ThrowIfDisposed();

            return _groupHandlesMap.TryGetValue(groupName, out var handles)
                ? handles.AsReadOnly()
                : new List<ResHandle<Object>>().AsReadOnly();
        }

        /// <summary>
        /// 释放指定的资源
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型（可选，默认释放所有匹配的资源）</param>
        public void ReleaseResource(string location, Type type = null)
        {
            ThrowIfDisposed();

            if (type != null)
            {
                ReleaseSingleResource(location, type);
            }
            else
            {
                ReleaseAllMatchingResources(location);
            }
        }

        private void ReleaseSingleResource(string location, Type type)
        {
            var key = (location, type);
            if (!_resourceMap.TryGetValue(key, out var handle))
                return;

            RemoveHandleFromAllCollections(handle, key);
            handle.Dispose();
        }

        private void ReleaseAllMatchingResources(string location)
        {
            var keysToRemove = _resourceMap
                .Where(kvp => kvp.Key.Location == location)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_resourceMap.TryGetValue(key, out var handle))
                {
                    RemoveHandleFromAllCollections(handle, key);
                    handle.Dispose();
                }
            }
        }

        /// <summary>
        /// 释放指定资源组的所有资源
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        public void ReleaseGroup(string groupName)
        {
            ThrowIfDisposed();

            if (!_groupHandlesMap.TryGetValue(groupName, out var handles))
                return;

            foreach (var handle in handles.ToList())
            {
                var key = (handle.Location, handle.Asset?.GetType() ?? typeof(Object));
                _resourceMap.Remove(key);
                _loadedHandles.Remove(handle);
                handle.Dispose();
            }

            _groupHandlesMap.Remove(groupName);
        }

        /// <summary>
        /// 释放所有持有的资源
        /// </summary>
        public void ReleaseAllResources()
        {
            foreach (var handle in _loadedHandles)
            {
                handle.Dispose();
            }

            _loadedHandles.Clear();
            _resourceMap.Clear();
            _groupHandlesMap.Clear();
        }

        /// <summary>
        /// 释放资源并清空注册表
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            ReleaseAllResources();
            IsDisposed = true;
        }

        private void RemoveHandleFromAllCollections(ResHandle<Object> handle, (string, Type) key)
        {
            _resourceMap.Remove(key);
            _loadedHandles.Remove(handle);

            foreach (var groupList in _groupHandlesMap.Values)
            {
                groupList.Remove(handle);
            }
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(PreloadResourceRegistry));
        }
    }
}
