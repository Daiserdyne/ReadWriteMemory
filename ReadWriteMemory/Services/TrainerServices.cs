﻿using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Main;
using System.Reflection;

namespace ReadWriteMemory.Services;

/// <summary>
/// Contains some usefull trainer helper-methods.
/// </summary>
public sealed class TrainerServices
{
    private static object? _mem;
    private static RWMemory? _memory;

    /// <summary>
    /// Gives you a thread-safe singleton instance of the <see cref="RWMemory"/> object.
    /// </summary>
    /// <param name="processName"></param>
    /// <returns></returns>
    public static RWMemory GetSingletonInstance(string processName)
    {
        _mem ??= new();

        lock (_mem)
        {
            return _memory ??= new(processName);
        }
    }

    /// <summary>
    /// Gives you a thread-safe singleton instance of the <see cref="RWMemory"/> object.
    /// </summary>
    /// <param name="processName"></param>
    /// <returns></returns>
    public static RWMemory GetSingletonInstance()
    {
        return _memory is not null ? _memory : throw new NullReferenceException("Memory has to be created before you can get it.");
    }

    /// <summary>
    /// <para>Returns a Dictionary of all classes which have implemented the <seealso cref="ITrainer"/> interface in your entry assembly.</para>
    /// The key is the <see cref="ITrainer.TrainerName"/> and the value the instantiated Trainer.
    /// </summary>
    /// <returns>A dictionary of all implemented trainers. If no trainers are found, this will return a <c>empty</c> dictionary.</returns>
    public static IDictionary<string, ITrainer> GetAllImplementedTrainers()
    {
        var trainerRegister = new Dictionary<string, ITrainer>();

        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly is null)
        {
            return trainerRegister;
        }

        var implementedTrainers = (from type in entryAssembly.GetTypes()
                                   where type.GetInterfaces().Contains(typeof(ITrainer))
                                         && type.GetConstructor(Type.EmptyTypes) != null
                                   select Activator.CreateInstance(type) as ITrainer).ToList();

        if (!implementedTrainers.Any())
        {
            return trainerRegister;
        }

        foreach (var trainer in implementedTrainers)
        {
            trainerRegister.Add(trainer.TrainerName, trainer);
        }

        return trainerRegister;
    }
}