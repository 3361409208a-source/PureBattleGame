using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 嵌入式终端 - 使用 SetParent 将控制台窗口嵌入到 WinForms 控件中
/// </summary>
public class EmbeddedTerminal : IDisposable
{
    // Windows API
    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private Process? _process;
    private IntPtr _consoleHandle;
    private Control? _hostControl;
    private bool _isRunning = false;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler? Exited;

    /// <summary>
    /// 启动一个命令并将其窗口嵌入到指定控件中
    /// </summary>
    public bool Start(string command, Control hostControl)
    {
        try
        {
            _hostControl = hostControl;

            // 创建进程启动信息
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {command}",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden // 先隐藏，然后再嵌入
            };

            _process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            _process.Exited += (s, e) =>
            {
                _isRunning = false;
                Exited?.Invoke(this, EventArgs.Empty);
            };

            // 启动进程
            _process.Start();
            _isRunning = true;

            // 等待窗口创建
            Thread.Sleep(500);

            // 找到控制台窗口
            _consoleHandle = FindConsoleWindow(_process.Id);

            if (_consoleHandle == IntPtr.Zero)
            {
                // 尝试获取进程主窗口
                _process.Refresh();
                _consoleHandle = _process.MainWindowHandle;
            }

            if (_consoleHandle == IntPtr.Zero)
            {
                OutputReceived?.Invoke(this, "Warning: Could not find console window, running in detached mode");
                return true; // 仍然返回 true，因为进程已启动
            }

            // 嵌入到宿主控件
            EmbedWindow(_consoleHandle, hostControl);

            return true;
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke(this, $"Failed to start: {ex.Message}");
            return false;
        }
    }

    private IntPtr FindConsoleWindow(int processId)
    {
        // 枚举窗口查找属于该进程的控制台窗口
        IntPtr result = IntPtr.Zero;
        EnumWindows((hwnd, lParam) =>
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == processId)
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(hwnd, className, className.Capacity);
                if (className.ToString().Contains("ConsoleWindowClass"))
                {
                    result = hwnd;
                    return false; // 停止枚举
                }
            }
            return true; // 继续枚举
        }, IntPtr.Zero);

        return result;
    }

    private void EmbedWindow(IntPtr hwnd, Control host)
    {
        try
        {
            // 设置父窗口
            SetParent(hwnd, host.Handle);

            // 设置窗口样式 - 移除边框和标题栏
            SetWindowLong(hwnd, GWL_STYLE, WS_CHILD | WS_VISIBLE);

            // 调整窗口大小和位置
            ResizeToHost();

            // 显示窗口
            ShowWindow(hwnd, SW_SHOW);

            // 监听宿主大小变化
            host.Resize += (s, e) => ResizeToHost();
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke(this, $"Embed error: {ex.Message}");
        }
    }

    public void ResizeToHost()
    {
        if (_consoleHandle == IntPtr.Zero || _hostControl == null) return;

        SetWindowPos(_consoleHandle, IntPtr.Zero,
            0, 0, _hostControl.Width, _hostControl.Height,
            SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }

    public void Stop()
    {
        try
        {
            _isRunning = false;
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
            }
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    // Windows API 导入
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_STYLE = -16;
    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}
