using UnityEngine;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// 连招输入类型扩展示例
    ///
    /// 这是一个示例类，展示了如何为项目添加自定义连招输入类型。
    /// 复制此文件到项目中并修改，或创建自己的扩展类。
    /// </summary>
    public class MyProjectComboInputExtension : ComboInputExtension
    {
        public override void RegisterTypes(System.Action<ComboInputTypeDefinition> register)
        {
            // 示例：添加格斗游戏特有的输入类型

            register(
                new ComboInputTypeDefinition
                {
                    Id = "Punch",
                    DisplayName = "Punch",
                    Category = "Fighting",
                    Color = new Color(1f, 0.7f, 0.3f),
                    Priority = 50,
                }
            );

            register(
                new ComboInputTypeDefinition
                {
                    Id = "Kick",
                    DisplayName = "Kick",
                    Category = "Fighting",
                    Color = new Color(0.8f, 0.4f, 0.3f),
                    Priority = 51,
                }
            );

            register(
                new ComboInputTypeDefinition
                {
                    Id = "Throw",
                    DisplayName = "Throw",
                    Category = "Fighting",
                    Color = new Color(0.6f, 0.4f, 0.8f),
                    Priority = 52,
                }
            );

            // 示例：添加连招派生类型
            register(
                new ComboInputTypeDefinition
                {
                    Id = "SpecialCancel",
                    DisplayName = "Special Cancel",
                    Category = "Advanced",
                    Color = new Color(0.3f, 0.9f, 0.9f),
                    Priority = 100,
                }
            );

            register(
                new ComboInputTypeDefinition
                {
                    Id = "JumpCancel",
                    DisplayName = "Jump Cancel",
                    Category = "Advanced",
                    Color = new Color(0.4f, 0.9f, 0.4f),
                    Priority = 101,
                }
            );
        }
    }

    /// <summary>
    /// 使用特性标记的方式扩展输入类型（备选方案）
    /// </summary>
    public static class ComboInputTypeAttributesExample
    {
        // 通过特性标记字段来注册输入类型
        // 这种方式不需要继承 ComboInputExtension

        [ComboInputType("MagicAttack", "Magic Attack", "Magic", 60)]
        public static string MagicAttackType;

        [ComboInputType("RangedAttack", "Ranged Attack", "Fighting", 61)]
        public static string RangedAttackType;

        [ComboInputType("Summon", "Summon", "Magic", 62)]
        public static string SummonType;
    }
}
