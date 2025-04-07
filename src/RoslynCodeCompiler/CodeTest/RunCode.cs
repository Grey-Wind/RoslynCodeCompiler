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
        private Process process;
        public event EventHandler<string> OutputReceived;
        public event EventHandler<string> ErrorReceived;
        public event EventHandler Exited;

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
                process.Exited += ProcessExited;

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
                        Console.WriteLine($"成功终止进程：{process.ProcessName} (ID: {process.Id})");
                    }
                    catch (Win32Exception ex)
                    {
                        Console.WriteLine($"权限不足，无法终止 {process.ProcessName}。请以管理员身份运行程序。");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine($"进程 {process.ProcessName} 已退出。");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"终止 {process.ProcessName} 时出错：{ex.Message}");
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
