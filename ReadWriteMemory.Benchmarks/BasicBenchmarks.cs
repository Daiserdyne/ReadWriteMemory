using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ReadWriteMemory.Main;
using ReadWriteMemory.Models;

namespace ReadWriteMemory.Benchmarks;

public class BasicBenchmarks
{
    public readonly RWMemory _memory = new("Outlast2");

    public readonly static MemoryAddress _enemy_X_Coordinate = new(0x56C55F, "Outlast2.exe");

    public readonly static byte[] _newCode = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };


    public static void Main()
    {
        BenchmarkRunner.Run<BasicBenchmarks>();
    }

    [Benchmark]
    public async Task ReadValue()
    {
        await _memory.CreateOrResumeCodeCaveAsync(_enemy_X_Coordinate, _newCode, 9);
    }
}