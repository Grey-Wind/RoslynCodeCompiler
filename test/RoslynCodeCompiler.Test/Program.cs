using NuGet.Versioning;
using RoslynCodeCompiler;

namespace CompilerTest
{
    internal class Program
    {
        static async Task Main()
        {
            var runtimeAssembly = typeof(object).Assembly;
            Console.WriteLine($"System.Runtime Version: {runtimeAssembly.GetName().Version}");

            CodeCompiler compiler = new();

            var code = @"
using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";

            var nugetReferences = new[]
            {
                new NuGetPackageReference
                {
                    PackageId = "Newtonsoft.Json",
                    Version = NuGetVersion.Parse("13.0.1")
                }
            };

            try
            {
                await compiler.CompileAsync(code, "HelloWorld.exe", nugetReferences);
                Console.WriteLine("Compilation succeeded!");
            }
            catch (CompilationException ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
