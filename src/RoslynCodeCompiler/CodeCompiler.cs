using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            string outputPath,
            IEnumerable<NuGetPackageReference> nuGetReferences = null)
        {
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

            // Configure compilation
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputPath),
                new[] { syntaxTree },
                references,
                options);

            // Execute compilation
            var result = compilation.Emit(outputPath);

            if (!result.Success)
            {
                var errors = result.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);
                throw new CompilationException(errors);
            }
        }

        private IEnumerable<MetadataReference> GetDefaultReferences()
        {
            // Add essential framework references
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            };

            // Add netstandard reference
            var netstandardAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "netstandard");
            if (netstandardAssembly != null)
            {
                references.Add(MetadataReference.CreateFromFile(netstandardAssembly.Location));
            }

            return references;
        }

        private async Task AddNuGetReferencesAsync(
    IEnumerable<NuGetPackageReference> nuGetReferences,
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
                    DependencyBehavior.Lowest,
                    new[] { packageRef.PackageId },
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<PackageReference>(),
                    Enumerable.Empty<PackageIdentity>(),
                    availablePackages,
                    (IEnumerable<PackageSource>)repositories,  // 修正参数类型
                    logger);

                var resolver = new PackageResolver();
                var resolvedPackages = resolver.Resolve(resolverContext, cancellationToken)
                    .Select(p => availablePackages.Single(x => x.Id == p.Id && x.Version == p.Version));

                foreach (var package in resolvedPackages)
                {
                    var repository = sourceRepositoryProvider.GetRepositories().FirstOrDefault(r => r.PackageSource.IsLocal == package.Source.PackageSource.IsLocal);

                    var downloadResource = await repository.GetResourceAsync<DownloadResource>();
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
