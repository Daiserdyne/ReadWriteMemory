using System.Runtime.InteropServices;
using System.Text;

namespace ReadWriteMemory.NativeImports;

internal static class PsApi
{
    internal const uint LIST_MODULES_ALL = 0x03;

    [DllImport("psapi.dll")]
    internal static extern bool EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr[] lphModule, int cb, out int lpcbNeeded, uint dwFilterFlag);

    [DllImport("psapi.dll")]
    internal static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);
}