// Assets/Editor/AssetReplacementTool/Utils/PrefabUtilityExtensions.cs

using System;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Extensions
{
    public static class PrefabUtilityExtensions
    {
        /// <summary>
        /// 安全地获取Prefab实例的Override列表
        /// </summary>
        public static PropertyModification[] GetSafePropertyModifications(GameObject instance)
        {
            if (instance == null)
                return null;

            try
            {
                return PrefabUtility.GetPropertyModifications(instance);
            }
            catch
            {
                // 某些情况下会抛出异常
                return null;
            }
        }

        /// <summary>
        /// 检查是否是Prefab实例（包括嵌套）
        /// </summary>
        public static bool IsPrefabInstance(this GameObject go)
        {
            return PrefabUtility.IsPartOfPrefabInstance(go);
        }

        /// <summary>
        /// 获取Prefab的核心源（跳过变体链）
        /// </summary>
        public static GameObject GetRootSourcePrefab(GameObject prefab)
        {
            if (!PrefabUtility.IsPartOfVariantPrefab(prefab))
                return prefab;

            GameObject basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            return GetRootSourcePrefab(basePrefab as GameObject);
        }

        /// <summary>
        /// 复制组件的完整状态（包括私有字段）
        /// </summary>
        public static void CopyComponentState(Component source, Component target)
        {
            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            if (sourceType != targetType)
                return;

            // 使用序列化方式复制
            SerializedObject sourceSerialized = new SerializedObject(source);
            SerializedObject targetSerialized = new SerializedObject(target);

            SerializedProperty prop = sourceSerialized.GetIterator();
            while (prop.Next(true))
            {
                if (prop.propertyPath == "m_Script")
                    continue;

                SerializedProperty targetProp = targetSerialized.FindProperty(prop.propertyPath);
                if (targetProp != null)
                {
                    targetSerialized.CopyFromSerializedProperty(prop);
                }
            }

            targetSerialized.ApplyModifiedProperties();
        }
    }
}
