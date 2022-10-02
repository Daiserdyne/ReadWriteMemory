namespace ReadWriteMemory.Interfaces;

public interface ITrainer
{
    /// <summary>
    /// Shortname of the trainer.
    /// </summary>
    public string Shortname { get; }

    /// <summary>
    /// Description of what this trainer does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// This will execute the trainer.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Enable(params string[] args);

    /// <summary>
    /// This will disable the trainer.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task Disable(params string[] args);
}