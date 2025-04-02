using Microsoft.CodeAnalysis;

namespace RoslynCodeCompiler
{
    public class CompilationException : Exception
    {
        public IEnumerable<Diagnostic> Errors { get; }

        public CompilationException(IEnumerable<Diagnostic> errors)
            : base(FormatErrorMessage(errors))
        {
            Errors = errors;
        }

        private static string FormatErrorMessage(IEnumerable<Diagnostic> errors)
        {
            try
            {
                return $"Compilation failed with {errors.Count()} errors:\n" +
                       string.Join("\n", errors.Select(e => FormatDiagnostic(e)));
            }
            catch (Exception formatEx)
            {
                return $"Failed to format compilation errors: {formatEx.Message}";
            }
        }

        private static string FormatDiagnostic(Diagnostic diagnostic)
        {
            // 使用更健壮的诊断信息格式化方式
            var location = diagnostic.Location.GetMappedLineSpan();
            return $"[{diagnostic.Severity}] {diagnostic.Id}: " +
                   $"{diagnostic.GetMessage()} " +
                   $"at {location.Path}({location.StartLinePosition.Line + 1},{location.StartLinePosition.Character + 1})";
        }
    }
}
