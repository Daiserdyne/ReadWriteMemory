using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestTrainer.Internal.InjectMe;

public static partial class EntryPoint
{
    public static ReadOnlySpan<byte> Mogus => "Amogus"u8;
    
    [LibraryImport("user32.dll")]
    private static partial int MessageBoxA(nint hWnd, ReadOnlySpan<byte> text, ReadOnlySpan<byte> caption, uint type);
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvFastcall)], EntryPoint = nameof(DllMain))]
    public static bool DllMain(nint module, uint reason, nint reserved)
    {
        if (reason == 1)
        {
            MessageBoxA(nint.Zero, Mogus, "Christus"u8, 0x000000100);
        }
        
        return true; 
    }
}