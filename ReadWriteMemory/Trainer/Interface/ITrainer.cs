namespace ReadWriteMemory.Trainer.Interface;

/// <summary>
/// Standard interface for trainer.
/// </summary>
public interface ITrainer
{
    /// <summary>
    /// Shortname of the trainer.
    /// </summary>
    public string TrainerName { get; }

    /// <summary>
    /// Description of what this trainer does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// If true, the <see cref="Disable(string[]?)"/> function will be called when the <see cref="Mem"/> object will be disposed.
    /// </summary>
    public bool DisableWhenDispose { get; }

    /// <summary>
    /// This will execute the trainer payload.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Enable(params string[]? args);

    /// <summary>
    /// This will disable the trainer payload.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Disable(params string[]? args);
}