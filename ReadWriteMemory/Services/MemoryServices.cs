using ReadWriteMemory.Interfaces;
using System.Reflection;

namespace ReadWriteMemory.Services;

public static class MemoryServices
{
    private static IEnumerable<ITrainer>? _implementedTrainers;

    /// <summary>
    /// Returns a collection of all classes which have implemented the <c>ITrainer</c> interface in your assembly.
    /// </summary>
    public static IEnumerable<ITrainer> ImplementedTrainers
    {
        get
        {
            if (_implementedTrainers is null || _implementedTrainers.Count() == 0)
                _implementedTrainers = GetAllImplementedTrainers();

            return _implementedTrainers;
        }
    }

    private static IEnumerable<ITrainer> GetAllImplementedTrainers()
    {

        var implementedTrainers = from t in Assembly.GetExecutingAssembly().GetTypes()
                                  where t.GetInterfaces().Contains(typeof(ITrainer))
                                        && t.GetConstructor(Type.EmptyTypes) != null
                                  select Activator.CreateInstance(t) as ITrainer;

        return implementedTrainers ?? Enumerable.Empty<ITrainer>();
    }
}