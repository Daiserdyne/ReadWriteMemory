using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Main;
using System.Collections.Frozen;
using System.Reflection;

namespace ReadWriteMemory.Services;

/// <summary>
/// Contains some usefull trainer helper-methods.
/// </summary>
public static class TrainerServices
{
    private static object? _threadObject;
    private static RWMemory? _memory;

    /// <summary>
    /// Gives you a <see cref="RWMemory"/> instance which you have created before with the <see cref="CreateAndGetSingletonInstance(string)"/> function.
    /// </summary>
    public static RWMemory GetCreatedSingletonInstance =>
        _memory is not null ? _memory : throw new NullReferenceException("A RWMemory instance has to be created before you can get it. " +
            $"Use the {nameof(CreateAndGetSingletonInstance)} method to creat a instance, then you can use this property.");

    /// <summary>
    /// Gives you a thread-safe singleton instance of the <see cref="RWMemory"/> object.
    /// </summary>
    /// <param name="processName"></param>
    /// <returns></returns>
    public static RWMemory CreateAndGetSingletonInstance(string processName)
    {
        _threadObject ??= new();

        lock (_threadObject)
        {
            return _memory ??= new(processName);
        }
    }

    /// <summary>
    /// <para>Returns a Dictionary of all classes which have implemented the <seealso cref="IMemoryTrainer"/> interface in your entry assembly.</para>
    /// The key is the <see cref="IMemoryTrainer.TrainerName"/> and the value the instantiated Trainer.
    /// </summary>
    /// <returns>A dictionary of all implemented trainers. If no trainers are found, this will return a <c>empty</c> dictionary.</returns>
    public static FrozenDictionary<string, IMemoryTrainer> GetAllImplementedTrainers()
    {
        var trainerRegister = new Dictionary<string, IMemoryTrainer>();

        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly is null)
        {
            return trainerRegister.ToFrozenDictionary();
        }

        var implementedTrainers = (from type in entryAssembly.GetTypes()
                                   where type.GetInterfaces().Contains(typeof(IMemoryTrainer))
                                         && type.GetConstructor(Type.EmptyTypes) != null
                                   select Activator.CreateInstance(type) as IMemoryTrainer).ToList();

        if (!implementedTrainers.Any())
        {
            return trainerRegister.ToFrozenDictionary();
        }

        foreach (var trainer in implementedTrainers)
        {
            trainerRegister.Add(trainer.TrainerName, trainer);
        }

        return trainerRegister.ToFrozenDictionary();
    }
}