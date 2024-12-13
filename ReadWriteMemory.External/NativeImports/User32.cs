using System.Runtime.InteropServices;
using ReadWriteMemory.External.Utilities;

namespace ReadWriteMemory.External.NativeImports;

internal static class User32
{
    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(Hotkeys.Key key);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int key);
}