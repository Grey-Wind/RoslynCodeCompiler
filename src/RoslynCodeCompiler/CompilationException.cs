using Microsoft.CodeAnalysis;

namespace RoslynCodeCompiler
{
    public class CompilationException : Exception
    {
        public IEnumerable<Diagnostic> Errors { get; }

        public CompilationException(IEnumerable<Diagnostic> errors)
            : base($"Compilation failed with {errors.Count()} errors:\n" +
                   string.Join("\n", errors.Select(e => e.GetMessage())))
        {
            Errors = errors;
        }
    }
}
