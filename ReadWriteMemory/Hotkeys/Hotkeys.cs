using System.Runtime.InteropServices;

namespace ReadWriteMemory.Hotkeys;

/// <summary>
/// Allows you to create hotkeys to call your code.
/// </summary>
public static partial class Hotkeys
{
    #region Native Methods

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int key);

    #endregion

    #region Enums

    /// <summary>
    /// Virtual-Key Codes.
    /// </summary>
    public enum Key : int
    {
        VK_LBUTTON = 0X01,
        VK_RBUTTON = 0X02,
        VK_MBUTTON = 0X04,
        VK_BACK = 0X08,
        VK_TAB = 0X09,
        VK_RETURN = 0X0D,
        VK_SHIFT = 0X10,
        VK_CONTROL = 0X11,
        VK_PAUSE = 0X13,
        VK_CAPITAL = 0X14,
        VK_ESCAPE = 0X1B,
        VK_SPACE = 0X20,
        VK_PRIOR = 0X21,
        VK_NEXT = 0X22,
        VK_END = 0X23,
        VK_HOME = 0X24,
        VK_LEFT = 0X25,
        VK_UP = 0X26,
        VK_RIGHT = 0X27,
        VK_DOWN = 0X28,
        VK_SNAPSHOT = 0X2C,
        VK_INSERT = 0X2D,
        VK_DELETE = 0X2E,

        VK_0 = 0X30,
        VK_1 = 0X31,
        VK_2 = 0X32,
        VK_3 = 0X33,
        VK_4 = 0X34,
        VK_5 = 0X35,
        VK_6 = 0X36,
        VK_7 = 0X37,
        VK_8 = 0X38,
        VK_9 = 0X39,

        VK_A = 0X41,
        VK_B = 0X42,
        VK_C = 0X43,
        VK_D = 0X44,
        VK_E = 0X45,
        VK_F = 0X46,
        VK_G = 0X47,
        VK_H = 0X48,
        VK_I = 0X49,
        VK_J = 0X4A,
        VK_K = 0X4B,
        VK_L = 0X4C,
        VK_M = 0X4D,
        VK_N = 0X4E,
        VK_O = 0X4F,
        VK_P = 0X50,
        VK_Q = 0X51,
        VK_R = 0X52,
        VK_S = 0X53,
        VK_T = 0X54,
        VK_U = 0X55,
        VK_V = 0X56,
        VK_W = 0X57,
        VK_X = 0X58,
        VK_Y = 0X59,
        VK_Z = 0X5A,

        VK_NUMPAD0 = 0X60,
        VK_NUMPAD1 = 0X61,
        VK_NUMPAD2 = 0X62,
        VK_NUMPAD3 = 0X63,
        VK_NUMPAD4 = 0X64,
        VK_NUMPAD5 = 0X65,
        VK_NUMPAD6 = 0X66,
        VK_NUMPAD7 = 0X67,
        VK_NUMPAD8 = 0X68,
        VK_NUMPAD9 = 0X69,

        VK_ADD = 0X6B,
        VK_SEPERATOR = 0X6C,
        VK_SUBTRACT = 0X6D,
        VK_DECIMAL = 0X6E,
        VK_DIVIDE = 0X6F,

        VK_F1 = 0X70,
        VK_F2 = 0X71,
        VK_F3 = 0X72,
        VK_F4 = 0X73,
        VK_F5 = 0X74,
        VK_F6 = 0X75,
        VK_F7 = 0X76,
        VK_F8 = 0X77,
        VK_F9 = 0X78,
        VK_F10 = 0X79,
        VK_F11 = 0X7A,
        VK_F12 = 0X7B,
        VK_NUMLOCK = 0X90,
        VK_SCROLL = 0X91,
        VK_LSHIFT = 0XA0,
        VK_RSHIFT = 0XA1,
        VK_LCONTROL = 0XA2,
        VK_RCONTROL = 0XA3,
    }

    #endregion

    /// <summary>
    /// Determines whether a key is up or down at the time the function is called, and whether the 
    /// key was pressed.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async Task<bool> KeyPressedAsync(Key key, bool waitForKeyRelease = true)
    {
        int targetKey = (int) key;

        if (GetAsyncKeyState(targetKey) < 0)
        {
            if (!waitForKeyRelease)
                return true;

            while (GetAsyncKeyState(targetKey) < 0)
                await Task.Delay(1);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a key is up or down at the time the function is called, and whether the 
    /// key was pressed.
    /// </summary>
    /// <param name="keyCode"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async Task<bool> KeyPressedAsync(int keyCode, bool waitForKeyRelease = true)
    {
        if (GetAsyncKeyState(keyCode) < 0)
        {
            if (!waitForKeyRelease)
                return true;

            while (GetAsyncKeyState(keyCode) < 0)
                await Task.Delay(1);

            return true;
        }

        return false;
    }
}