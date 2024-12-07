using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// Calls a function inside the process. You just have to know the signature of the function.
    /// </summary>
    /// <param name="memoryAddress"></param>
    public unsafe void CallFunction(MemoryAddress memoryAddress)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<void>)targetAddress;

        func();
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <typeparam name="TArg1"></typeparam>
    public unsafe void CallFunction<TArg1>(MemoryAddress memoryAddress, TArg1 arg1)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, void>)targetAddress;

        func(arg1);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, void>)targetAddress;

        func(arg1, arg2);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2,
        TArg3 arg3)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, void>)targetAddress;

        func(arg1, arg2, arg3);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;

        func(arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    /// <typeparam name="TArg8"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7, TArg8 arg8)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <param name="arg9"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    /// <typeparam name="TArg8"></typeparam>
    /// <typeparam name="TArg9"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func =
            (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <param name="arg9"></param>
    /// <param name="arg10"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    /// <typeparam name="TArg8"></typeparam>
    /// <typeparam name="TArg9"></typeparam>
    /// <typeparam name="TArg10"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
        TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func =
            (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, void>)
            targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<TResult>(MemoryAddress memoryAddress)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TResult>)targetAddress;

        return func();
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, TResult>(MemoryAddress memoryAddress, T1 arg1)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, TResult>)targetAddress;

        return func(arg1);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, TResult>)targetAddress;

        return func(arg1, arg2);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, TResult>)targetAddress;

        return func(arg1, arg2, arg3);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, T4, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3,
        T4 arg4)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, T6, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <param name="arg9"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <param name="arg6"></param>
    /// <param name="arg7"></param>
    /// <param name="arg8"></param>
    /// <param name="arg9"></param>
    /// <param name="arg10"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <typeparam name="T10"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult? CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>)targetAddress;

        return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
}