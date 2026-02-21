using System;
using System.Collections.Generic;
using Asaki.Core.Context;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// 架构注册器，用于注册和管理不同的架构实例。
    /// </summary>
    public class ArchitectureRegister : IAsakiService, IDisposable
    {
        private Dictionary<Type, IAsakiArchitecture> _architectures =
            new Dictionary<Type, IAsakiArchitecture>();

        public void RegisterArchitecture<T>(T architecture)
            where T : IAsakiArchitecture
        {
            if (_architectures.ContainsKey(typeof(T)))
            {
                _architectures[typeof(T)] = architecture; // 覆盖已注册的架构
            }
            else
            {
                _architectures.Add(typeof(T), architecture);
            }
        }

        public void RegisterArchitecture(IAsakiArchitecture architecture)
        {
            if (_architectures.ContainsKey(architecture.GetType()))
            {
                _architectures[architecture.GetType()] = architecture; // 覆盖已注册的架构
            }
            else
            {
                _architectures.Add(architecture.GetType(), architecture);
            }
        }

        public void RemoveArchitecture<T>()
            where T : IAsakiArchitecture
        {
            _architectures.Remove(typeof(T));
        }

        public void RemoveArchitecture(IAsakiArchitecture architecture)
        {
            _architectures.Remove(architecture.GetType());
        }

        public void Dispose()
        {
            _architectures.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：获取所有已注册的Architecture类型
        /// </summary>
        public IReadOnlyDictionary<Type, IAsakiArchitecture> GetArchitecturesForEditor()
        {
            return _architectures;
        }
#endif
    }
}
