using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp. Syntax;

namespace Asaki.CodeGen. Generators
{
    public static class AsakiTypeRegistryGenerator
    {
        public static void Execute(GeneratorExecutionContext context, List<TypeDeclarationSyntax> candidates)
        {
            if (candidates.Count == 0) return;

            if (context.Compilation.AssemblyName != null)
            {
                string assemblyName = context.Compilation.AssemblyName. Replace(".", "_").Replace("-", "_");
                string className = $"AsakiTypeRegistry_{assemblyName}";

                StringBuilder sb = new StringBuilder();
            
                sb.AppendLine("// <Auto Generated> Type Registration for Blackboard System");
                sb.AppendLine("using UnityEngine;");
                sb. AppendLine("using Asaki.Core. Blackboard. Variables;");
            
                sb.AppendLine("namespace Asaki.Generated");
                sb.AppendLine("{");
                sb.AppendLine($"    public static class {className}");
                sb.AppendLine("    {");
            
                sb.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
                sb.AppendLine("        public static void RegisterTypes()");
                sb.AppendLine("        {");

                foreach (var typeSyntax in candidates)
                {
                    var semanticModel = context.Compilation. GetSemanticModel(typeSyntax.SyntaxTree);
                    var typeSymbol = semanticModel.GetDeclaredSymbol(typeSyntax);
                    if (typeSymbol == null) continue;

                    string fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    sb.AppendLine($"            AsakiTypeBridge.Register<{fullTypeName}>();");
                }
            
                sb.AppendLine("        }");
                
                sb.AppendLine();
                sb.AppendLine("#if UNITY_EDITOR");
                sb.AppendLine("        [UnityEditor.InitializeOnLoadMethod]");
                sb.AppendLine("        static void RegisterInEditor()");
                sb.AppendLine("        {");
                sb.AppendLine("            RegisterTypes();");
                sb.AppendLine("        }");
                sb.AppendLine("#endif");
                
                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource($"{className}.g.cs", sb.ToString());
            }
        }
    }
}