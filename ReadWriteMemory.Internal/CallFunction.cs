using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// Calls a function inside the process. You have to know the signature of the function and the calling convention
    /// to call the function properly.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="callConv"></param>
    public unsafe void CallFunction(MemoryAddress memoryAddress, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<void>)targetAddress;
                func();
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<void>)targetAddress;
                func();
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<void>)targetAddress;
                func();
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<void>)targetAddress;
                func();
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<void>)targetAddress;
                func();
                return;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    public unsafe void CallFunction<TArg1>(MemoryAddress memoryAddress, TArg1 arg1, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, void>)targetAddress;
                func(arg1);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, void>)targetAddress;
                func(arg1);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, void>)targetAddress;
                func(arg1);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, void>)targetAddress;
                func(arg1);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<TArg1, void>)targetAddress;
                func(arg1);
                return;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, TArg2, void>)targetAddress;
                func(arg1, arg2);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, TArg2, void>)targetAddress;
                func(arg1, arg2);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, TArg2, void>)targetAddress;
                func(arg1, arg2);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, TArg2, void>)targetAddress;
                func(arg1, arg2);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, void>)targetAddress;
                func(arg1, arg2);
                return;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg3"></param>
    /// <param name="callConv"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2,
        TArg3 arg3, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, void>)targetAddress;
                func(arg1, arg2, arg3);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, void>)targetAddress;
                func(arg1, arg2, arg3);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, void>)targetAddress;
                func(arg1, arg2, arg3);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, void>)targetAddress;
                func(arg1, arg2, arg3);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, void>)targetAddress;
                func(arg1, arg2, arg3);
                return;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4>(MemoryAddress memoryAddress, TArg1 arg1, TArg2 arg2,
        TArg3 arg3, TArg4 arg4, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;
                func(arg1, arg2, arg3, arg4);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;
                func(arg1, arg2, arg3, arg4);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;
                func(arg1, arg2, arg3, arg4);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;
                func(arg1, arg2, arg3, arg4);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4, void>)targetAddress;
                func(arg1, arg2, arg3, arg4);
                return;
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4, TArg5, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5);
                return;
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(MemoryAddress memoryAddress, TArg1 arg1,
        TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4,
                    TArg5, TArg6, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return;
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4,
                    TArg5, TArg6, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return;
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4,
                    TArg5, TArg6, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return;
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4,
                    TArg5, TArg6, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4,
                        TArg5, TArg6, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return;
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(MemoryAddress memoryAddress,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func =
                    (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return;
            }
            case CallConv.Stdcall:
            {
                var func =
                    (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return;
            }
            case CallConv.ThisCall:
            {
                var func =
                    (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return;
            }
            case CallConv.Fastcall:
            {
                var func =
                    (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4,
                        TArg5, TArg6, TArg7, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return;
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="TArg1"></typeparam>
    /// <typeparam name="TArg2"></typeparam>
    /// <typeparam name="TArg3"></typeparam>
    /// <typeparam name="TArg4"></typeparam>
    /// <typeparam name="TArg5"></typeparam>
    /// <typeparam name="TArg6"></typeparam>
    /// <typeparam name="TArg7"></typeparam>
    /// <typeparam name="TArg8"></typeparam>
    public unsafe void CallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(MemoryAddress memoryAddress,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func =
                    (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return;
            }
            case CallConv.Stdcall:
            {
                var func =
                    (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return;
            }
            case CallConv.ThisCall:
            {
                var func =
                    (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return;
            }
            case CallConv.Fastcall:
            {
                var func =
                    (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4,
                        TArg5, TArg6, TArg7, TArg8, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return;
            }
        }
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
    /// <param name="callConv"></param>
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
        MemoryAddress memoryAddress,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func =
                    (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return;
            }
            case CallConv.Stdcall:
            {
                var func =
                    (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return;
            }
            case CallConv.ThisCall:
            {
                var func =
                    (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return;
            }
            case CallConv.Fastcall:
            {
                var func =
                    (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4,
                        TArg5, TArg6, TArg7, TArg8, TArg9, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return;
            }
        }
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
    /// <param name="callConv"></param>
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
        MemoryAddress memoryAddress,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9,
        TArg10 arg10, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            case CallConv.Cdecl:
            {
                var func =
                    (delegate* unmanaged[Cdecl]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, TArg10, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
                return;
            }
            case CallConv.Stdcall:
            {
                var func =
                    (delegate* unmanaged[Stdcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, TArg10, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
                return;
            }
            case CallConv.ThisCall:
            {
                var func =
                    (delegate* unmanaged[Thiscall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, TArg10, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
                return;
            }
            case CallConv.Fastcall:
            {
                var func =
                    (delegate* unmanaged[Fastcall]<TArg1, TArg2, TArg3, TArg4, TArg5,
                        TArg6, TArg7, TArg8, TArg9, TArg10, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
                return;
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<TArg1, TArg2, TArg3, TArg4,
                        TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, void>)targetAddress;
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
                return;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<TResult>(MemoryAddress memoryAddress, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<TResult>)targetAddress;

                return func();
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<TResult>)targetAddress;

                return func();
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<TResult>)targetAddress;

                return func();
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<TResult>)targetAddress;

                return func();
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<TResult>)targetAddress;

                return func();
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, TResult>(MemoryAddress memoryAddress, T1 arg1,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, TResult>)targetAddress;

                return func(arg1);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, TResult>)targetAddress;

                return func(arg1);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, TResult>)targetAddress;

                return func(arg1);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, TResult>)targetAddress;

                return func(arg1);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, TResult>)targetAddress;

                return func(arg1);
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, TResult>)targetAddress;

                return func(arg1, arg2);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, TResult>)targetAddress;

                return func(arg1, arg2);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, TResult>)targetAddress;

                return func(arg1, arg2);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, TResult>)targetAddress;

                return func(arg1, arg2);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, TResult>)targetAddress;

                return func(arg1, arg2);
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, TResult>)targetAddress;

                return func(arg1, arg2, arg3);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, TResult>)targetAddress;

                return func(arg1, arg2, arg3);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, TResult>)targetAddress;

                return func(arg1, arg2, arg3);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, TResult>)targetAddress;

                return func(arg1, arg2, arg3);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, TResult>)targetAddress;

                return func(arg1, arg2, arg3);
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="CallFunction"/>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, T4, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2, T3 arg3,
        T4 arg4, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4);
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5);
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, TResult>(MemoryAddress memoryAddress, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4, T5 arg5, T6 arg6, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, T6, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
        }
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
    /// <param name="callConv"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, T6, T7, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }
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
    /// <param name="callConv"></param>
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
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(MemoryAddress memoryAddress, T1 arg1,
        T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, T6, 
                    T7, T8, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, T6,
                    T7, T8, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, T6, 
                    T7, T8, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, T6, 
                    T7, T8, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, 
                        T6, T7, T8, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
        }
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
    /// <param name="callConv"></param>
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
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, T6, T7, 
                    T8, T9, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            case CallConv.Stdcall:
            {
                var func = (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, T6, T7, 
                    T8, T9, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            case CallConv.ThisCall:
            {
                var func = (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, T6, T7,
                    T8, T9, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            case CallConv.Fastcall:
            {
                var func = (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, T6, T7, 
                    T8, T9, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            case CallConv.SuppressGcTransition:
            {
                var func = (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, T6, 
                        T7, T8, T9, TResult>)
                    targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
        }
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
    /// <param name="callConv"></param>
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
    public unsafe TResult CallFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(MemoryAddress memoryAddress,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
        CallConv callConv = CallConv.Cdecl)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        switch (callConv)
        {
            default:
            case CallConv.Cdecl:
            {
                var func = (delegate* unmanaged[Cdecl]<T1, T2, T3, T4, T5, T6, T7, 
                    T8, T9, T10, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            case CallConv.Stdcall:
            {
                var func =
                    (delegate* unmanaged[Stdcall]<T1, T2, T3, T4, T5, T6, T7, T8, 
                        T9, T10, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            case CallConv.ThisCall:
            {
                var func =
                    (delegate* unmanaged[Thiscall]<T1, T2, T3, T4, T5, T6, T7, T8, 
                        T9, T10, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            case CallConv.Fastcall:
            {
                var func =
                    (delegate* unmanaged[Fastcall]<T1, T2, T3, T4, T5, T6, T7, T8, 
                        T9, T10, TResult>)targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            case CallConv.SuppressGcTransition:
            {
                var func =
                    (delegate* unmanaged[SuppressGCTransition]<T1, T2, T3, T4, T5, 
                        T6, T7, T8, T9, T10, TResult>)
                    targetAddress;

                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
        }
    }
}