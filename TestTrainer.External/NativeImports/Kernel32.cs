using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace TestTrainer.External.NativeImports;

public static class Kernel32
{
    public delegate bool ConsoleCtrlDelegate(CtrlTypes ctrlType);

    // Import der Windows-API
    [DllImport("Kernel32")]
    public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handler, bool add);
    
    public enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }
}