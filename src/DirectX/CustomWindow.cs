using System.Runtime.InteropServices;

namespace omd2.basics.DirectX;

/// <summary>
/// Creates a temporary window for D3D9 VTable extraction.
/// </summary>
internal class CustomWindow : IDisposable
{
    public IntPtr Hwnd { get; private set; }

    private const int ErrorClassAlreadyExists = 1410;
    private bool _disposed;
    private WndProc _wndProcDelegate;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (Hwnd != IntPtr.Zero)
        {
            DestroyWindow(Hwnd);
            Hwnd = IntPtr.Zero;
        }

        _disposed = true;
    }

    public CustomWindow(string className)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);

        _wndProcDelegate = CustomWndProc;
        var wndClass = new Wndclass
        {
            lpszClassName = className,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate)
        };

        ushort classAtom = RegisterClassW(ref wndClass);
        int lastError = Marshal.GetLastWin32Error();

        if (classAtom == 0 && lastError != ErrorClassAlreadyExists)
            throw new Exception($"Could not register window class. Error: {lastError}");

        Hwnd = CreateWindowExW(0, className, string.Empty, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Wndclass
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    #region Native
    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClassW([In] ref Wndclass lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle,
        [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);
    #endregion
}
