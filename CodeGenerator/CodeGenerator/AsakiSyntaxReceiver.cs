using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Asaki.CodeGen
{
	public class AsakiSyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();
		public List<TypeDeclarationSyntax> PotentialSubscribers { get; } = new List<TypeDeclarationSyntax>();
		public List<TypeDeclarationSyntax> SaveCandidates { get; } = new List<TypeDeclarationSyntax>();
		public List<MethodDeclarationSyntax> ListenerMethods { get; } = new List<MethodDeclarationSyntax>();
		public List<ClassDeclarationSyntax> CandidateConfigs { get; } = new List<ClassDeclarationSyntax>();

		public List<ClassDeclarationSyntax> GraphEditorCandidates { get; } = new List<ClassDeclarationSyntax>();

		public List<ClassDeclarationSyntax> ModuleCandidates { get; } = new List<ClassDeclarationSyntax>();

		public List<TypeDeclarationSyntax> NetMessageCandidates { get; } = new List<TypeDeclarationSyntax>();

		public List<TypeDeclarationSyntax> SchemaCandidates { get; } = new List<TypeDeclarationSyntax>();

		public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			// 1. 处理 TypeDeclaration (Class, Struct, etc.)
			if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
			{
				if (typeDeclaration is ClassDeclarationSyntax classDecl)
				{
					if (HasAttribute(classDecl.AttributeLists, "AsakiBind"))
					{
						CandidateClasses.Add(classDecl);
					}

					if (HasBaseType(classDecl, "IAsakiDataTable"))
					{
						CandidateConfigs.Add(classDecl);
					}

					if (HasAttribute(classDecl.AttributeLists, "AsakiModule"))
					{
						ModuleCandidates.Add(classDecl);
					}

					// 检查是否有 CustomGraphEditor 特性
					if (HasAttribute(classDecl.AttributeLists, "CustomGraphEditor"))
					{
						GraphEditorCandidates.Add(classDecl);
					}
				}

				// 检查是否实现了 IAsakiHandler（支持 class 和 struct）
				if (typeDeclaration.BaseList != null &&
				    typeDeclaration.BaseList.Types.Any(t =>
					    t.Type.ToString().Contains("IAsakiHandler")))
				{
					PotentialSubscribers.Add(typeDeclaration);
				}

				// 处理 [AsakiSave] 和 [AsakiBlackboardValueSchema]
				if (HasAttribute(typeDeclaration.AttributeLists, "AsakiSave"))
				{
					SaveCandidates.Add(typeDeclaration);
				}
				if (HasAttribute(typeDeclaration.AttributeLists, "AsakiBlackboardValueSchema"))
				{
					SchemaCandidates.Add(typeDeclaration);
				}
			}


			if (syntaxNode is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration)
			{
				if (HasAttribute(methodDeclaration.AttributeLists, "AsakiListener"))
				{
					ListenerMethods.Add(methodDeclaration);
				}
				
				if (methodDeclaration.AttributeLists.Count > 0)
				{
					CandidateMethods.Add(methodDeclaration);
				}
			}
		}

		private bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
		{
			return attributeLists.SelectMany(list => list.Attributes)
			                     .Any(attr => attr.Name.ToString().Contains(attributeName));
		}

		private bool HasBaseType(ClassDeclarationSyntax classDecl, string typeName)
		{
			if (classDecl.BaseList == null) return false;

			return classDecl.BaseList.Types.Any(t => t.Type.ToString().EndsWith(typeName));
		}
	}
}
