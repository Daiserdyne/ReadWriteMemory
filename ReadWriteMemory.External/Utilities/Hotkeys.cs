using ReadWriteMemory.External.NativeImports;

namespace ReadWriteMemory.External.Utilities;

/// <summary>
/// Allows you to create hotkeys to call your code.
/// </summary>
public static class Hotkeys
{
    #region Enums

    /// <summary>
    /// Virtual-Key Codes.
    /// </summary>
    [Flags]
    public enum Key : int
    {
        Lbutton = 0X01,
        Rbutton = 0X02,
        Mbutton = 0X04,
        Back = 0X08,
        Tab = 0X09,
        Return = 0X0D,
        Shift = 0X10,
        Control = 0X11,
        Pause = 0X13,
        Capital = 0X14,
        Escape = 0X1B,
        Space = 0X20,
        Prior = 0X21,
        Next = 0X22,
        End = 0X23,
        Home = 0X24,
        Left = 0X25,
        Up = 0X26,
        Right = 0X27,
        Down = 0X28,
        Snapshot = 0X2C,
        Insert = 0X2D,
        Delete = 0X2E,

        Zero = 0X30,
        One = 0X31,
        Two = 0X32,
        Three = 0X33,
        Four = 0X34,
        Five = 0X35,
        Six = 0X36,
        Seven = 0X37,
        Eight = 0X38,
        Nine = 0X39,

        A = 0X41,
        B = 0X42,
        C = 0X43,
        D = 0X44,
        E = 0X45,
        F = 0X46,
        G = 0X47,
        H = 0X48,
        I = 0X49,
        J = 0X4A,
        K = 0X4B,
        L = 0X4C,
        M = 0X4D,
        N = 0X4E,
        O = 0X4F,
        P = 0X50,
        Q = 0X51,
        R = 0X52,
        S = 0X53,
        T = 0X54,
        U = 0X55,
        V = 0X56,
        W = 0X57,
        X = 0X58,
        Y = 0X59,
        Z = 0X5A,

        Numpad0 = 0X60,
        Numpad1 = 0X61,
        Numpad2 = 0X62,
        Numpad3 = 0X63,
        Numpad4 = 0X64,
        Numpad5 = 0X65,
        Numpad6 = 0X66,
        Numpad7 = 0X67,
        Numpad8 = 0X68,
        Numpad9 = 0X69,

        Add = 0X6B,
        Seperator = 0X6C,
        Subtract = 0X6D,
        Decimal = 0X6E,
        Divide = 0X6F,

        F1 = 0X70,
        F2 = 0X71,
        F3 = 0X72,
        F4 = 0X73,
        F5 = 0X74,
        F6 = 0X75,
        F7 = 0X76,
        F8 = 0X77,
        F9 = 0X78,
        F10 = 0X79,
        F11 = 0X7A,
        F12 = 0X7B,

        Numlock = 0X90,
        Scroll = 0X91,
        Lshift = 0XA0,
        Rshift = 0XA1,
        Lcontrol = 0XA2,
        Rcontrol = 0XA3
    }

    #endregion

    /// <summary>
    /// Determines whether a key is up or down at the time the function is called, and whether the 
    /// key was pressed.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async ValueTask<bool> KeyPressedAsync(Key key, bool waitForKeyRelease = true)
    {
        if (User32.GetAsyncKeyState(key) < 0)
        {
            if (!waitForKeyRelease)
            {
                return true;
            }

            while (User32.GetAsyncKeyState(key) < 0)
            {
                await Task.Delay(1);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a key is up or down at the time the function is called, and whether the 
    /// key was pressed.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async ValueTask<bool> KeyPressedAsync(int key, bool waitForKeyRelease = true)
    {
        if (User32.GetAsyncKeyState(key) < 0)
        {
            if (!waitForKeyRelease)
            {
                return true;
            }

            while (User32.GetAsyncKeyState(key) < 0)
            {
                await Task.Delay(1);
            }

            return true;
        }

        return false;
    }
}