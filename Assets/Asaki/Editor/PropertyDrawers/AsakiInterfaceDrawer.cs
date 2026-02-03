// File: Assets/Asaki/Editor/PropertyDrawers/AsakiInterfaceDrawer.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(AsakiInterfaceAttribute))]
    public class AsakiInterfaceDrawer : PropertyDrawer
    {
        // 缓存反射结果，避免每次重绘都扫描程序集
        private static readonly Dictionary<Type, List<Type>> _typeCache =
            new Dictionary<Type, List<Type>>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 如果折叠展开，计算子属性高度；否则只占一行
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1. 获取 Attribute 传入的接口类型
            if (attribute is AsakiInterfaceAttribute attr)
            {
                Type interfaceType = attr.InterfaceType;

                string currentName = "Null (Empty)";
                if (property.managedReferenceValue != null)
                {
                    Type currentType = property.managedReferenceValue.GetType();
                    currentName = currentType.Name;
                }

                // 绘制标签与 Foldout
                property.isExpanded = EditorGUI.Foldout(
                    new Rect(
                        position.x,
                        position.y,
                        position.width - 20,
                        EditorGUIUtility.singleLineHeight
                    ),
                    property.isExpanded,
                    label,
                    true
                );

                // 绘制类型选择按钮 (放在最右侧)
                Rect buttonRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth + 20,
                    position.y,
                    position.width - EditorGUIUtility.labelWidth - 20,
                    EditorGUIUtility.singleLineHeight
                );

                if (GUI.Button(buttonRect, currentName, EditorStyles.popup))
                {
                    ShowTypePopup(property, interfaceType);
                }
            }

            // 3. 如果展开且不为空，绘制对象内容的默认属性面板
            if (property.isExpanded)
            {
                if (property.managedReferenceValue != null)
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty iterator = property.Copy();
                    SerializedProperty endProperty = iterator.GetEndProperty();

                    Rect contentRect = new Rect(
                        position.x,
                        position.y + EditorGUIUtility.singleLineHeight + 2,
                        position.width,
                        position.height
                    );

                    iterator.NextVisible(true);
                    while (SerializedProperty.EqualContents(iterator, endProperty) == false)
                    {
                        float h = EditorGUI.GetPropertyHeight(iterator);
                        contentRect.height = h;
                        EditorGUI.PropertyField(contentRect, iterator, true);
                        contentRect.y += h + 2;
                        iterator.NextVisible(false);
                    }

                    EditorGUI.indentLevel--;
                }
                else
                {
                    Rect helpRect = new Rect(
                        position.x,
                        position.y + EditorGUIUtility.singleLineHeight,
                        position.width,
                        EditorGUIUtility.singleLineHeight
                    );

                    EditorGUI.HelpBox(
                        helpRect,
                        "Please select a pure C# type from the dropdown (MonoBehaviour types are excluded).",
                        MessageType.Info
                    );
                }
            }

            EditorGUI.EndProperty();
        }

        private void ShowTypePopup(SerializedProperty property, Type interfaceType)
        {
            GenericMenu menu = new GenericMenu();

            // 添加 "Null" 选项
            menu.AddItem(
                new GUIContent("None"),
                property.managedReferenceValue == null,
                () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            );

            // 获取所有实现该接口的非抽象、非 MonoBehaviour 类
            var types = GetImplementations(interfaceType);

            if (types.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("(No valid types found)"));
            }
            else
            {
                foreach (Type type in types)
                {
                    menu.AddItem(
                        new GUIContent(type.Name),
                        property.managedReferenceValue != null
                            && property.managedReferenceValue.GetType() == type,
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
                                    $"[AsakiInterfaceDrawer] Failed to create instance of {type.Name}: {ex.Message}"
                                );
                            }
                        }
                    );
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 获取接口实现类 (带缓存)
        /// <para>✅ 排除 MonoBehaviour 派生类</para>
        /// </summary>
        private List<Type> GetImplementations(Type interfaceType)
        {
            if (_typeCache.TryGetValue(interfaceType, out var cached))
            {
                return cached;
            }

            // 扫描所有程序集，排除：
            // 1. 抽象类
            // 2. MonoBehaviour 派生类
            // 3. ScriptableObject 派生类
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
                    interfaceType.IsAssignableFrom(t)
                    && // 实现目标接口
                    t.IsClass
                    && // 是类
                    !t.IsAbstract
                    && // 非抽象
                    !typeof(MonoBehaviour).IsAssignableFrom(t)
                    && // ✅ 排除 MonoBehaviour
                    !typeof(ScriptableObject).IsAssignableFrom(t) // ✅ 排除 ScriptableObject
                )
                .OrderBy(t => t.Name)
                .ToList();

            _typeCache[interfaceType] = types;
            return types;
        }

        /// <summary>
        /// 清除类型缓存（用于调试）
        /// </summary>
        [MenuItem("Asaki/Cache/Clear Type Cache", false, 200)]
        private static void ClearTypeCache()
        {
            _typeCache.Clear();
            Debug.Log("[Asaki] Type cache cleared.");
        }
    }
}
