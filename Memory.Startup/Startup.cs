using ReadWriteMemory;

var mem = Memory.Instance;

mem.Logger.OnLogging += Logger_OnLogging;

mem.OpenProcess("Outlast2");
mem.GetTargetAddress("Outlast2.exe", 0x219FF58, new int[] { 0xC38, 0x7F58 });

async void Logger_OnLogging(string caption, string message)
{
    Console.WriteLine(caption + message);
}

Console.ReadLine();