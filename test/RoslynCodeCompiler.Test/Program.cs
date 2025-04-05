using DotnetCodeCompiler;

namespace CompilerTest
{
    internal class Program
    {
        static Task Main()
        {
            var compiler = new CodeCompiler();

            // 示例代码
            string code = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, Compiled Code!"");
        Console.ReadKey();
    }
}";

            string outputDirectory = @"./CompiledOutput"; // 替换为你的输出目录

            try
            {
                Directory.CreateDirectory(outputDirectory);
                compiler.code = code;
                compiler.dotnetVersion = CodeCompiler.DotnetVersion.net8;
                compiler.buildType = CodeCompiler.BuildType.Debug;
                compiler.CompileCode(outputDirectory);
                List<CodeCompiler.DotnetVersion> list = compiler.CheckLocalVersion();
                foreach (var netv in list)
                {
                    Console.WriteLine(netv);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：{ex.Message}");
            }

            Console.ReadLine();
            return Task.CompletedTask;
        }
    }
}
