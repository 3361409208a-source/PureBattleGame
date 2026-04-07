using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace PureBattleGame.Games.CockroachPet;

/// <summary>
/// 使用 Windows ConPTY API 的高级终端
/// 支持真正的交互式 CLI 工具嵌入
/// </summary>
public class ConPtyTerminal : IDisposable
{
    // Windows API imports
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessW(string lpApplicationName, StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
        uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute,
        IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int HANDLE_FLAG_INHERIT = 0x00000001;
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    // Instance fields
    private IntPtr _pseudoConsole;
    private IntPtr _inputRead;
    private IntPtr _inputWrite;
    private IntPtr _outputRead;
    private IntPtr _outputWrite;
    private StreamReader? _outputReader;
    private StreamWriter? _inputWriter;
    private Thread? _outputThread;
    private bool _running = false;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler? Exited;

    public bool Start(string command, int width = 80, int height = 30)
    {
        try
        {
            // Create pipes for communication
            if (!CreatePipe(out _inputRead, out _inputWrite, IntPtr.Zero, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!CreatePipe(out _outputRead, out _outputWrite, IntPtr.Zero, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Set handle inheritance
            SetHandleInformation(_inputWrite, HANDLE_FLAG_INHERIT, 0);
            SetHandleInformation(_outputRead, HANDLE_FLAG_INHERIT, 0);

            // Create pseudo console
            COORD size = new COORD { X = (short)width, Y = (short)height };
            if (!CreatePseudoConsole(size, _inputRead, _outputWrite, 0, out _pseudoConsole))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Create process with pseudo console
            var startupInfo = new STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

            // Initialize attribute list
            IntPtr sizePtr = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref sizePtr);
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(sizePtr);
            if (!InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref sizePtr))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Set pseudo console attribute
            if (!UpdateProcThreadAttribute(startupInfo.lpAttributeList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                _pseudoConsole, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Create the process
            var cmdLine = new StringBuilder($"cmd.exe /k {command}");
            if (!CreateProcessW(null, cmdLine, IntPtr.Zero, IntPtr.Zero, false,
                EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref startupInfo, out var procInfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Cleanup attribute list
            DeleteProcThreadAttributeList(startupInfo.lpAttributeList);
            Marshal.FreeHGlobal(startupInfo.lpAttributeList);

            CloseHandle(procInfo.hThread);

            // Create stream wrappers
            var inputStream = new FileStream(new SafeFileHandle(_inputWrite, false), FileAccess.Write);
            var outputStream = new FileStream(new SafeFileHandle(_outputRead, false), FileAccess.Read);
            _inputWriter = new StreamWriter(inputStream) { AutoFlush = true };
            _outputReader = new StreamReader(outputStream);

            // Start reading output
            _running = true;
            _outputThread = new Thread(ReadOutput);
            _outputThread.IsBackground = true;
            _outputThread.Start();

            return true;
        }
        catch (Exception ex)
        {
            Cleanup();
            OutputReceived?.Invoke(this, $"Failed to start terminal: {ex.Message}");
            return false;
        }
    }

    private void ReadOutput()
    {
        try
        {
            char[] buffer = new char[1024];
            while (_running && _outputReader != null)
            {
                int read = _outputReader.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    string text = new string(buffer, 0, read);
                    OutputReceived?.Invoke(this, text);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
        catch (Exception ex)
        {
            if (_running)
                OutputReceived?.Invoke(this, $"\nRead error: {ex.Message}");
        }
    }

    public void WriteInput(string input)
    {
        try
        {
            _inputWriter?.WriteLine(input);
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke(this, $"\nWrite error: {ex.Message}");
        }
    }

    public void Resize(int width, int height)
    {
        if (_pseudoConsole != IntPtr.Zero)
        {
            COORD size = new COORD { X = (short)width, Y = (short)height };
            ResizePseudoConsole(_pseudoConsole, size);
        }
    }

    private void Cleanup()
    {
        _running = false;

        _inputWriter?.Close();
        _outputReader?.Close();

        if (_pseudoConsole != IntPtr.Zero)
        {
            ClosePseudoConsole(_pseudoConsole);
            _pseudoConsole = IntPtr.Zero;
        }

        CloseHandle(_inputRead);
        CloseHandle(_outputRead);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
