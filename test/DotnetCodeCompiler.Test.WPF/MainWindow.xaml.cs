using System.IO;
using System.Text;
using System.Windows;
using DotnetCodeCompiler;
using DotnetCodeCompiler.CodeTest;

namespace RoslynCodeCompiler.Test.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RunCode runner = new RunCode();

        public MainWindow()
        {
            InitializeComponent();

#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
            runner.OutputReceived += OnOutputReceived;
            runner.ErrorReceived += OnErrorReceived;
            runner.Exited += OnExited;
#pragma warning restore CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
        }

        #region runner
        private void OnOutputReceived(object sender, string data)
        {
            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText(data + Environment.NewLine);
                OutputBox.ScrollToEnd();
            });
        }

        private void OnErrorReceived(object sender, string data)
        {
            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText($"[错误] {data}{Environment.NewLine}");
                OutputBox.ScrollToEnd();
            });
        }

        private void OnExited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText("进程已退出。" + Environment.NewLine);
            });
        }
        #endregion

        private void CompilerBtn_Click(object sender, RoutedEventArgs e)
        {
            var compiler = new CodeCompiler("Test");

            string code = CodeBox.Text;

            string outputDirectory = OutputPathBox.Text;

            try
            {
                Directory.CreateDirectory(outputDirectory);
                compiler.Code = code;
                compiler.dotnetVersion = CodeCompiler.DotnetVersion.auto;
                compiler.buildType = CodeCompiler.BuildType.Release;
                compiler.CompileCode(outputDirectory); // 同步编译
                // await compiler.CompileCodeAsync(outputDirectory); // 异步编译

                OutputBox.Clear();

                //runner.Kill(compiler.AppName);

                // 获取当前工作目录或应用程序基目录
                string baseDirectory = Directory.GetCurrentDirectory(); // 或者 AppDomain.CurrentDomain.BaseDirectory
                                                                        // 合并路径并解析为绝对路径
                string absolutePath = Path.GetFullPath(Path.Combine(baseDirectory, compiler.AppFolderPath));
                MessageBox.Show(absolutePath + compiler.AppName);
                runner.Execute(compiler.AppName, absolutePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误：{ex.Message}");
            }
        }
    }
}