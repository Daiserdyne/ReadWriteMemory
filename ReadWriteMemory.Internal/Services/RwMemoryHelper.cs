namespace ReadWriteMemory.Internal.Services;

/// <summary>
/// Contains some useful trainer helper-methods.
/// </summary>
public static class RwMemoryHelper
{
    private static object? _threadObject;
    private static RwMemory? _memory;

    /// <summary>
    /// Gives you a <see cref="Internal.RwMemory"/> instance which you have created
    /// before with the <see cref="CreateAndGetSingletonInstance()"/> function.
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    public static RwMemory RwMemory => _memory ?? CreateAndGetSingletonInstance();

    /// <summary>
    /// Gives you a thread-safe singleton instance of the <see cref="Internal.RwMemory"/> object.
    /// </summary>
    /// <returns></returns>
    private static RwMemory CreateAndGetSingletonInstance()
    {
        _threadObject ??= new();

        lock (_threadObject)
        {
            return _memory ??= new();
        }
    }
}