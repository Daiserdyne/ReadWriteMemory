using ReadWriteMemory.Main;

namespace ReadWriteMemory.Interfaces;

/// <summary>
/// Standard interface for a trainer.
/// </summary>
public interface ITrainer
{
    /// <summary>
    /// Name of the trainer.
    /// </summary>
    public string TrainerName { get; }

    /// <summary>
    /// Description of what this trainer does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// If true, the <see cref="Disable"/> function will be called when the <see cref="RWMemory"/> object will be disposed.
    /// </summary>
    public bool DisableWhenDispose { get; }

    /// <summary>
    /// This function contains the trainer logic.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Enable(params string[]? args);

    /// <summary>
    /// This function restores the official state of the program.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Disable(params string[]? args);
}