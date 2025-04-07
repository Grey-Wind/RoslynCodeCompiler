using System.Diagnostics;

namespace DotnetCodeCompiler
{
    /// <summary>
    /// Compile by calling the command line .NET project
    /// </summary>
    public class CodeCompiler : ICompiler
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
        public DotnetVersion dotnetVersion { get; set; } = DotnetVersion.auto;

        /// <summary>
        /// Select the build mode
        /// </summary>
        public BuildType buildType {  get; set; } = BuildType.Release;

        /// <summary>
        /// Code to compile
        /// </summary>
        public string Code {  get; set; }

        /// <summary>
        /// The name of the project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The output file path. Include the .exe .
        /// </summary>
        public string AppPath { get; private set; }

        /// <summary>
        /// The output file path. Not include the .exe .
        /// </summary>
        public string AppFolderPath { get; private set; }

        public string AppName {  get; private set; }
        #endregion

        #region public method
#pragma warning disable 8618
        /// <summary>
        /// Compile by calling the command line .NET project
        /// </summary>
        /// <param name="projectName">The name of the project</param>
        /// <param name="dv">.NET version</param>
        public CodeCompiler(string projectName, DotnetVersion dv = DotnetVersion.auto)
        {
            dotnetVersion = dv;
            ProjectName = projectName;
            AutoNetVersionSelect();
        }

        /// <summary>
        /// Compile by calling the command line .NET project
        /// </summary>
        /// <param name="projectName">The name of the project</param>
        /// <param name="c">Code</param>
        /// <param name="dv">.NET version</param>
        public CodeCompiler(string projectName, string c, DotnetVersion dv = DotnetVersion.auto)
        {
            ProjectName = projectName;
            Code = c;
            dotnetVersion = dv;
            AutoNetVersionSelect();
        }
#pragma warning restore 8618

        /// <summary>
        /// Start compiling code
        /// </summary>
        /// <param name="outputPath">Output path</param>
        /// <exception cref="Exception">Compilation failure</exception>
        public virtual void CompileCode(string outputPath)
        {
            // 创建临时目录
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 创建项目文件
                string projectFile = Path.Combine(tempDir, $"{ProjectName}.csproj");

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

                GenerateOutputInfo(outputPath);
            }
            finally
            {
                // 清理临时目录
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Start compiling code
        /// </summary>
        /// <param name="outputPath">Output path</param>
        /// <exception cref="Exception">Compilation failure</exception>
        public async Task CompileCodeAsync(string outputPath) => await Task.Run(() => CompileCode(outputPath));

        /// <summary>
        /// Get locally available. NET version
        /// </summary>
        /// <returns>A list of all available.NET versions</returns>
        public virtual List<DotnetVersion> CheckLocalVersion()
        {
            List<string> versions = GetNetVersion.GetInstalledNet5PlusVersions();
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

        #region private method
        private void AutoNetVersionSelect()
        {
            if (dotnetVersion != DotnetVersion.auto)
            {
                return;
            }

            List<DotnetVersion> dvs = CheckLocalVersion();

            if (dvs == null)
            {
                return;
            }

            if (dvs.Contains(DotnetVersion.net5))
            {
                dotnetVersion = DotnetVersion.net5;
            }
            else if (dvs.Contains(DotnetVersion.net6))
            {
                dotnetVersion = DotnetVersion.net6;
            }
            else if (dvs.Contains(DotnetVersion.net7))
            {
                dotnetVersion = DotnetVersion.net7;
            }
            else if (dvs.Contains(DotnetVersion.net8))
            {
                dotnetVersion = DotnetVersion.net8;
            }
            else if (dvs.Contains(DotnetVersion.net9))
            {
                dotnetVersion = DotnetVersion.net9;
            }
        }

        private void GenerateOutputInfo(string outputPath)
        {
            AppPath = outputPath + "/" + ProjectName + ".exe";
            AppFolderPath = Path.Combine(outputPath) + "/";
            AppName = ProjectName + ".exe";
        }
        #endregion
    }
}
