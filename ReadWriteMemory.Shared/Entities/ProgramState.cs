namespace ReadWriteMemory.Shared.Entities;

/// <summary>
/// Gives you the current state of the program.
/// </summary>
public enum ProgramState : byte
{
    /// <summary>
    /// Target program is running.
    /// </summary>
    Started,
    /// <summary>
    /// Target program is not running.
    /// </summary>
    Closed
}