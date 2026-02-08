// File: Assets/Asaki/Tests/Resources/Mocks/MockAsakiResDependencyLookup.cs
// 模拟的依赖查询，用于测试

using System.Collections.Generic;
using Asaki.Core.Resources;

namespace Asaki.Tests.Resources.Mocks
{
    /// <summary>
    /// 模拟的依赖查询，用于测试依赖加载逻辑
    /// </summary>
    public class MockAsakiResDependencyLookup : IAsakiResDependencyLookup
    {
        private readonly Dictionary<string, List<string>> _dependencies = new();

        /// <summary>
        /// 注册资源的依赖关系
        /// </summary>
        /// <param name="location">主资源路径</param>
        /// <param name="dependencies">依赖资源路径列表</param>
        public void RegisterDependencies(string location, params string[] dependencies)
        {
            _dependencies[location] = new List<string>(dependencies);
        }

        /// <summary>
        /// 清除所有依赖关系
        /// </summary>
        public void Clear()
        {
            _dependencies.Clear();
        }

        /// <summary>
        /// 获取指定资源的依赖列表
        /// </summary>
        public IEnumerable<string> GetDependencies(string location)
        {
            if (_dependencies.TryGetValue(location, out var deps))
            {
                return deps;
            }

            return null;
        }
    }
}
