using ReadWriteMemory.Trainer.Interface;
using System.Reflection;

namespace ReadWriteMemory.Services;

public class TrainerServices
{
    private static IDictionary<string, ITrainer>? _trainerRegister;

    /// <summary>
    /// <para>Returns a Dictionary of all classes which have implemented the <seealso cref="ITrainer"/> interface in your entry assembly.</para>
    /// The key is the trainer <see cref="ITrainer.TrainerName"/> and the value the instantiated Trainer.
    /// </summary>
    /// <returns>A dictionary of all implemented trainers. If no trainers are found, this will return a <c>empty</c> dictionary.</returns>
    public static IDictionary<string, ITrainer> ImplementedTrainers
    {
        get => _trainerRegister ??= GetAllImplementedTrainers();
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
            trainerRegister.Add(trainer.TrainerName, trainer);

        return trainerRegister;
    }
}