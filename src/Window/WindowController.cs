using System.Runtime.InteropServices;
using omd2.basics.Configuration;
using Reloaded.Mod.Interfaces;

namespace omd2.basics.Window;

/// <summary>
/// Controls window sizing to match the configured resolution.
/// </summary>
public class WindowController(Config config, ILogger logger)
{
    private const string GameWindowClass = "VVideoClass";

    /// <summary>
    /// Resizes the game window to match the specified resolution.
    /// </summary>
    public void ResizeGameWindow(int width, int height)
    {
        var hwnd = FindWindowW(GameWindowClass, null);
        if (hwnd == IntPtr.Zero)
        {
            logger.WriteLine("[WindowController] Game window not found");
            return;
        }

        ResizeWindow(hwnd, width, height);
    }

    /// <summary>
    /// Resizes a window to the specified client area size, centered on screen.
    /// </summary>
    public void ResizeWindow(IntPtr hwnd, int clientWidth, int clientHeight)
    {
        if (hwnd == IntPtr.Zero) return;

        _ = config; // Available for future use
        
        // Get current window style to calculate border sizes
        int style = GetWindowLong(hwnd, GWL_STYLE);
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        bool hasMenu = GetMenu(hwnd) != IntPtr.Zero;

        // Calculate window size including borders
        var rect = new RECT { Left = 0, Top = 0, Right = clientWidth, Bottom = clientHeight };
        AdjustWindowRectEx(ref rect, style, hasMenu, exStyle);

        int windowWidth = rect.Right - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;

        // Get screen dimensions for centering
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        // Center the window
        int x = (screenWidth - windowWidth) / 2;
        int y = (screenHeight - windowHeight) / 2;

        // Apply the new size and position
        SetWindowPos(hwnd, IntPtr.Zero, x, y, windowWidth, windowHeight, SWP_NOZORDER | SWP_NOACTIVATE);

        logger.WriteLine($"[WindowController] Window resized to {clientWidth}x{clientHeight} (window: {windowWidth}x{windowHeight}) at ({x}, {y})");
    }

    #region Native Methods
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMenu(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    #endregion
}
