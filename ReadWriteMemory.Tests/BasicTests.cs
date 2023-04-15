using ReadWriteMemory.Models;
using System.Diagnostics;

namespace ReadWriteMemory.Tests;

public class BasicTests
{
    private readonly Mem _memory;

    public BasicTests()
    {
        _memory = new("ReadWriteMemory.Tests.DummyProgram");
    }

    [Fact]
    public void TestWriteInt16()
    {
        Process.Start("cmd.exe", @" /c start P:\Tim\Dev\ReadWriteMemory.Tests.DummyProgram\ReadWriteMemory.Tests.DummyProgram\bin\Debug\net7.0\ReadWriteMemory.Tests.DummyProgram.exe");

        var shortValue = new MemoryAddress(0x00357618, "ReadWriteMemory.Tests.DummyProgram.exe", 0x58, 0x40, 0x38);
        short targetValue = 1200;

        var shortValueRead = _memory.ReadInt16(shortValue);

        Assert.False(shortValueRead is null);

        Assert.True(shortValueRead == 1200 || shortValueRead == 1201);
    }

    [Fact]
    public void TestWriteInt32()
    {

    }

    [Fact]
    public void TestWriteInt64()
    {

    }

    [Fact]
    public void TestWriteFloat()
    {

    }

    [Fact]
    public void TestWriteDouble()
    {

    }
}