using System.Runtime.InteropServices;
using ReadWriteMemory.Internal.Utilities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ReadWriteMemory.Internal.NativeImports;

public static partial class User32
{
    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(Hotkeys.Key key);

    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int key);
}