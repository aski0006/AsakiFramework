using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    [Serializable]
    public class AssetInfo
    {
        public string guid;
        public string name;
        public string path;
        public string extension;
        public long fileSize;
        public long lastModifiedTicks; // 改用 long 存储 Ticks，JsonUtility 对 DateTime 支持不稳定
        public AssetCategory category;
        public bool isFavorite;

        // [关键修复] 运行时缓存字段必须标记为 NonSerialized
        [NonSerialized]
        public Object cachedObject;

        [NonSerialized]
        public Texture2D previewIcon;

        [NonSerialized]
        private bool _objectLoaded = false;

        // 辅助属性用于兼容旧代码
        public DateTime lastModified
        {
            get => new DateTime(lastModifiedTicks, DateTimeKind.Utc);
            set => lastModifiedTicks = value.Ticks;
        }

        public Object GetAssetObject()
        {
            if (!_objectLoaded && cachedObject == null && !string.IsNullOrEmpty(guid))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    cachedObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                }
                _objectLoaded = true;
            }
            return cachedObject;
        }

        public string GetFormattedSize()
        {
            const long kb = 1024;
            const long mb = kb * 1024;
            const long gb = mb * 1024;
            if (fileSize >= gb)
                return $"{fileSize / (float)gb:F2} GB";
            if (fileSize >= mb)
                return $"{fileSize / (float)mb:F2} MB";
            if (fileSize >= kb)
                return $"{fileSize / (float)kb:F2} KB";
            return $"{fileSize} B";
        }
    }
}
