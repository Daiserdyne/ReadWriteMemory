using System.Runtime.InteropServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ReadWriteMemory.Internal.NativeImports;

public static partial class User32
{
    [LibraryImport("User32")]
    public static partial int MessageBoxA(nint hWnd, ReadOnlySpan<byte> msg, ReadOnlySpan<byte> wParam, nint lParam);
}