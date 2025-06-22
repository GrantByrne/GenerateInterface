using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;

namespace GenerateInterface.Tests;

public class InterfaceSourceGeneratorTests
{
    [Fact]
    public async Task GenerateInterface_SimpleClass_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class TestService
            {
                public void DoSomething() { }
                public string GetValue() => "test";
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ITestService
            {
                void DoSomething();
                string GetValue();
            }
            """;

        await VerifySourceGeneratorAsync(source, ("ITestService.g.cs", expectedInterface));
    }

    [Fact]
    public async Task GenerateInterface_WithAutoProperties_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class ConfigService
            {
                public string Value { get; set; } = string.Empty;
                public int Count { get; } = 0;
                public bool IsEnabled { get; set; }
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface IConfigService
            {
                string Value { get; set; }
                int Count { get; }
                bool IsEnabled { get; set; }
            }
            """;

        await VerifySourceGeneratorAsync(source, ("IConfigService.g.cs", expectedInterface));
    }

    [Fact]
    public async Task GenerateInterface_WithDefaultParameters_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class TestService
            {
                public void Method(string message = "Hello", int count = 1, bool enabled = true) { }
                public void MethodWithNull(string? value = null) { }
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ITestService
            {
                void Method(string message = "Hello", int count = 1, bool enabled = true);
                void MethodWithNull(string? value = null);
            }
            """;

        await VerifySourceGeneratorAsync(source, ("ITestService.g.cs", expectedInterface));
    }

    [Fact]
    public async Task GenerateInterface_WithGenerics_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class Repository<T> where T : class
            {
                public T? GetById<TKey>(TKey id) where TKey : notnull => null;
                public void Save(T entity) { }
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface IRepository<T>
                where T : class
            {
                T? GetById<TKey>(TKey id) where TKey : notnull;
                void Save(T entity);
            }
            """;

        await VerifySourceGeneratorAsync(source, ("IRepository.g.cs", expectedInterface));
    }

    [Fact]
    public async Task GenerateInterface_WithCustomNameAndNamespace_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface(InterfaceName = "ICustomService", Namespace = "CustomNamespace")]
            public class TestService
            {
                public void DoWork() { }
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace CustomNamespace;

            public interface ICustomService
            {
                void DoWork();
            }
            """;

        await VerifySourceGeneratorAsync(source, ("ICustomService.g.cs", expectedInterface));
    }

    [Fact]
    public async Task GenerateInterface_IgnoresPrivateMembers_GeneratesCorrectInterface()
    {
        var source = """
            using GenerateInterface;

            namespace TestNamespace;

            [GenerateInterface]
            public class TestService
            {
                public void PublicMethod() { }
                private void PrivateMethod() { }
                protected void ProtectedMethod() { }
                internal void InternalMethod() { }
                public static void StaticMethod() { }
            }
            """;

        var expectedInterface = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            public interface ITestService
            {
                void PublicMethod();
            }
            """;

        await VerifySourceGeneratorAsync(source, ("ITestService.g.cs", expectedInterface));
    }

    private static async Task VerifySourceGeneratorAsync(string source, params (string filename, string content)[] expectedGeneratedSources)
    {
        var test = new CSharpSourceGeneratorTest<InterfaceSourceGenerator, XUnitVerifier>
        {
            TestState =
            {
                Sources = { source },
                GeneratedSources =
                {
                    (typeof(InterfaceSourceGenerator), "GenerateInterfaceAttribute.cs", """
                        using System;

                        namespace GenerateInterface;

                        /// <summary>
                        /// Marks a class for automatic interface generation.
                        /// The source generator will create an interface with all public members of the annotated class.
                        /// </summary>
                        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                        public sealed class GenerateInterfaceAttribute : Attribute
                        {
                            /// <summary>
                            /// Gets or sets the name of the generated interface.
                            /// If not specified, the interface name will be "I" + class name.
                            /// </summary>
                            public string? InterfaceName { get; set; }

                            /// <summary>
                            /// Gets or sets the namespace for the generated interface.
                            /// If not specified, the interface will be generated in the same namespace as the class.
                            /// </summary>
                            public string? Namespace { get; set; }
                        }
                        """)
                }
            }
        };

        foreach (var (filename, content) in expectedGeneratedSources)
        {
            test.TestState.GeneratedSources.Add((typeof(InterfaceSourceGenerator), filename, content));
        }

        await test.RunAsync();
    }
}