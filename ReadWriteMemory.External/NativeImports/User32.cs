using System.Runtime.InteropServices;
using ReadWriteMemory.External.Utilities;

namespace ReadWriteMemory.External.NativeImports;

internal static partial class User32
{
    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(Hotkeys.Key key);

    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int key);
}