using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateInterface
{
    [Generator]
    public class InterfaceSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Get the syntax receiver
            if (!(context.SyntaxReceiver is ClassSyntaxReceiver receiver))
                return;

            // Get the compilation
            var compilation = context.Compilation;

            // Get the GenerateInterface attribute symbol
            var attributeSymbol = compilation.GetTypeByMetadataName("GenerateInterface.GenerateInterfaceAttribute");
            if (attributeSymbol == null)
                return;

            // Process each class that has the GenerateInterface attribute
            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                
                if (classSymbol == null)
                    continue;

                // Check if the class has the GenerateInterface attribute
                var attribute = classSymbol.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));

                if (attribute == null)
                    continue;

                // Generate the interface
                var interfaceSource = GenerateInterface(classSymbol, attribute);
                if (!string.IsNullOrEmpty(interfaceSource))
                {
                    var interfaceName = GetInterfaceName(classSymbol, attribute);
                    context.AddSource($"{interfaceName}.g.cs", SourceText.From(interfaceSource, Encoding.UTF8));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver());
        }

        private string GenerateInterface(INamedTypeSymbol classSymbol, AttributeData attribute)
        {
            var interfaceName = GetInterfaceName(classSymbol, attribute);
            var interfaceNamespace = GetInterfaceNamespace(classSymbol, attribute);
            
            var stringBuilder = new StringBuilder();
            
            // Add using statements
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.Threading.Tasks;");
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine();

            // Add namespace
            stringBuilder.AppendLine($"namespace {interfaceNamespace}");
            stringBuilder.AppendLine("{");

            // Add interface declaration
            var accessibility = GetAccessibilityString(classSymbol.DeclaredAccessibility);
            stringBuilder.AppendLine($"    {accessibility} interface {interfaceName}");
            stringBuilder.AppendLine("    {");

            // Add public members
            var publicMembers = GetPublicMembers(classSymbol);
            foreach (var member in publicMembers)
            {
                var memberDeclaration = GenerateMemberDeclaration(member);
                if (!string.IsNullOrEmpty(memberDeclaration))
                {
                    stringBuilder.AppendLine($"        {memberDeclaration}");
                }
            }

            // Close interface and namespace
            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }

        private string GetInterfaceName(INamedTypeSymbol classSymbol, AttributeData attribute)
        {
            // Check if InterfaceName is specified in the attribute
            var interfaceNameArg = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "InterfaceName");

            if (!interfaceNameArg.Equals(default) && interfaceNameArg.Value.Value is string customName)
                return customName;

            // Default: "I" + class name
            return "I" + classSymbol.Name;
        }

        private string GetInterfaceNamespace(INamedTypeSymbol classSymbol, AttributeData attribute)
        {
            // Check if Namespace is specified in the attribute
            var namespaceArg = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "Namespace");

            if (!namespaceArg.Equals(default) && namespaceArg.Value.Value is string customNamespace)
                return customNamespace;

            // Default: same namespace as the class
            return classSymbol.ContainingNamespace.ToDisplayString();
        }

        private string GetAccessibilityString(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                _ => "public"
            };
        }

        private IEnumerable<ISymbol> GetPublicMembers(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetMembers()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                .Where(m => m.Kind == SymbolKind.Method || m.Kind == SymbolKind.Property)
                .Where(m => !m.IsStatic)
                .Where(m => m.Kind != SymbolKind.Method || ((IMethodSymbol)m).MethodKind != MethodKind.Constructor);
        }

        private string GenerateMemberDeclaration(ISymbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => GenerateMethodDeclaration((IMethodSymbol)member),
                SymbolKind.Property => GeneratePropertyDeclaration((IPropertySymbol)member),
                _ => string.Empty
            };
        }

        private string GenerateMethodDeclaration(IMethodSymbol method)
        {
            // Skip special methods like property getters/setters
            if (method.MethodKind != MethodKind.Ordinary)
                return string.Empty;

            var returnType = method.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", method.Parameters.Select(p => 
                $"{p.Type.ToDisplayString()} {p.Name}"));

            var typeParameters = "";
            if (method.TypeParameters.Length > 0)
            {
                typeParameters = "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">";
            }

            var constraints = "";
            if (method.TypeParameters.Length > 0)
            {
                var constraintClauses = method.TypeParameters
                    .Where(tp => tp.ConstraintTypes.Length > 0 || tp.HasReferenceTypeConstraint || tp.HasValueTypeConstraint || tp.HasUnmanagedTypeConstraint || tp.HasNotNullConstraint)
                    .Select(tp => GenerateTypeParameterConstraints(tp))
                    .Where(c => !string.IsNullOrEmpty(c));

                if (constraintClauses.Any())
                {
                    constraints = " " + string.Join(" ", constraintClauses);
                }
            }

            return $"{returnType} {method.Name}{typeParameters}({parameters}){constraints};";
        }

        private string GeneratePropertyDeclaration(IPropertySymbol property)
        {
            var propertyType = property.Type.ToDisplayString();
            var accessors = new List<string>();

            if (property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                accessors.Add("get");
            if (property.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                accessors.Add("set");

            if (accessors.Count == 0)
                return string.Empty;

            var accessorString = "{ " + string.Join("; ", accessors) + "; }";
            return $"{propertyType} {property.Name} {accessorString}";
        }

        private string GenerateTypeParameterConstraints(ITypeParameterSymbol typeParameter)
        {
            var constraints = new List<string>();

            if (typeParameter.HasReferenceTypeConstraint)
                constraints.Add("class");
            if (typeParameter.HasValueTypeConstraint)
                constraints.Add("struct");
            if (typeParameter.HasUnmanagedTypeConstraint)
                constraints.Add("unmanaged");
            if (typeParameter.HasNotNullConstraint)
                constraints.Add("notnull");

            constraints.AddRange(typeParameter.ConstraintTypes.Select(ct => ct.ToDisplayString()));

            if (typeParameter.HasConstructorConstraint)
                constraints.Add("new()");

            if (constraints.Count == 0)
                return string.Empty;

            return $"where {typeParameter.Name} : {string.Join(", ", constraints)}";
        }
    }

    internal class ClassSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Look for class declarations with attributes
            if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.AttributeLists.Count > 0)
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}