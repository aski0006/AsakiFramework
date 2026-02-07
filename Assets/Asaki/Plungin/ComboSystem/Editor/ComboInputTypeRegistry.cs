using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Plungin.ComboSystem.Editor
{
    /// <summary>
    /// 连招输入类型定义 - 可扩展的输入类型
    /// </summary>
    [Serializable]
    public class ComboInputTypeDefinition
    {
        /// <summary>
        /// 类型唯一标识符
        /// </summary>
        public string Id;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 类型分类（用于分组显示）
        /// </summary>
        public string Category;

        /// <summary>
        /// 显示颜色
        /// </summary>
        public Color Color = Color.white;

        /// <summary>
        /// 排序优先级（越小越靠前）
        /// </summary>
        public int Priority = 0;

        /// <summary>
        /// 图标（可选）
        /// </summary>
        public string IconPath;
    }

    /// <summary>
    /// 连招输入类型注册特性 - 用于标记自定义输入类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ComboInputTypeAttribute : Attribute
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Category { get; private set; }
        public int Priority { get; private set; }

        public ComboInputTypeAttribute(string id, string displayName, string category = "General", int priority = 0)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            Priority = priority;
        }
    }

    /// <summary>
    /// 连招输入类型注册表 - 集中管理所有输入类型
    /// 支持运行时扩展和用户自定义
    /// </summary>
    public static class ComboInputTypeRegistry
    {
        private static readonly Dictionary<string, ComboInputTypeDefinition> _definitions = new();
        private static readonly List<string> _orderedIds = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// 注册内置输入类型
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // 清空现有定义
            _definitions.Clear();
            _orderedIds.Clear();

            // 注册内置基础类型
            RegisterBuiltInTypes();

            // 扫描并注册特性标记的类型
            ScanAttributedTypes();

            // 扫描并注册项目自定义类型
            ScanProjectExtensions();

            // 加载用户自定义类型
            LoadUserDefinedTypes();

            _isInitialized = true;
        }

        /// <summary>
        /// 注册内置基础输入类型
        /// </summary>
        private static void RegisterBuiltInTypes()
        {
            Register(new ComboInputTypeDefinition
            {
                Id = "LightAttack",
                DisplayName = "Light attack",
                Category = "Basic attack",
                Color = new Color(1f, 0.8f, 0.4f),
                Priority = 0
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "HeavyAttack",
                DisplayName = "Heavy attack",
                Category = "Basic attack",
                Color = new Color(1f, 0.4f, 0.4f),
                Priority = 1
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Skill1",
                DisplayName = "Skill 1",
                Category = "Skill",
                Color = new Color(0.4f, 0.8f, 1f),
                Priority = 10
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Skill2",
                DisplayName = "Skill 2",
                Category = "Skill",
                Color = new Color(0.4f, 0.6f, 1f),
                Priority = 11
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Skill3",
                DisplayName = "Skill 3",
                Category = "Skill",
                Color = new Color(0.4f, 0.4f, 1f),
                Priority = 12
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Ultimate",
                DisplayName = "Ultimate skill",
                Category = "Skill",
                Color = new Color(0.9f, 0.4f, 1f),
                Priority = 20
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Dodge",
                DisplayName = "Dodge",
                Category = "Action",
                Color = new Color(0.4f, 1f, 0.6f),
                Priority = 30
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Block",
                DisplayName = "Block",
                Category = "Action",
                Color = new Color(0.6f, 0.6f, 0.6f),
                Priority = 31
            });

            Register(new ComboInputTypeDefinition
            {
                Id = "Parry",
                DisplayName = "Parry",
                Category = "Action",
                Color = new Color(1f, 0.9f, 0.4f),
                Priority = 32
            });
        }

        /// <summary>
        /// 扫描特性标记的输入类型
        /// </summary>
        private static void ScanAttributedTypes()
        {
            // 扫描所有程序集查找带有 ComboInputTypeAttribute 的字段
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var field in type.GetFields())
                        {
                            var attr = field.GetCustomAttributes(typeof(ComboInputTypeAttribute), false)
                                .FirstOrDefault() as ComboInputTypeAttribute;

                            if (attr != null)
                            {
                                // 检查是否已存在
                                if (_definitions.ContainsKey(attr.Id))
                                {
                                    Debug.LogWarning($"[ComboInputTypeRegistry] Input type '{attr.Id}' is already registered. Skipping attribute from {type.Name}.{field.Name}");
                                    continue;
                                }

                                Register(new ComboInputTypeDefinition
                                {
                                    Id = attr.Id,
                                    DisplayName = attr.DisplayName,
                                    Category = attr.Category,
                                    Priority = attr.Priority
                                });
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ComboInputTypeRegistry] Failed to scan assembly {assembly.FullName}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 扫描项目中的扩展定义
        /// </summary>
        private static void ScanProjectExtensions()
        {
            // 查找所有继承自 ComboInputExtension 的类
            var extensionTypes = TypeCache.GetTypesDerivedFrom<ComboInputExtension>();
            foreach (var type in extensionTypes)
            {
                if (type.IsAbstract) continue;

                try
                {
                    var extension = Activator.CreateInstance(type) as ComboInputExtension;
                    extension?.RegisterTypes(Register);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ComboInputTypeRegistry] Failed to initialize extension {type.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 从配置文件加载用户自定义类型
        /// </summary>
        private static void LoadUserDefinedTypes()
        {
            // 从EditorPrefs加载用户自定义类型
            string json = EditorPrefs.GetString("Asaki.ComboSystem.UserInputTypes", "[]");
            try
            {
                var userTypes = JsonUtility.FromJson<ComboInputTypeList>(json);
                if (userTypes?.Types != null)
                {
                    foreach (var type in userTypes.Types)
                    {
                        if (!_definitions.ContainsKey(type.Id))
                        {
                            Register(type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ComboInputTypeRegistry] Failed to load user defined types: {e.Message}");
            }
        }

        /// <summary>
        /// 保存用户自定义类型到配置文件
        /// </summary>
        public static void SaveUserDefinedTypes()
        {
            // 筛选出用户自定义的类型（非内置）
            var userTypes = _definitions.Values
                .Where(d => !IsBuiltInType(d.Id))
                .ToList();

            var list = new ComboInputTypeList { Types = userTypes };
            string json = JsonUtility.ToJson(list);
            EditorPrefs.SetString("Asaki.ComboSystem.UserInputTypes", json);
        }

        /// <summary>
        /// 注册输入类型
        /// </summary>
        public static void Register(ComboInputTypeDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Id))
            {
                Debug.LogError("[ComboInputTypeRegistry] Cannot register input type with empty Id");
                return;
            }

            if (_definitions.ContainsKey(definition.Id))
            {
                Debug.LogWarning($"[ComboInputTypeRegistry] Input type '{definition.Id}' is already registered. Overwriting.");
                _orderedIds.Remove(definition.Id);
            }

            _definitions[definition.Id] = definition;

            // 按优先级插入到有序列表
            int insertIndex = _orderedIds.FindIndex(id => _definitions[id].Priority > definition.Priority);
            if (insertIndex < 0)
                _orderedIds.Add(definition.Id);
            else
                _orderedIds.Insert(insertIndex, definition.Id);
        }

        /// <summary>
        /// 获取输入类型定义
        /// </summary>
        public static ComboInputTypeDefinition GetDefinition(string id)
        {
            EnsureInitialized();
            return _definitions.GetValueOrDefault(id);
        }

        /// <summary>
        /// 获取所有输入类型ID
        /// </summary>
        public static IReadOnlyList<string> GetAllIds()
        {
            EnsureInitialized();
            return _orderedIds;
        }

        /// <summary>
        /// 获取所有输入类型定义
        /// </summary>
        public static IReadOnlyList<ComboInputTypeDefinition> GetAllDefinitions()
        {
            EnsureInitialized();
            return _orderedIds.Select(id => _definitions[id]).ToList();
        }

        /// <summary>
        /// 按分类获取输入类型
        /// </summary>
        public static Dictionary<string, List<ComboInputTypeDefinition>> GetDefinitionsByCategory()
        {
            EnsureInitialized();
            return _definitions.Values.GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Priority).ToList());
        }

        /// <summary>
        /// 检查是否为内置类型
        /// </summary>
        public static bool IsBuiltInType(string id)
        {
            return id switch
            {
                "LightAttack" or "HeavyAttack" or "Skill1" or "Skill2" or "Skill3" or "Ultimate" or "Dodge" or "Block" or "Parry" => true,
                _ => false
            };
        }

        /// <summary>
        /// 检查类型是否存在
        /// </summary>
        public static bool HasType(string id)
        {
            EnsureInitialized();
            return _definitions.ContainsKey(id);
        }

        /// <summary>
        /// 移除用户自定义类型
        /// </summary>
        public static bool RemoveUserType(string id)
        {
            if (IsBuiltInType(id))
            {
                Debug.LogWarning($"[ComboInputTypeRegistry] Cannot remove built-in type '{id}'");
                return false;
            }

            if (_definitions.Remove(id))
            {
                _orderedIds.Remove(id);
                SaveUserDefinedTypes();
                return true;
            }

            return false;
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // 编辑器加载时初始化
            EditorApplication.delayCall += () =>
            {
                Initialize();
            };
        }
    }

    /// <summary>
    /// 用于序列化的列表包装类
    /// </summary>
    [Serializable]
    public class ComboInputTypeList
    {
        public List<ComboInputTypeDefinition> Types = new List<ComboInputTypeDefinition>();
    }

    /// <summary>
    /// 连招输入类型扩展基类 - 用于项目自定义扩展
    /// 继承此类并在项目中实现 RegisterTypes 方法来添加自定义输入类型
    /// </summary>
    public abstract class ComboInputExtension
    {
        /// <summary>
        /// 注册自定义输入类型
        /// </summary>
        /// <param name="register">注册委托</param>
        public abstract void RegisterTypes(Action<ComboInputTypeDefinition> register);
    }
}
