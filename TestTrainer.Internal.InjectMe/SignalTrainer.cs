using System.Runtime.InteropServices;
using ReadWriteMemory.Internal;

namespace TestTrainer.Internal.InjectMe;

public sealed partial class SignalTrainer
{
    [LibraryImport("User32")]
    public static partial int MessageBoxA(nint hWnd, ReadOnlySpan<byte> msg, ReadOnlySpan<byte> wParam, nint lParam);
    
    private readonly RwMemory _memory = new();

    public Task Main(CancellationToken _)
    {
        MessageBoxA(nint.Zero, "Amogus"u8, "Christus"u8, nint.Zero);
        
        return Task.CompletedTask;
    }
}