using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

#pragma warning disable CS1591

namespace ReadWriteMemory.Internal.NativeImports;

public static partial class Kernel32
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllocConsole();
    
    [LibraryImport("kernel32.dll")]
    public static partial void FreeLibraryAndExitThread(nint hLibModule, int dwExitCode);
    
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FreeLibrary(nint hLibModule);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint CreateThread(
        nint lpThreadAttributes,
        uint dwStackSize,
        nint lpStartAddress,
        nint lpParameter,
        uint dwCreationFlags,
        out uint lpThreadId
    );
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void ExitThread(uint dwExitCode);
   
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string moduleName);
}