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

    /// <summary>
    /// Virtual-Key Codes.
    /// </summary>
    public enum Hotkey : int
    {
        /// <summary>
        /// Left mouse button.
        /// </summary>
        VK_LBUTTON = 0x01,
        /// <summary>
        /// Right mouse button.
        /// </summary>
        VK_RBUTTON = 0x02,
        /// <summary>
        /// F1 key.
        /// </summary>
        VK_F1 = 0x70
    }

    /// <summary>
    /// Determines whether a key is up or down at the time the function is called, and whether the 
    /// key was pressed.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async Task<bool> HotKeyPressedAsync(Hotkey key, bool waitForKeyRelease = true)
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
    /// <param name="hotkey"></param>
    /// <param name="waitForKeyRelease"></param>
    public static async Task<bool> HotKeyPressedAsync(int hotkey, bool waitForKeyRelease = true)
    {
        if (GetAsyncKeyState(hotkey) < 0)
        {
            if (!waitForKeyRelease)
                return true;

            while (GetAsyncKeyState(hotkey) < 0)
                await Task.Delay(1);

            return true;
        }

        return false;
    }
}