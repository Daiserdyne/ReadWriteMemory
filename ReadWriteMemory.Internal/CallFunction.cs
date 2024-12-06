using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    public unsafe void CallFunction(MemoryAddress memoryAddress)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<void>)targetAddress;

        func();
    }

    public unsafe void CallFunction<TArg1>(MemoryAddress memoryAddress, TArg1 arg1)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, void>)targetAddress;

        func(arg1);
    }

    public unsafe void CallFunction<TArg1, TArg2>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, void>)targetAddress;

        func(arg1, arg2);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2,
        TArg3 arg3)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, void>)targetAddress;

        func(arg1, arg2, arg3);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;

        func(arg1, arg2, arg3, arg4);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7, TArg8 arg8)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(
        MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func =
            (delegate* unmanaged<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, void>)targetAddress;

        func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

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

    public unsafe TResult CallFunction<TResult>(MemoryAddress memoryAddress)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var func = (delegate* unmanaged<TResult>)targetAddress;

        return func();
    }

    public unsafe TResult CallFunction<T1, TResult>(MemoryAddress memoryAddress, T1 arg1)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit einem Parameter aufrufen
        return ((delegate* unmanaged<T1, TResult>)targetAddress)(arg1);
    }

    public unsafe TResult CallFunction<T1, T2, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit zwei Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, TResult>)targetAddress)(arg1, arg2);
    }

    public unsafe TResult CallFunction<T1, T2, T3, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit drei Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, TResult>)targetAddress)(arg1, arg2, arg3);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3,
        T4 arg4)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit vier Parametern aufrufen
        return ((delegate* unmanaged[Stdcall]<T1, T2, T3, T4, TResult>)targetAddress)(arg1, arg2, arg3, arg4);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit fünf Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, TResult>)targetAddress)(arg1, arg2, arg3, arg4, arg5);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit sechs Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, T6, TResult>)targetAddress)(arg1, arg2, arg3, arg4, arg5,
            arg6);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit sieben Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress)(arg1, arg2, arg3, arg4, arg5,
            arg6, arg7);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit acht Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, TResult>)targetAddress)(arg1, arg2, arg3, arg4,
            arg5, arg6, arg7, arg8);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit neun Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>)targetAddress)(arg1, arg2, arg3, arg4,
            arg5, arg6, arg7, arg8, arg9);
    }

    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        // Funktionszeiger mit zehn Parametern aufrufen
        return ((delegate* unmanaged<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>)targetAddress)(arg1, arg2, arg3,
            arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
}