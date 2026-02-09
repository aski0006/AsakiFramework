using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Attributes;
using Asaki.Core.Resources;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.PropertyDrawers
{
    /// <summary>
    /// AsakiResourceTypeAttribute 的自定义属性绘制器。
    /// 在 Inspector 中显示为下拉菜单，可选择预定义的资源类型。
    /// </summary>
    [CustomPropertyDrawer(typeof(AsakiResourceTypeAttribute))]
    public class AsakiResourceTypeDrawer : PropertyDrawer
    {
        // 缓存所有 SerializableResourceType 子类型
        private static readonly List<Type> _resourceTypeCache = new List<Type>();
        private static bool _typeCacheInitialized = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 基础高度 + 如果是自定义类型则额外显示类型全名字段
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue is CustomResourceType)
            {
                height += EditorGUIUtility.singleLineHeight + 2;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            InitializeTypeCache();

            // 获取当前类型名称
            string currentName = "None";
            Type currentType = null;

            if (property.managedReferenceValue != null)
            {
                currentType = property.managedReferenceValue.GetType();
                if (property.managedReferenceValue is SerializableResourceType resourceType)
                {
                    currentName = resourceType.TypeName;
                }
                else
                {
                    currentName = currentType.Name;
                }
            }

            // 绘制标签
            Rect labelRect = new Rect(
                position.x,
                position.y,
                EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight
            );
            EditorGUI.LabelField(labelRect, label);

            // 绘制类型选择按钮
            Rect buttonRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight
            );

            if (GUI.Button(buttonRect, currentName, EditorStyles.popup))
            {
                ShowTypePopup(property);
            }

            // 如果是自定义类型，显示类型全名字段
            if (property.managedReferenceValue is CustomResourceType customType)
            {
                Rect customFieldRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth,
                    position.y + EditorGUIUtility.singleLineHeight + 2,
                    position.width - EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight
                );

                EditorGUI.BeginChangeCheck();
                string newTypeFullName = EditorGUI.TextField(
                    customFieldRect,
                    customType.TypeFullName
                );
                if (EditorGUI.EndChangeCheck())
                {
                    customType.TypeFullName = newTypeFullName;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }

        private void ShowTypePopup(SerializedProperty property)
        {
            GenericMenu menu = new GenericMenu();

            // 添加 "None" 选项
            menu.AddItem(
                new GUIContent("None"),
                property.managedReferenceValue == null,
                () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            );

            menu.AddSeparator("");

            // 添加所有预定义的资源类型
            foreach (Type type in _resourceTypeCache)
            {
                string displayName = GetDisplayName(type);
                bool isSelected = property.managedReferenceValue?.GetType() == type;

                menu.AddItem(
                    new GUIContent(displayName),
                    isSelected,
                    () =>
                    {
                        try
                        {
                            object newInstance = Activator.CreateInstance(type);
                            property.managedReferenceValue = newInstance;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(
                                $"[AsakiResourceTypeDrawer] Failed to create instance of {type.Name}: {ex.Message}"
                            );
                        }
                    }
                );
            }

            menu.ShowAsContext();
        }

        private void InitializeTypeCache()
        {
            if (_typeCacheInitialized)
                return;

            _resourceTypeCache.Clear();

            // 获取所有继承自 SerializableResourceType 的非抽象类
            var types = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Enumerable.Empty<Type>();
                    }
                })
                .Where(t =>
                    typeof(SerializableResourceType).IsAssignableFrom(t)
                    && t.IsClass
                    && !t.IsAbstract
                )
                .OrderBy(t => GetDisplayOrder(t))
                .ThenBy(t => t.Name);

            _resourceTypeCache.AddRange(types);
            _typeCacheInitialized = true;
        }

        private string GetDisplayName(Type type)
        {
            // 自定义类型放在最后
            if (type == typeof(CustomResourceType))
                return "Custom/Custom Type...";

            // 其他类型按类别分组
            return type.Name.Replace("ResourceType", "");
        }

        private int GetDisplayOrder(Type type)
        {
            // 定义显示顺序：常用类型在前，自定义类型在最后
            return type switch
            {
                _ when type == typeof(GameObjectResourceType) => 0,
                _ when type == typeof(SpriteResourceType) => 1,
                _ when type == typeof(Texture2DResourceType) => 2,
                _ when type == typeof(MaterialResourceType) => 3,
                _ when type == typeof(AudioClipResourceType) => 4,
                _ when type == typeof(TextAssetResourceType) => 5,
                _ when type == typeof(AnimationClipResourceType) => 6,
                _ when type == typeof(ScriptableObjectResourceType) => 7,
                _ when type == typeof(ShaderResourceType) => 8,
                _ when type == typeof(MeshResourceType) => 9,
                _ when type == typeof(CustomResourceType) => 100,
                _ => 50,
            };
        }

        /// <summary>
        /// 清除类型缓存（用于调试）
        /// </summary>
        [MenuItem("Asaki/Cache/Clear Resource Type Cache", false, 201)]
        private static void ClearTypeCache()
        {
            _resourceTypeCache.Clear();
            _typeCacheInitialized = false;
            Debug.Log("[Asaki] Resource type cache cleared.");
        }
    }
}
