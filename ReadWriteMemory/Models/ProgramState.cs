namespace ReadWriteMemory.Models;

/// <summary>
/// Gives you the current state of the program.
/// </summary>
public enum ProgramState : byte
{
    /// <summary>
    /// Target program is running.
    /// </summary>
    Running,
    /// <summary>
    /// Target program is not running.
    /// </summary>
    NotRunning
}