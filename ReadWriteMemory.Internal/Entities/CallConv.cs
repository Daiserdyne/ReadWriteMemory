namespace ReadWriteMemory.Internal.Entities;

/// <summary>
/// Represents the different calling conventions used for unmanaged function calls.
/// </summary>
public enum CallConv : byte
{
    /// <summary>
    /// The C calling convention (Cdecl). 
    /// Parameters are pushed onto the stack from right to left, 
    /// and the caller cleans up the stack after the function call.
    /// Commonly used in C/C++ libraries.
    /// </summary>
    Cdecl,

    /// <summary>
    /// The standard calling convention (Stdcall). 
    /// Parameters are pushed onto the stack from right to left, 
    /// and the called function cleans up the stack.
    /// Commonly used in Windows API functions.
    /// </summary>
    Stdcall,

    /// <summary>
    /// The thiscall calling convention (ThisCall). 
    /// Used for C++ instance methods, where the "this" pointer 
    /// is passed in a register (typically ECX on x86). 
    /// Parameters are pushed onto the stack like Cdecl.
    /// </summary>
    ThisCall,

    /// <summary>
    /// The fastcall calling convention (Fastcall). 
    /// The first few parameters are passed in registers (e.g., ECX, EDX on x86),
    /// with the rest pushed onto the stack.
    /// Designed for performance-critical functions.
    /// </summary>
    Fastcall,

    /// <summary>
    /// The Suppress GC Transition calling convention (SuppressGcTransition). 
    /// Optimizes performance by skipping the transition to a garbage collection-safe state 
    /// during a function call. Should only be used for short and safe operations.
    /// </summary>
    SuppressGcTransition
}