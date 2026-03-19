using System;
using System.Windows.Forms;

namespace PureBattleGame;

static class Program
{
    /// <summary>
    /// 应用程序的主入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 捕获未处理异常
        Application.ThreadException += (s, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject as Exception);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new BattleForm());
    }

    static void LogException(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            string logPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "PureBattleGame_Error.log");
            string log = $"[{DateTime.Now}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            System.IO.File.AppendAllText(logPath, log);
            MessageBox.Show(
                $"程序发生错误:\n{ex.Message}\n\n详细日志已保存到:\n{logPath}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { }
    }
}