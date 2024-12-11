using System.Runtime.InteropServices;
using ReadWriteMemory.Shared.Utilities;

namespace ReadWriteMemory.Shared.NativeImports;

public static class User32
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Hotkeys.Key key);
    
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);
}