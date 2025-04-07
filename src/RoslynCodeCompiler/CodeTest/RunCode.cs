using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCodeCompiler.CodeTest
{
    /// <summary>
    /// Run the code
    /// </summary>
    public class RunCode
    {
        private Process? process;
        /// <summary>
        /// Output received event
        /// </summary>
        public event EventHandler<string>? OutputReceived;
        /// <summary>
        /// Error received event
        /// </summary>
        public event EventHandler<string>? ErrorReceived;
        /// <summary>
        /// Exited event
        /// </summary>
        public event EventHandler? Exited;

        /// <summary>
        /// Execute the app
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="workingDirectory">Working directory</param>
        public void Execute(string filePath, string workingDirectory)
        {
            try
            {
                process = new Process();

                //process.StartInfo.FileName = "cmd.exe";
                //process.StartInfo.Arguments = @"/c" + filePath;

                //process.StartInfo.FileName = filePath;

                process.StartInfo.FileName = "cmd.exe"; // 使用cmd作为宿主
                process.StartInfo.Arguments = $"/c \"{filePath}\""; // /c 表示执行后关闭cmd

                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.EnableRaisingEvents = true;
#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
                process.Exited += ProcessExited;
#pragma warning restore CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        OutputReceived?.Invoke(this, e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        ErrorReceived?.Invoke(this, e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, $"启动进程失败: {ex.Message}");
            }
            finally
            {
                //process.Kill();
                //process.WaitForExit();
            }
        }

        /// <summary>
        /// Kill the process
        /// </summary>
        /// <param name="filePath">File name</param>
        public void Kill(string filePath)
        {
            string processName = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"未找到进程：{filePath}");
                    return;
                }

                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(); // 等待进程终止
                        Console.WriteLine($"Successfully terminated process：{process.ProcessName} (ID: {process.Id})");
                    }
                    catch (Win32Exception)
                    {
                        Console.WriteLine($"{process.ProcessName} cannot be terminated because the permission is insufficient. Procedure Please run the program as administrator.");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine($"The process {process.ProcessName} has exited.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error terminating {process.ProcessName} : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：{ex.Message}");
            }
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            Exited?.Invoke(this, EventArgs.Empty);
            Cleanup();
        }

        /// <summary>
        /// Stop the process
        /// </summary>
        public void Stop()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, $"停止进程失败: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            process?.Dispose();
            process = null!;
        }
    }
}
