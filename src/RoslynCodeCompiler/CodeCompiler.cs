using System.Diagnostics;

namespace DotnetCodeCompiler
{
    /// <summary>
    /// Compile by calling the command line .NET project
    /// </summary>
    public class CodeCompiler
    {
        #region private fields
        private readonly string net5ProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        private readonly string net6ProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        private readonly string net7ProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        private readonly string net8ProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        private readonly string net9ProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        #endregion

        #region enum
        /// <summary>
        /// Select the version of .NET
        /// </summary>
        public enum DotnetVersion
        {
            /// <summary>
            /// Auto get the recommand .NET version and select and build
            /// </summary>
            auto,
            /// <summary>
            /// .NET 5.0
            /// </summary>
            net5,
            /// <summary>
            /// .NET 6.0
            /// </summary>
            net6,
            /// <summary>
            /// .NET 7.0
            /// </summary>
            net7,
            /// <summary>
            /// .NET 8.0
            /// </summary>
            net8,
            /// <summary>
            /// .NET 8.0
            /// </summary>
            net9,
        }

        /// <summary>
        /// Select the build mode of .NET
        /// </summary>
        public enum BuildType
        {
            /// <summary>
            /// Release
            /// </summary>
            Release,
            /// <summary>
            /// Debug
            /// </summary>
            Debug,
        }
        #endregion

        #region public fields
        /// <summary>
        /// Select the one to use when building .NET version
        /// </summary>
        public DotnetVersion dotnetVersion { get; set; } = DotnetVersion.net6;

        /// <summary>
        /// Select the build mode
        /// </summary>
        public BuildType buildType {  get; set; } = BuildType.Release;

        /// <summary>
        /// Code to compile
        /// </summary>
        public string Code {  get; set; }
        #endregion

        #region public method
        /// <summary>
        /// Start compiling code
        /// </summary>
        /// <param name="outputPath">Output path</param>
        /// <exception cref="Exception">Compilation failure</exception>
        public void CompileCode(string outputPath)
        {
            // 创建临时目录
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 创建项目文件
                string projectFile = Path.Combine(tempDir, "TempProject.csproj");

                switch (dotnetVersion)
                {
                    case DotnetVersion.net5:
                        File.WriteAllText(projectFile, net5ProjectContent);
                        break;
                    case DotnetVersion.net6:
                        File.WriteAllText(projectFile, net6ProjectContent);
                        break;
                    case DotnetVersion.net7:
                        File.WriteAllText(projectFile, net7ProjectContent);
                        break;
                    case DotnetVersion.net8:
                        File.WriteAllText(projectFile, net8ProjectContent);
                        break;
                    case DotnetVersion.net9:
                        File.WriteAllText(projectFile, net9ProjectContent);
                        break;
                    default:
                        File.WriteAllText(projectFile, net6ProjectContent);
                        break;
                }

                // 创建源代码文件
                string sourceFile = Path.Combine(tempDir, "Program.cs");
                File.WriteAllText(sourceFile, Code);

                // 执行编译命令
                if (buildType == BuildType.Release)
                {
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"build -c Release -o {Path.Combine(tempDir, "output")}",
                            WorkingDirectory = tempDir,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"编译失败：\n{output}\n{error}");
                    }

                    // 复制生成的文件
                    string outputDir = Path.Combine(tempDir, "output");
                    foreach (var file in Directory.GetFiles(outputDir))
                    {
                        File.Copy(file, Path.Combine(outputPath, Path.GetFileName(file)), true);
                    }

                    Console.WriteLine($"编译成功！输出文件已保存到：{outputPath}");
                }
                else
                {
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"build -c Debug -o {Path.Combine(tempDir, "output")}",
                            WorkingDirectory = tempDir,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"编译失败：\n{output}\n{error}");
                    }

                    // 复制生成的文件
                    string outputDir = Path.Combine(tempDir, "output");
                    foreach (var file in Directory.GetFiles(outputDir))
                    {
                        File.Copy(file, Path.Combine(outputPath, Path.GetFileName(file)), true);
                    }

                    Console.WriteLine($"编译成功！输出文件已保存到：{outputPath}");
                }
            }
            finally
            {
                // 清理临时目录
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Get locally available. NET version
        /// </summary>
        /// <returns>A list of all available.NET versions</returns>
        public List<DotnetVersion> CheckLocalVersion()
        {
            List<string> versions = GetInstalledNet5PlusVersions();
            List<DotnetVersion> dotnetVersions = new();

            foreach (string version in versions)
            {
                if (version.Contains(".NET 5"))
                {
                    if (!dotnetVersions.Contains(DotnetVersion.net5))
                    {
                        dotnetVersions.Add(DotnetVersion.net5);
                        //Console.WriteLine(5);
                    }
                }
                else if (version.Contains(".NET 6"))
                {
                    if (!dotnetVersions.Contains(DotnetVersion.net6))
                    {
                        dotnetVersions.Add(DotnetVersion.net6);
                        //Console.WriteLine(6);
                    }
                }
                else if (version.Contains(".NET 7"))
                {
                    if (!dotnetVersions.Contains(DotnetVersion.net7))
                    {
                        dotnetVersions.Add(DotnetVersion.net7);
                        //Console.WriteLine(7);
                    }
                }
                else if (version.Contains(".NET 8"))
                {
                    if (!dotnetVersions.Contains(DotnetVersion.net8))
                    {
                        dotnetVersions.Add(DotnetVersion.net8);
                        //Console.WriteLine(8);
                    }
                }
                else if (version.Contains(".NET 9"))
                {
                    if (!dotnetVersions.Contains(DotnetVersion.net9))
                    {
                        dotnetVersions.Add(DotnetVersion.net9);
                        //Console.WriteLine(9);
                    }
                }
            }

            return dotnetVersions;
        }
        #endregion

        private static List<string> GetInstalledNet5PlusVersions()
        {
            var versions = new HashSet<string>();

            try
            {
                // 执行命令获取运行时列表
                string output = ExecuteDotnetListCommand();

                // 逐行解析输出
                foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (TryParseNet5PlusVersion(line, out string version))
                    {
                        versions.Add(version);
                    }
                }
            }
            catch { /* 异常处理 */ }

            // 转换为有序列表
            var result = new List<string>(versions);
            result.Sort(CompareVersions);
            return result;
        }

        private static string ExecuteDotnetListCommand()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000); // 3秒超时
            return output;
        }

        private static bool TryParseNet5PlusVersion(string line, out string version)
        {
            version = null;

            // 过滤有效运行时组件
            if (!line.StartsWith("Microsoft.NETCore.App") &&
                !line.StartsWith("Microsoft.AspNetCore.App") &&
                !line.StartsWith("Microsoft.WindowsDesktop.App"))
            {
                return false;
            }

            // 提取版本号
            string[] parts = line.Split(' ');
            if (parts.Length < 2) return false;

            try
            {
                var ver = new Version(parts[1]);
                if (ver.Major < 5) return false; // 仅保留5+

                version = $".NET {ver.Major}"; // 格式化为大版本号
                return true;
            }
            catch { return false; }
        }

        private static int CompareVersions(string a, string b)
        {
            static int ExtractVersion(string s) => int.Parse(s.Replace(".NET ", ""));
            return ExtractVersion(a).CompareTo(ExtractVersion(b));
        }

        private static bool TryParseMajorVersion(string line, out string version)
        {
            version = null;

            // 匹配有效的运行时前缀
            if (!line.StartsWith("Microsoft.NETCore.App") &&
                !line.StartsWith("Microsoft.AspNetCore.App") &&
                !line.StartsWith("Microsoft.WindowsDesktop.App"))
            {
                return false;
            }

            // 提取版本号
            var segments = line.Split(' ');
            if (segments.Length < 2) return false;

            try
            {
                var ver = new Version(segments[1]);
                version = $".NET {ver.Major}";  // 只取主版本号
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
