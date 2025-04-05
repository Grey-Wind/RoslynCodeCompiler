using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCodeCompiler
{
    internal class GetNetVersion
    {
        public static List<string> GetInstalledNet5PlusVersions()
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
            version = null!;

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
            version = null!;

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
