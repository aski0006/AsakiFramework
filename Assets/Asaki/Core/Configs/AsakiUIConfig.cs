using System;
using System.Collections.Generic;
using Asaki.Core.UI;
using UnityEngine;

namespace Asaki.Core.Configs
{
    /// <summary>
    /// 用于存储用户界面（UI）相关信息的结构体。
    /// </summary>
    [Serializable]
    public struct UIInfo
    {
        /// <summary>
        /// UI的名称。
        /// </summary>
        public string Name;

        /// <summary>
        /// UI的唯一标识符。
        /// </summary>
        public int ID;

        /// <summary>
        /// UI所在的层级。
        /// </summary>
        public AsakiUILayer Layer;

        /// <summary>
        /// UI资源的路径。
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// 是否使用对象池来管理该UI。
        /// </summary>
        public bool UsePool;
    }

    /// <summary>
    /// 用于存储UI小部件模板信息的结构体。
    /// </summary>
    [Serializable]
    public struct WidgetTemplate
    {
        /// <summary>
        /// UI小部件的类型。
        /// </summary>
        public AsakiUIWidgetType Type;

        /// <summary>
        /// UI小部件的预制体。
        /// </summary>
        public GameObject Prefab;
    }

    /// <summary>
    /// Asaki用户界面配置类，用于管理UI相关的全局设置、注册表和模板。
    /// </summary>
    [Serializable]
    public class AsakiUIConfig
    {
        /// <summary>
        /// 全局设置部分，参考分辨率，默认值为(1920, 1080)。
        /// 用于UI布局的参考分辨率。
        /// </summary>
        [Header("Global Settings")]
        public Vector2 ReferenceResolution = new Vector2(1920, 1080);

        /// <summary>
        /// 全局设置部分，宽度或高度匹配比例，范围在0f到1f之间，默认值为0.5f。
        /// 用于控制UI布局在不同分辨率下的适配方式。
        /// </summary>
        [Range(0f, 1f)]
        public float MatchWidthOrHeight = 0.5f;

        /// <summary>
        /// 注册表部分，包含所有UI信息的列表。
        /// </summary>
        [Header("Registry")]
        public List<UIInfo> UIList = new List<UIInfo>();

        /// <summary>
        /// 模板部分，包含所有UI小部件模板的列表。
        /// </summary>
        [Header("Templates")]
        public List<WidgetTemplate> Templates = new List<WidgetTemplate>();

        /// <summary>
        /// 根据给定的UI小部件类型获取对应的预制体。
        /// 通过线性查找Templates列表来找到匹配的预制体。
        /// </summary>
        /// <param name="type">要查找的UI小部件类型。</param>
        /// <returns>匹配类型的预制体，如果未找到则返回null。</returns>
        public GameObject GetTemplate(AsakiUIWidgetType type)
        {
            // 简单的线性查找
            foreach (WidgetTemplate t in Templates)
            {
                if (t.Type == type)
                    return t.Prefab;
            }
            return null;
        }

        /// <summary>
        /// 运行时缓存，用于快速查找UI信息，此变量不会被序列化。
        /// </summary>
        private Dictionary<int, UIInfo> _lookup;

        /// <summary>
        /// 初始化查找表。如果查找表已存在则不执行操作。
        /// 将UIList中的信息添加到查找表中，以提高查找效率。
        /// </summary>
        public void InitializeLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<int, UIInfo>(UIList.Count);

            foreach (UIInfo item in UIList)
            {
                _lookup.TryAdd(item.ID, item);
            }
        }

        /// <summary>
        /// 根据给定的ID尝试获取对应的UI信息。
        /// 如果查找表未初始化，则先进行初始化。
        /// </summary>
        /// <param name="id">要查找的UI的ID。</param>
        /// <param name="info">找到的UI信息，如果未找到则为默认值。</param>
        /// <returns>如果找到对应的UI信息则返回true，否则返回false。</returns>
        public bool TryGet(int id, out UIInfo info)
        {
            info = default(UIInfo);
            if (_lookup == null)
                InitializeLookup();
            return _lookup != null && _lookup.TryGetValue(id, out info);
        }
    }
}
