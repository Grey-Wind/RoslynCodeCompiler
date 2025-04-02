using System.ComponentModel;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace RoslynCodeCompiler
{
    public class CodeCompiler
    {
        public async Task CompileAsync(
            string code,
            string outputDirectory,
            string assemblyName,
            IEnumerable<NuGetPackageReference>? nuGetReferences = null,
            string runtimeIdentifier = "win-x64")
        {
            // 创建输出目录
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{assemblyName}.exe");

            // Validate code for top-level statements
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync();
            if (root.DescendantNodes().OfType<GlobalStatementSyntax>().Any())
            {
                throw new ArgumentException("Top-level statements are not allowed.");
            }

            // Prepare metadata references
            var references = new List<MetadataReference>();
            references.AddRange(GetDefaultReferences());

            // Add NuGet references
            if (nuGetReferences != null && nuGetReferences.Any())
            {
                await AddNuGetReferencesAsync(nuGetReferences, references);
            }

            #region 配置编译选项
            // 配置编译选项
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: options);

            // 配置Emit参数
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.Embedded,
                pdbFilePath: Path.Combine(outputDirectory, $"{assemblyName}.pdb"));

            // 执行编译输出到文件
            var result = compilation.Emit(outputPath);
            #endregion

            if (result.Success)
            {
                // 使用.NET 6兼容的依赖复制方式
                CopyRuntimeDependencies(outputDirectory);
            }
        }

        private void CopyRuntimeDependencies(string outputDir)
        {
            // 获取当前运行时目录
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            // 需要复制的基础程序集列表
            var essentialAssemblies = new[]
            {
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "netstandard.dll",
            "System.Console.dll",
            "System.Threading.Tasks.dll",
            "System.Linq.dll"
        };

            // 复制基础程序集
            foreach (var asm in essentialAssemblies)
            {
                var sourcePath = Path.Combine(runtimeDir, asm);
                if (File.Exists(sourcePath))
                {
                    File.Copy(
                        sourcePath,
                        Path.Combine(outputDir, asm),
                        overwrite: true);
                }
            }

            // 复制所有.NET运行时DLL（可选）
            foreach (var file in Directory.GetFiles(runtimeDir, "*.dll"))
            {
                var destPath = Path.Combine(outputDir, Path.GetFileName(file));
                if (!File.Exists(destPath))
                {
                    File.Copy(file, destPath);
                }
            }
        }

        // 修改后的GetDefaultReferences方法
        private IEnumerable<MetadataReference> GetDefaultReferences()
        {
            var assemblies = new[]
            {
                // 基础类型程序集
                typeof(object).Assembly,           // System.Runtime
                typeof(Uri).Assembly,              // System.Private.Uri
                typeof(GCSettings).Assembly,  // System.Private.CoreLib
                typeof(Enum).Assembly,         // System.Runtime
                typeof(ValueType).Assembly,     // System.Runtime
                typeof(Delegate).Assembly,      // System.Runtime
                typeof(Task).Assembly,          // System.Threading.Tasks
                typeof(Console).Assembly,       // System.Console
                typeof(Enumerable).Assembly,    // System.Linq
                typeof(IQueryable).Assembly,    // System.Linq.Expressions
                typeof(Decimal).Assembly,       // System.Runtime
                typeof(EditorBrowsableAttribute).Assembly // System.ComponentModel.TypeConverter
            };

            var netstandardAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "netstandard");

            var references = new List<MetadataReference>();

            // 添加自动发现的程序集
            foreach (var assembly in assemblies)
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {
                    // 处理部分程序集无法加载的情况
                }
            }

            // 手动添加关键程序集
            AddAssemblyIfMissing(references, "System.Private.CoreLib");
            AddAssemblyIfMissing(references, "System.Runtime");
            AddAssemblyIfMissing(references, "netstandard");

            // 添加netstandard引用
            if (netstandardAssembly != null)
            {
                references.Add(MetadataReference.CreateFromFile(netstandardAssembly.Location));
            }

            return references;
        }

        // 辅助方法：添加缺失的程序集引用
        private void AddAssemblyIfMissing(List<MetadataReference> references, string assemblyName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (assembly != null && !references.Any(r => r.Display?.Contains(assemblyName) == true))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        private async Task AddNuGetReferencesAsync(IEnumerable<NuGetPackageReference> nuGetReferences,
                                                   List<MetadataReference> metadataReferences)
        {
            using var cache = new SourceCacheContext();
            var logger = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;

            var settings = Settings.LoadDefaultSettings(root: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(
                new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());

            foreach (var packageRef in nuGetReferences)
            {
                var repositories = sourceRepositoryProvider.GetRepositories();
                var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                // 获取包依赖的正确方式
                await GetPackageDependenciesAsync(
                    new PackageIdentity(packageRef.PackageId, packageRef.Version),
                    repositories,
                    cache,
                    logger,
                    cancellationToken,
                    availablePackages);

                // 创建正确的PackageResolverContext
                var resolverContext = new PackageResolverContext(
                    dependencyBehavior: DependencyBehavior.Lowest,
                    targetIds: new[] { packageRef.PackageId },
                    requiredPackageIds: Enumerable.Empty<string>(),
                    packagesConfig: Enumerable.Empty<PackageReference>(),
                    preferredVersions: Enumerable.Empty<PackageIdentity>(),
                    availablePackages: availablePackages,
                    packageSources: repositories.Select(r => r.PackageSource), // 关键修正点
                    log: logger);

                var resolver = new PackageResolver();
                var resolvedPackages = resolver.Resolve(resolverContext, cancellationToken)
                    .Select(p => availablePackages.Single(x => x.Id == p.Id && x.Version == p.Version));

                foreach (var package in resolvedPackages)
                {
                    var repository = sourceRepositoryProvider.GetRepositories().FirstOrDefault(r => r.PackageSource.IsLocal == package.Source.PackageSource.IsLocal);

                    var downloadResource = await repository!.GetResourceAsync<DownloadResource>();
                    var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                        package,
                        new PackageDownloadContext(cache),
                        SettingsUtility.GetGlobalPackagesFolder(settings),
                        logger,
                        cancellationToken);

                    // 使用正确的包存储路径
                    var packageDirectory = Path.Combine(
                        SettingsUtility.GetGlobalPackagesFolder(settings),
                        package.Id.ToLowerInvariant(),
                        package.Version.ToNormalizedString());

                    // 使用NuGet.Packaging.PackageExtractor保存包
                    await PackageExtractor.ExtractPackageAsync(
                        packageDirectory,
                        downloadResult.PackageStream,
                        new PackagePathResolver(SettingsUtility.GetGlobalPackagesFolder(settings)),
                        new PackageExtractionContext(
                            PackageSaveMode.Defaultv3,
                            XmlDocFileSaveMode.Skip,
                            ClientPolicyContext.GetClientPolicy(settings, logger),
                            logger),
                        cancellationToken); // 添加的第五个参数

                    AddPackageReferences(packageDirectory, metadataReferences);
                }
            }
        }

        private async Task GetPackageDependenciesAsync(
            PackageIdentity package,
            IEnumerable<SourceRepository> repositories,
            SourceCacheContext cache,
            ILogger logger,
            CancellationToken cancellationToken,
            ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;

            foreach (var repo in repositories)
            {
                var dependencyInfoResource = await repo.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package,
                    NuGetFramework.Parse("netstandard2.1"),
                    cache,
                    logger,
                    cancellationToken);

                if (dependencyInfo != null)
                {
                    availablePackages.Add(dependencyInfo);
                    foreach (var dependency in dependencyInfo.Dependencies)
                    {
                        await GetPackageDependenciesAsync(
                            new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                            repositories,
                            cache,
                            logger,
                            cancellationToken,
                            availablePackages);
                    }
                    break;
                }
            }
        }

        private void AddPackageReferences(string packageDirectory, List<MetadataReference> metadataReferences)
        {
            var libDir = Path.Combine(packageDirectory, "lib");
            if (!Directory.Exists(libDir)) return;

            var targetFrameworks = new[] { "netstandard2.1", "netcoreapp3.1", "net5.0", "net6.0" };
            foreach (var tf in targetFrameworks)
            {
                var tfDir = Path.Combine(libDir, tf);
                if (Directory.Exists(tfDir))
                {
                    foreach (var dll in Directory.GetFiles(tfDir, "*.dll"))
                    {
                        metadataReferences.Add(MetadataReference.CreateFromFile(dll));
                    }
                    break;
                }
            }
        }
    }
}
