using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Asaki.CodeGen.Generators
{
    /// <summary>
    /// Asaki 数据绑定代码生成器
    /// <para>
    /// 扫描标记了 <c>[AsakiBind]</c> 的类，识别其中的 <c>AsakiProperty&lt;T&gt;</c> 成员，
    /// 并自动生成强类型的绑定胶水代码（BindXxx 方法）和属性 ID 枚举。
    /// </para>
    /// </summary>
    public static class AsakiBindGenerator
    {
        /// <summary>
        /// 执行生成逻辑
        /// </summary>
        /// <param name="context">生成器执行上下文</param>
        /// <param name="candidates">通过语法接收器筛选出的候选类列表</param>
        public static void Execute(GeneratorExecutionContext context, List<ClassDeclarationSyntax> candidates)
        {
            var compilation = context.Compilation;
            
            var attributeSymbol = compilation.GetTypeByMetadataName("Asaki.Core.Attributes.AsakiBindAttribute");
            var asakiPropertySymbol = compilation.GetTypeByMetadataName("Asaki.Core.Reactive.AsakiProperty`1");

            foreach (var classDeclaration in candidates)
            {
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                
                var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                
                if (classSymbol == null) continue;

                var hasAttribute = classSymbol.GetAttributes().Any(ad =>
                {
                    if (ad.AttributeClass == null) return false;

                    if (attributeSymbol != null && 
                        SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeSymbol))
                    {
                        return true;
                    }

                    var name = ad.AttributeClass.Name;
                    return name == "AsakiBindAttribute" || name == "AsakiBind";
                });

                if (hasAttribute)
                {
                    var properties = ScanAsakiProperties(classSymbol, asakiPropertySymbol);

                    if (properties.Count > 0)
                    {
                        var source = GenerateZeroReflectionCode(classSymbol, properties);
                        var fileName = $"{classSymbol.Name}.AsakiBind.g.cs";
                        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
                    }
                }
            }
        }

        // -----------------------------------------------------------
        // 内部辅助结构与方法
        // -----------------------------------------------------------

        /// <summary>
        /// 描述一个可绑定的属性信息
        /// </summary>
        private struct BindableProperty
        {
            /// <summary>
            /// 属性名称 (例如 "HP")
            /// </summary>
            public string Name;
            
            /// <summary>
            /// 泛型参数类型的字符串表示 (例如 "int" 或 "UnityEngine.Vector3")
            /// </summary>
            public string InnerType;
        }

        /// <summary>
        /// 扫描类中的 AsakiProperty 成员
        /// </summary>
        /// <param name="classSymbol">目标类符号</param>
        /// <returns>找到的可绑定属性列表</returns>
        private static List<BindableProperty> ScanAsakiProperties(INamedTypeSymbol classSymbol, INamedTypeSymbol asakiPropertySymbol)
        {
            var results = new List<BindableProperty>();

            var members = classSymbol.GetMembers().Where(m => 
                !m.IsStatic && 
                m.DeclaredAccessibility == Accessibility.Public &&
                (m is IPropertySymbol || m is IFieldSymbol));

            foreach (var member in members)
            {
                ITypeSymbol typeSymbol = null;
                if (member is IPropertySymbol p) typeSymbol = p.Type;
                if (member is IFieldSymbol f) typeSymbol = f.Type;

                if (typeSymbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
                {
                    bool isAsakiProperty = false;

                    if (asakiPropertySymbol != null && 
                        SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, asakiPropertySymbol))
                    {
                        isAsakiProperty = true;
                    }
                    else if (typeSymbol.Name == "AsakiProperty")
                    {
                        isAsakiProperty = true;
                    }

                    if (isAsakiProperty)
                    {
                        var innerType = namedType.TypeArguments[0].ToDisplayString();
                        results.Add(new BindableProperty
                        {
                            Name = member.Name,
                            InnerType = innerType
                        });
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// 生成零反射绑定的 Partial Class 代码
        /// </summary>
        /// <param name="classSymbol">类符号</param>
        /// <param name="properties">属性列表</param>
        /// <returns>生成的源代码字符串</returns>
        private static string GenerateZeroReflectionCode(INamedTypeSymbol classSymbol, List<BindableProperty> properties)
        {
            var sb = new StringBuilder();
            var className = classSymbol.Name;
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var isGlobal = classSymbol.ContainingNamespace.IsGlobalNamespace;

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated by AsakiBindGenerator");
            sb.AppendLine("#pragma warning disable CS1591");
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Asaki.Core.Reactive;");
            sb.AppendLine();

            if (!isGlobal)
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public partial class {className}");
            sb.AppendLine("    {");

            // 1. PropertyId Enum: 为每个属性生成一个唯一的 ID
            sb.AppendLine($"        public enum PropertyId");
            sb.AppendLine("        {");
            foreach (var prop in properties) sb.AppendLine($"            {prop.Name},");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 2. GetProperty: 使用 switch-case 替代反射查找
            sb.AppendLine($"        public object GetProperty(PropertyId id)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (id)");
            sb.AppendLine("            {");
            foreach (var prop in properties)
                sb.AppendLine($"                case PropertyId.{prop.Name}: return this.{prop.Name};");
            sb.AppendLine("            }");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 3. Bind Methods: 生成强类型的绑定方法
            foreach (var prop in properties)
            {
                // Overload A: Action<T> (便捷模式)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// 绑定到 Action 委托 (便捷模式)");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public void Bind{prop.Name}(Action<{prop.InnerType}> action)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (this.{prop.Name} != null) this.{prop.Name}.Subscribe(action);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// 解绑到 Action 委托 (便捷模式)");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public void Unbind{prop.Name}(Action<{prop.InnerType}> action)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (this.{prop.Name} != null) this.{prop.Name}.Unsubscribe(action);");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Overload B: IAsakiObserver<T> (ZeroGC 模式)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// 绑定到 Observer 结构体 (高性能 ZeroGC 模式)");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public void Bind{prop.Name}(IAsakiObserver<{prop.InnerType}> observer)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (this.{prop.Name} != null) this.{prop.Name}.Bind(observer);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// 解绑到 Observer 结构体 (高性能 ZeroGC 模式)");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public void Unbind{prop.Name}(IAsakiObserver<{prop.InnerType}> observer)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (this.{prop.Name} != null) this.{prop.Name}.Unbind(observer);");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");

            if (!isGlobal) sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
