using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCodeCompiler
{
    /// <summary>
    /// An interface for compiling code.
    /// </summary>
    public interface ICompiler
    {
        /// <summary>
        /// Code to compile
        /// </summary>
        string Code { get; set; }

        /// <summary>
        /// Start compiling code
        /// </summary>
        /// <param name="outputPath">Output path</param>
        /// <exception cref="Exception">Compilation failure</exception>
        void CompileCode(string outputPath);
        /// <summary>
        /// Get locally available. NET version
        /// </summary>
        /// <returns>A list of all available.NET versions</returns>
        List<CodeCompiler.DotnetVersion> CheckLocalVersion();
    }
}
