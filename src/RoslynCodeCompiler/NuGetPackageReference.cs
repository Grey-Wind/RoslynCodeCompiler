using NuGet.Versioning;

namespace RoslynCodeCompiler
{
    public class NuGetPackageReference
    {
        public string PackageId { get; set; }
        public NuGetVersion Version { get; set; }
    }
}
