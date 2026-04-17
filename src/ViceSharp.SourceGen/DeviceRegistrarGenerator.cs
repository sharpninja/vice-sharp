using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViceSharp.SourceGen
{
    [Generator]
    public class DeviceRegistrarGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all classes annotated with ViceSharpDeviceAttribute
            var deviceClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                    transform: static (ctx, _) => GetDeviceClass(ctx))
                .Where(static m => m is not null);

            // Generate source code
            context.RegisterSourceOutput(deviceClasses, GenerateSource);
        }

        private static ClassDeclarationSyntax? GetDeviceClass(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString().Contains("ViceSharpDevice"))
                    {
                        return classDeclaration;
                    }
                }
            }

            return null;
        }

        private static void GenerateSource(SourceProductionContext context, ClassDeclarationSyntax? deviceClass)
        {
            if (deviceClass == null)
                return;

            var className = deviceClass.Identifier.Text;
            var namespaceName = GetNamespace(deviceClass);

            context.AddSource($"{className}.g.cs", $@"
namespace {namespaceName}
{{
    partial class {className}
    {{
        // Auto-generated device registration
        public static partial void Register()
        {{
            // Device registration code will be generated here
        }}
    }}
}}
");
        }

        private static string GetNamespace(SyntaxNode syntaxNode)
        {
            var current = syntaxNode.Parent;
            while (current != null)
            {
                if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                    return namespaceDeclaration.Name.ToString();
                if (current is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
                    return fileScopedNamespace.Name.ToString();
                
                current = current.Parent;
            }
            
            return "ViceSharp.Generated";
        }
    }
}