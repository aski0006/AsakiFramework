using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Asaki.CodeGen; 
using Asaki.CodeGen.Tests; 

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 启动 Asaki CodeGen 模拟环境...");

        // 1. 分别解析语法树
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(TestSources.CoreFramework, path: "CoreFramework.cs"),
            CSharpSyntaxTree.ParseText(TestSources.UserGameCode, path: "UserGameCode.cs")
        };

        // 2. 准备引用
        var references = new List<MetadataReference>();
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
        foreach (var path in trustedAssembliesPaths)
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
        Console.WriteLine($"📚 已加载基础引用: {references.Count} 个");

        // 3. 创建编译上下文
        var compilation = CSharpCompilation.Create(
            "Asaki.GeneratedTest",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // [修改点]：即使有错也不要 return！
        // 因为 UserGameCode 依赖生成的代码，初始编译必然会缺东西。
        var compilerDiags = compilation.GetDiagnostics();
        if (compilerDiags.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            Console.ForegroundColor = ConsoleColor.Yellow; // 改为黄色警告
            Console.WriteLine("⚠️ 初始代码存在编译错误 (这是预期的，因为生成代码尚未运行):");
            // 仅打印前3个错误，避免刷屏
            foreach(var diag in compilerDiags.Where(d => d.Severity == DiagnosticSeverity.Error).Take(3))
            {
                Console.WriteLine($"   - {diag.GetMessage()}");
            }
            Console.ResetColor();
            // ❌ 删除 return;  <-- 关键！让它继续跑！
        }

        // 4. 初始化生成器
        var generator = new AsakiGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // 5. 执行
        Console.WriteLine("\n⏳ 正在运行 Generator...");
        
        // 运行生成器，并得到一个新的 Compilation (outputCompilation)
        // 这个 outputCompilation 包含了原始代码 + 生成的代码
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var genDiagnostics);

        // 6. 结果验证
        PrintDiagnostics(genDiagnostics); // 打印 Generator 内部的日志
        PrintGeneratedOutput(trees, outputCompilation);

        // 7. [新增] 验证最终编译结果
        // 如果生成器工作正常，之前缺少的 AsakiRegister 错误应该消失了
        Console.WriteLine("\n---------------- 最终编译检查 ----------------");
        var finalDiags = outputCompilation.GetDiagnostics();
        var finalErrors = finalDiags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        
        if (finalErrors.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ 最终编译依然失败 ({finalErrors.Count} Errors):");
            foreach (var diag in finalErrors) Console.WriteLine($"   - {diag.GetMessage()}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 最终编译通过！生成器成功修复了缺失的方法。");
        }
        Console.ResetColor();

        Console.ReadLine();
    }

    // --- 辅助方法保持不变 ---
    static void PrintDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        Console.WriteLine("\n---------------- 诊断信息 (Generator Logs) ----------------");
        if (diagnostics.Any())
        {
            foreach (var diag in diagnostics)
            {
                // 将 Debug 信息的灰色打印出来，方便区分
                var color = diag.Severity == DiagnosticSeverity.Error ? ConsoleColor.Red : ConsoleColor.Gray;
                Console.ForegroundColor = color;
                Console.WriteLine($"[{diag.Id}] {diag.GetMessage()}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Generator 没有产生错误或警告。");
        }
        Console.ResetColor();
    }

    static void PrintGeneratedOutput(IEnumerable<SyntaxTree> originalTrees, Compilation outputCompilation)
    {
        Console.WriteLine("\n---------------- 生成结果 (Generated Files) ----------------");
        
        var newTrees = outputCompilation.SyntaxTrees
            .Where(t => !originalTrees.Contains(t))
            .ToList();

        if (newTrees.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("⚠️ 未生成任何代码。");
        }

        foreach (var tree in newTrees)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📄 文件: {tree.FilePath ?? "Generated.cs"}");
            Console.ResetColor();
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine(new string('-', 60));
        }
    }
}