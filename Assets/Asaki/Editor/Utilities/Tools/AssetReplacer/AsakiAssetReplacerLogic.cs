using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.AssetReplacer
{
    /// <summary>
    /// 资产替换核心逻辑 - 遵循 Asaki 实用主义
    /// 利用 SerializedProperty 进行通用替换，不依赖具体组件类型
    /// </summary>
    public static class AsakiAssetReplacerLogic
    {
        public class ReplaceEntry
        {
            public Object TargetObject; // 被修改的组件或ScriptableObject
            public string PropertyPath; // 属性路径
            public SerializedObject SerializedObj;
            public bool IsSelected = true; // UI上是否勾选
        }

        /// <summary>
        /// 扫描引用
        /// </summary>
        public static List<ReplaceEntry> FindReferences(
            Object sourceAsset,
            Object targetAsset,
            bool searchProject
        )
        {
            var results = new List<ReplaceEntry>();

            if (sourceAsset == null)
                return results;
            if (targetAsset != null && sourceAsset.GetType() != targetAsset.GetType())
            {
                Debug.LogError(
                    $"[Asaki Replacer] 类型不匹配: 源是 {sourceAsset.GetType()}, 目标是 {targetAsset.GetType()}"
                );
                return results;
            }

            // 1. 确定搜索范围
            var objectsToScan = new List<Object>();

            if (searchProject)
            {
                // 搜索项目中的 Prefab 和 ScriptableObject
                string[] guids = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    objectsToScan.Add(AssetDatabase.LoadAssetAtPath<Object>(path));
                }
            }
            else
            {
                // 搜索当前场景
                var rootObjects = UnityEngine
                    .SceneManagement.SceneManager.GetActiveScene()
                    .GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    // 获取所有 Component
                    objectsToScan.AddRange(root.GetComponentsInChildren<Component>(true));
                }
            }

            // 2. 遍历属性
            foreach (Object obj in objectsToScan)
            {
                if (obj == null)
                    continue;

                SerializedObject so = new SerializedObject(obj);
                SerializedProperty sp = so.GetIterator();

                while (sp.NextVisible(true)) // 遍历所有可见属性
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // 核心判断：属性引用的对象是否等于源对象
                        if (sp.objectReferenceValue == sourceAsset)
                        {
                            // 排除自身（防止循环引用或误操作）
                            if (sp.objectReferenceValue == obj)
                                continue;

                            results.Add(
                                new ReplaceEntry
                                {
                                    TargetObject = obj,
                                    PropertyPath = sp.propertyPath,
                                    SerializedObj = so, // 缓存 SO 以便后续写入
                                }
                            );
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 执行替换
        /// </summary>
        public static void ExecuteReplace(List<ReplaceEntry> entries, Object targetAsset)
        {
            int count = 0;

            // 开启撤销组
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Asaki Asset Replace");

            foreach (ReplaceEntry entry in entries)
            {
                if (!entry.IsSelected)
                    continue;
                if (entry.TargetObject == null)
                    continue;

                // 更新 SerializedObject
                entry.SerializedObj.Update();
                SerializedProperty sp = entry.SerializedObj.FindProperty(entry.PropertyPath);

                if (sp != null)
                {
                    // 核心操作：替换引用
                    sp.objectReferenceValue = targetAsset;
                    entry.SerializedObj.ApplyModifiedProperties(); // 应用修改

                    // 标记脏数据 (如果是场景对象，Unity会自动处理；如果是Prefab，需要手动)
                    if (AssetDatabase.Contains(entry.TargetObject))
                    {
                        EditorUtility.SetDirty(entry.TargetObject);
                    }
                    else
                    {
                        // 场景对象
                        if (entry.TargetObject is Component comp)
                        {
                            EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                        }
                    }

                    count++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets(); // 保存项目资源

            Debug.Log($"<color=#4CAF50>[Asaki Replacer]</color> 成功替换 {count} 处引用。");
        }
    }
}
