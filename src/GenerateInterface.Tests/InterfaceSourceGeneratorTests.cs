using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace GenerateInterface.Tests;

public class InterfaceSourceGeneratorTests
{
    [Fact]
    public void GenerateInterface_SimpleClass_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface]
                public class TestService
                {
                    public void DoSomething() { }
                    public string GetValue() => "test";
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("public interface ITestService", result);
        Assert.Contains("void DoSomething();", result);
        Assert.Contains("string GetValue();", result);
    }

    [Fact]
    public void GenerateInterface_WithAutoProperties_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface]
                public class ConfigService
                {
                    public string Value { get; set; } = string.Empty;
                    public int Count { get; } = 0;
                    public bool IsEnabled { get; set; }
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("public interface IConfigService", result);
        Assert.Contains("string Value { get; set; }", result);
        Assert.Contains("int Count { get; }", result);
        Assert.Contains("bool IsEnabled { get; set; }", result);
    }

    [Fact]
    public void GenerateInterface_WithDefaultParameters_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface]
                public class TestService
                {
                    public void Method(string message = "Hello", int count = 1, bool enabled = true) { }
                    public void MethodWithNull(string? value = null) { }
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("public interface ITestService", result);
        Assert.Contains("void Method(string message = \"Hello\", int count = 1, bool enabled = true);", result);
        Assert.Contains("void MethodWithNull(string? value = null);", result);
    }

    [Fact]
    public void GenerateInterface_WithGenerics_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface]
                public class Repository<T> where T : class
                {
                    public T? GetById<TKey>(TKey id) where TKey : notnull => null;
                    public void Save(T entity) { }
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("public interface IRepository<T>", result);
        Assert.Contains("where T : class", result);
        Assert.Contains("T? GetById<TKey>(TKey id) where TKey : notnull;", result);
        Assert.Contains("void Save(T entity);", result);
    }

    [Fact]
    public void GenerateInterface_WithCustomNameAndNamespace_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface(InterfaceName = "ICustomService", Namespace = "CustomNamespace")]
                public class TestService
                {
                    public void DoWork() { }
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("namespace CustomNamespace;", result);
        Assert.Contains("public interface ICustomService", result);
        Assert.Contains("void DoWork();", result);
    }

    [Fact]
    public void GenerateInterface_IgnoresPrivateMembers_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace
            {
                [GenerateInterface]
                public class TestService
                {
                    public void PublicMethod() { }
                    private void PrivateMethod() { }
                    protected void ProtectedMethod() { }
                    internal void InternalMethod() { }
                    public static void StaticMethod() { }
                }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("public interface ITestService", result);
        Assert.Contains("void PublicMethod();", result);
        Assert.DoesNotContain("PrivateMethod", result);
        Assert.DoesNotContain("ProtectedMethod", result);
        Assert.DoesNotContain("InternalMethod", result);
        Assert.DoesNotContain("StaticMethod", result);
    }

    [Fact]
    public void GenerateInterface_FileScoped_GeneratesInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class TestService
            {
                public void DoSomething() { }
            }
            """;

        var result = GetGeneratedOutput(source);

        Assert.Contains("namespace TestNamespace;", result);
        Assert.Contains("public interface ITestService", result);
        Assert.Contains("void DoSomething();", result);
    }

    private static string GetGeneratedOutput(string source)
    {
        var attributeSource = """
            using System;

            namespace GenerateInterface
            {
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                public sealed class GenerateInterfaceAttribute : Attribute
                {
                    public string? InterfaceName { get; set; }
                    public string? Namespace { get; set; }
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attributeSyntaxTree = CSharpSyntaxTree.ParseText(attributeSource);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree, attributeSyntaxTree },
            references: references);

        var generator = new InterfaceSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var result = driver.GetRunResult();

        if (result.GeneratedTrees.Length == 0)
            return string.Empty;

        // Find the generated interface (not the attribute)
        foreach (var tree in result.GeneratedTrees)
        {
            var text = tree.GetText().ToString();
            if (text.Contains("public interface"))
                return text;
        }

        return string.Empty;
    }
}