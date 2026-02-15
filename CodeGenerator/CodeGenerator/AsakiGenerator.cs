using Asaki.CodeGen.Generators;
using Microsoft.CodeAnalysis;

namespace Asaki.CodeGen
{
    [Generator]
    public class AsakiGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AsakiSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is AsakiSyntaxReceiver receiver))
                return;

            // Task A: Data Binding
            AsakiBindGenerator.Execute(context, receiver.CandidateClasses);

            // Task B: Message Broker
            AsakiBrokerGenerator.Execute(
                context,
                receiver.PotentialSubscribers, // IAsakiHandler 实现（支持 class 和 struct）
                receiver.ListenerMethods // [AsakiListener] 方法
            );

            // Task C: [新增] Serialization System
            // 我们即将创建这个类
            AsakiSaveGenerator.Execute(context, receiver.SaveCandidates);

            AsakiConfigRegistryGenerator.Execute(context, receiver.CandidateConfigs);

            AsakiGraphRegistryGenerator.Execute(context, receiver.GraphEditorCandidates);

            AsakiModuleRegistryGenerator.Execute(context, receiver.ModuleCandidates);

            AsakiTypeRegistryGenerator.Execute(context, receiver.SchemaCandidates);

            AsakiInjectGenerator.Execute(context, receiver.CandidateMethods);
        }
    }
}
