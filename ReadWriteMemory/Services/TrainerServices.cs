using ReadWriteMemory.Interfaces;
using System.Reflection;

namespace ReadWriteMemory.Services;

public static class TrainerServices
{
    private static IDictionary<string, ITrainer>? _implementedTrainers;

    /// <summary>
    /// Returns a Dictionary of all classes which have implemented the <c>ITrainer</c> interface in your assembly.
    /// The key ist is the trainer shortname and the value the instantiated Trainer.
    /// </summary>
    public static IDictionary<string, ITrainer> ImplementedTrainers
    {
        get => _implementedTrainers ??= GetAllImplementedTrainers();
    }

    private static IDictionary<string, ITrainer> GetAllImplementedTrainers()
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly is null)
            return new Dictionary<string, ITrainer>();

        var implementedTrainers = (from t in entryAssembly.GetTypes()
                                  where t.GetInterfaces().Contains(typeof(ITrainer))
                                        && t.GetConstructor(Type.EmptyTypes) != null
                                  select Activator.CreateInstance(t) as ITrainer).ToList();

        if (!implementedTrainers.Any())
            return new Dictionary<string, ITrainer>();

        var trainerRegister = new Dictionary<string, ITrainer>();

        foreach (var trainer in implementedTrainers)
            trainerRegister.Add(trainer.Shortname, trainer);

        return trainerRegister;
    }
}