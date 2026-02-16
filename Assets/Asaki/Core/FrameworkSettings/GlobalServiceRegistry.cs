using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    [Serializable]
    public class GlobalServiceEntry
    {
        [Tooltip("服务预制体（必须包含 IAsakiGlobalService 组件）")]
        public GameObject Prefab;

        [Tooltip("是否启用此服务")]
        public bool Enabled = true;

        [Tooltip("服务描述")]
        [TextArea(1, 3)]
        public string Description;

        [Tooltip("加载优先级（数值越小越先加载）")]
        [Range(0, 100)]
        public int Priority = 50;
    }

    [CreateAssetMenu(fileName = "GlobalServiceRegistry", menuName = "Asaki/GlobalServiceRegistry")]
    public class GlobalServiceRegistry : ScriptableObject
    {
        [Header("Global Service Prefabs")]
        [Tooltip("全局服务预制体列表，框架启动时按优先级顺序实例化并注册")]
        [SerializeField]
        private List<GlobalServiceEntry> _serviceEntries = new List<GlobalServiceEntry>();

        [Header("Settings")]
        [Tooltip("是否在启动时验证预制体有效性")]
        [SerializeField]
        private bool _validateOnStart = true;

        [Header("Version Control")]
        [SerializeField]
        private int _version = 1;

        [SerializeField]
        private string _lastModified;

        private static GlobalServiceRegistry _instance;

        public static GlobalServiceRegistry Instance => _instance;

        public IReadOnlyList<GlobalServiceEntry> ServiceEntries => _serviceEntries;

        public bool ValidateOnStart => _validateOnStart;

        public int Version => _version;

        private void OnEnable()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void AddServiceEntry(GlobalServiceEntry entry)
        {
            if (entry == null || entry.Prefab == null)
                return;

            _serviceEntries.Add(entry);
            SortByPriority();
            SetModified();
        }

        public void RemoveServiceEntry(int index)
        {
            if (index >= 0 && index < _serviceEntries.Count)
            {
                _serviceEntries.RemoveAt(index);
                SetModified();
            }
        }

        public void RemoveServiceEntry(GlobalServiceEntry entry)
        {
            if (_serviceEntries.Remove(entry))
            {
                SetModified();
            }
        }

        public void MoveEntry(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _serviceEntries.Count)
                return;
            if (toIndex < 0 || toIndex >= _serviceEntries.Count)
                return;

            var entry = _serviceEntries[fromIndex];
            _serviceEntries.RemoveAt(fromIndex);
            _serviceEntries.Insert(toIndex, entry);
            SetModified();
        }

        public void SortByPriority()
        {
            _serviceEntries.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public List<GlobalServiceEntry> GetEnabledEntries()
        {
            var result = new List<GlobalServiceEntry>();
            foreach (var entry in _serviceEntries)
            {
                if (entry.Enabled && entry.Prefab != null)
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        public List<GameObject> GetEnabledPrefabs()
        {
            var result = new List<GameObject>();
            foreach (var entry in _serviceEntries)
            {
                if (entry.Enabled && entry.Prefab != null)
                {
                    result.Add(entry.Prefab);
                }
            }
            return result;
        }

        public void SetModified()
        {
            _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void ClearAll()
        {
            _serviceEntries.Clear();
            SetModified();
        }

        public int IndexOf(GlobalServiceEntry entry)
        {
            return _serviceEntries.IndexOf(entry);
        }

        public int Count => _serviceEntries.Count;

        public GlobalServiceEntry this[int index] => _serviceEntries[index];
    }
}
