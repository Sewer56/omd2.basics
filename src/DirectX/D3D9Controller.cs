using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using SharpDX.Direct3D9;

namespace omd2.basics.DirectX;

/// <summary>
/// Controls D3D9 device creation with resolution override and D3D9Ex upgrade.
/// </summary>
public unsafe class D3D9Controller : IDisposable
{
    private readonly ILogger _logger;
    private readonly Action<int, int> _onResolutionChanged;

    private DX9Hook? _dx9Hook;
    private Direct3DEx? _d3dEx;
    private DeviceEx? _deviceEx;
    
    private IHook<DX9Hook.CreateDevice>? _createDeviceHook;
    private IHook<DX9Hook.Reset>? _resetHook;
    private IHook<DX9Hook.CreateTexture>? _createTextureHook;
    private IHook<DX9Hook.CreateVertexBuffer>? _createVertexBufferHook;
    private IHook<DX9Hook.CreateIndexBuffer>? _createIndexBufferHook;

    private int _currentWidth;
    private int _currentHeight;

    /// <summary>
    /// The current render width.
    /// </summary>
    public int CurrentWidth => _currentWidth;

    /// <summary>
    /// The current render height.
    /// </summary>
    public int CurrentHeight => _currentHeight;

    /// <summary>
    /// The game window handle (found after device creation).
    /// </summary>
    public IntPtr GameWindowHandle { get; private set; }

    public D3D9Controller(IReloadedHooks hooks, ILogger logger, Action<int, int> onResolutionChanged)
    {
        _logger = logger;
        _onResolutionChanged = onResolutionChanged;

        try
        {
            _dx9Hook = new DX9Hook(hooks);
            
            _createDeviceHook = _dx9Hook.Direct3D9VTable
                .CreateFunctionHook<DX9Hook.CreateDevice>((int)IDirect3D9Methods.CreateDevice, CreateDeviceHook)
                .Activate();
            
            _resetHook = _dx9Hook.DeviceVTable
                .CreateFunctionHook<DX9Hook.Reset>((int)IDirect3DDevice9Methods.Reset, ResetHook)
                .Activate();

            // D3D9Ex compatibility hooks - convert Pool.Managed to Pool.Default
            _createTextureHook = _dx9Hook.DeviceVTable
                .CreateFunctionHook<DX9Hook.CreateTexture>((int)IDirect3DDevice9Methods.CreateTexture, CreateTextureHook)
                .Activate();

            _createVertexBufferHook = _dx9Hook.DeviceVTable
                .CreateFunctionHook<DX9Hook.CreateVertexBuffer>((int)IDirect3DDevice9Methods.CreateVertexBuffer, CreateVertexBufferHook)
                .Activate();

            _createIndexBufferHook = _dx9Hook.DeviceVTable
                .CreateFunctionHook<DX9Hook.CreateIndexBuffer>((int)IDirect3DDevice9Methods.CreateIndexBuffer, CreateIndexBufferHook)
                .Activate();

            _logger.WriteLine("[D3D9Controller] Hooks installed successfully");
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[D3D9Controller] Failed to initialize: {ex.Message}");
            throw;
        }
    }

    private int CreateDeviceHook(
        IntPtr direct3DPointer,
        uint adapter,
        DeviceType deviceType,
        IntPtr hFocusWindow,
        CreateFlags behaviorFlags,
        ref PresentParameters presentParameters,
        IntPtr* ppReturnedDeviceInterface)
    {
        // Create our own Direct3DEx instance
        _d3dEx = new Direct3DEx();
        
        // Get resolution - use config values or fall back to desktop resolution
        GetTargetResolution(out int width, out int height);
        _currentWidth = width;
        _currentHeight = height;
        
        _logger.WriteLine($"[D3D9Controller] CreateDevice intercepted - Target resolution: {width}x{height}");

        // Store window handle
        GameWindowHandle = hFocusWindow;

        // Apply our settings to present parameters
        ApplyPresentParameters(ref presentParameters, width, height);

        // Add hardware vertex processing if not already set
        if ((behaviorFlags & CreateFlags.HardwareVertexProcessing) == 0 &&
            (behaviorFlags & CreateFlags.SoftwareVertexProcessing) == 0)
        {
            behaviorFlags |= CreateFlags.HardwareVertexProcessing;
        }

        try
        {
            _deviceEx = new DeviceEx(_d3dEx, (int)adapter, deviceType, hFocusWindow, behaviorFlags, presentParameters);

            *ppReturnedDeviceInterface = _deviceEx.NativePointer;
            _logger.WriteLine($"[D3D9Controller] D3D9Ex device created successfully at {width}x{height}");

            // Notify about resolution
            _onResolutionChanged(width, height);

            return 0; // S_OK
        }
        catch (SharpDX.SharpDXException ex)
        {
            _logger.WriteLine($"[D3D9Controller] Failed to create D3D9Ex device: {ex.Message}");
            
            // Fall back to original function
            _d3dEx?.Dispose();
            _d3dEx = null;
            return _createDeviceHook!.OriginalFunction(direct3DPointer, adapter, deviceType, hFocusWindow, 
                behaviorFlags, ref presentParameters, ppReturnedDeviceInterface);
        }
    }

    private int ResetHook(IntPtr device, ref PresentParameters presentParameters)
    {
        // Only intercept our device
        if (_deviceEx == null || device != _deviceEx.NativePointer)
        {
            return _resetHook!.OriginalFunction(device, ref presentParameters);
        }

        GetTargetResolution(out int width, out int height);
        
        bool resolutionChanged = width != _currentWidth || height != _currentHeight;
        _currentWidth = width;
        _currentHeight = height;

        _logger.WriteLine($"[D3D9Controller] Reset intercepted - Resolution: {width}x{height}");

        ApplyPresentParameters(ref presentParameters, width, height);

        var result = _resetHook!.OriginalFunction(device, ref presentParameters);

        if (result == 0 && resolutionChanged) // S_OK
        {
            _onResolutionChanged(width, height);
        }

        return result;
    }

    private static void ApplyPresentParameters(ref PresentParameters pp, int width, int height)
    {
        var config = Mod.Configuration;
        
        pp.BackBufferWidth = width;
        pp.BackBufferHeight = height;
        pp.Windowed = true;

        // D3D9Ex requires these settings for windowed mode
        pp.SwapEffect = SwapEffect.FlipEx;
        pp.BackBufferCount = 2;
        pp.MultiSampleType = MultisampleType.None;
        pp.MultiSampleQuality = 0;
        
        // VSync control - only apply if override is enabled
        if (config.OverrideFpsLimit)
            pp.PresentationInterval = config.VSync ? PresentInterval.One : PresentInterval.Immediate;
    }

    private void GetTargetResolution(out int width, out int height)
    {
        var config = Mod.Configuration;
        if (config.OverrideResolution && config.ResolutionWidth > 0 && config.ResolutionHeight > 0)
        {
            width = config.ResolutionWidth;
            height = config.ResolutionHeight;
        }
        else
        {
            // Use desktop resolution
            width = GetSystemMetrics(SM_CXSCREEN);
            height = GetSystemMetrics(SM_CYSCREEN);
        }
    }

    #region D3D9Ex Compatibility Hooks
    
    // D3D9Ex doesn't support Pool.Managed, so we convert to Pool.Default with Usage.Dynamic

    private int CreateTextureHook(IntPtr devicePointer, int width, int height, int levels, 
        Usage usage, Format format, Pool pool, void** ppTexture, void* sharedHandle)
    {
        if (pool == Pool.Managed)
        {
            pool = Pool.Default;
            usage |= Usage.Dynamic;
        }
        return _createTextureHook!.OriginalFunction(devicePointer, width, height, levels, usage, format, pool, ppTexture, sharedHandle);
    }

    private int CreateVertexBufferHook(IntPtr devicePointer, uint length, Usage usage, 
        VertexFormat vertexFormat, Pool pool, void** ppVertexBuffer, void* sharedHandle)
    {
        if (pool == Pool.Managed)
        {
            pool = Pool.Default;
            usage |= Usage.Dynamic;
        }
        return _createVertexBufferHook!.OriginalFunction(devicePointer, length, usage, vertexFormat, pool, ppVertexBuffer, sharedHandle);
    }

    private int CreateIndexBufferHook(IntPtr devicePointer, uint length, Usage usage, 
        Format format, Pool pool, void** ppIndexBuffer, void* sharedHandle)
    {
        if (pool == Pool.Managed)
        {
            pool = Pool.Default;
            usage |= Usage.Dynamic;
        }
        return _createIndexBufferHook!.OriginalFunction(devicePointer, length, usage, format, pool, ppIndexBuffer, sharedHandle);
    }

    #endregion

    public void Dispose()
    {
        _createDeviceHook?.Disable();
        _resetHook?.Disable();
        _createTextureHook?.Disable();
        _createVertexBufferHook?.Disable();
        _createIndexBufferHook?.Disable();
        
        _deviceEx?.Dispose();
        _d3dEx?.Dispose();
    }

    #region Native
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    #endregion
}
