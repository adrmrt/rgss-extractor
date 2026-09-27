using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RgssExtractor.Core;

namespace RgssExtractor
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                var foregroundWindow = GetForegroundWindow();
                int processId;
                GetWindowThreadProcessId(foregroundWindow, out processId);
                var processById = Process.GetProcessById(processId);
                if (processById.ProcessName == "cmd")
                {
                    AttachConsole(processById.Id);
                }
                else
                {
                    AllocConsole();
                }

                int exitCode = RunCommandLine(args);
                FreeConsole();
                return exitCode;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
                MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunCommandLine(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: \"RGSS Extractor.exe\" <archive> <output folder>");
                return 1;
            }

            try
            {
                var mainParser = new MainParser();
                if (mainParser.ParseFile(args[0]) == null)
                {
                    Console.Error.WriteLine("{0} is not a supported RGSS archive.", args[0]);
                    return 1;
                }

                mainParser.ExportArchive(args[1]);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }
    }
}