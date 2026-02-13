using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Asaki.CodeGen
{
	public class AsakiSyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();
		public List<ClassDeclarationSyntax> PotentialSubscribers { get; } = new List<ClassDeclarationSyntax>();
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
			// 1. 处理 Class ([AsakiBind], IAsakiHandler)
			if (syntaxNode is ClassDeclarationSyntax classDeclaration)
			{
				if (HasAttribute(classDeclaration.AttributeLists, "AsakiBind"))
				{
					CandidateClasses.Add(classDeclaration);
				}

				if (classDeclaration.BaseList != null &&
				    classDeclaration.BaseList.Types.Any(t =>
					    t.Type.ToString().Contains("IAsakiHandler")))
				{
					PotentialSubscribers.Add(classDeclaration);
				}

				if (HasBaseType(classDeclaration, "IAsakiDataTable"))
				{
					CandidateConfigs.Add(classDeclaration);
				}

				if (HasAttribute(classDeclaration.AttributeLists, "AsakiModule"))
				{
					ModuleCandidates.Add(classDeclaration);
				}
			}

			// 2. [新增] 处理 [AsakiSave] (Class 或 Struct)
			if (syntaxNode is TypeDeclarationSyntax typeDecl)
			{
				if (HasAttribute(typeDecl.AttributeLists, "AsakiSave"))
				{
					SaveCandidates.Add(typeDecl);
				}
				if (HasAttribute(typeDecl.AttributeLists, "AsakiBlackboardValueSchema"))
				{
					SchemaCandidates.Add(typeDecl);
				}

			}


			if (syntaxNode is ClassDeclarationSyntax classDecl)
			{
				// 检查是否有 CustomGraphEditor 特性
				if (HasAttribute(classDecl.AttributeLists, "CustomGraphEditor"))
				{
					GraphEditorCandidates.Add(classDecl);
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
