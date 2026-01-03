using System.Diagnostics;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Mod.Interfaces;

namespace omd2.basics.Graphics;

/// <summary>
/// Hooks Vision engine's VisVideo_cl::SetMode to override resolution at engine initialization.
/// 
/// This intercepts the VVideoConfig structure before it's passed to VVideo::InitializeScreen,
/// ensuring the game's internal logic uses the correct resolution from the start.
/// 
/// VVideoConfig structure layout (from reverse engineering):
///   +0x08: width (DWORD)
///   +0x0C: height (DWORD)
///   +0x10: refresh rate (DWORD)
///   +0x1C: frontbuffer bpp (DWORD)
///   +0x48: fullscreen flag (BYTE)
/// </summary>
public unsafe class VisionVideoController : IDisposable
{
    private const string TargetModuleName = "Vision90.dll";

    // Signature for VisVideo_cl::SetMode
    private const string SetModeSignature = 
        "6A ?? 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC ?? 53 55 56 57 A1 ?? ?? ?? ?? 33 C4 50 8D 44 24 ?? 64 A3 ?? ?? ?? ?? 8B F1 8B 7C 24";

    private readonly ILogger _logger;
    private readonly IReloadedHooks _hooks;
    private readonly IScannerFactory _scannerFactory;
    private readonly Action<int, int>? _onResolutionApplied;

    private IHook<SetModeDelegate>? _setModeHook;
    private nuint _hookAddress;

    /// <summary>
    /// Delegate for VisVideo_cl::SetMode
    /// int __thiscall SetMode(VVideoConfig* config)
    /// </summary>
    [Function(CallingConventions.MicrosoftThiscall)]
    private delegate int SetModeDelegate(IntPtr thisPtr, IntPtr videoConfig);

    public VisionVideoController(
        IReloadedHooks hooks,
        IScannerFactory scannerFactory,
        ILogger logger,
        Action<int, int>? onResolutionApplied = null)
    {
        _hooks = hooks;
        _scannerFactory = scannerFactory;
        _logger = logger;
        _onResolutionApplied = onResolutionApplied;

        // Vision90.dll should already be loaded by the time we initialize
        var moduleInfo = GetModuleInfo(TargetModuleName);
        if (moduleInfo.HasValue)
        {
            _logger.WriteLine($"[VisionVideo] {TargetModuleName} found at 0x{moduleInfo.Value.BaseAddress:X}");
            ScanAndInstallHook(moduleInfo.Value.BaseAddress, moduleInfo.Value.Size);
        }
        else
        {
            _logger.WriteLine($"[VisionVideo] ERROR: {TargetModuleName} not loaded!");
        }
    }

    private void ScanAndInstallHook(nuint baseAddress, int size)
    {
        try
        {
            _logger.WriteLine($"[VisionVideo] Scanning {TargetModuleName} for SetMode...");

            using var scanner = _scannerFactory.CreateScanner((byte*)baseAddress, size);
            var result = scanner.FindPattern(SetModeSignature);

            if (!result.Found)
            {
                _logger.WriteLine("[VisionVideo] SetMode pattern not found");
                _logger.WriteLine($"[VisionVideo] Pattern: {SetModeSignature}");
                return;
            }

            _hookAddress = baseAddress + (nuint)result.Offset;

            _logger.WriteLine($"[VisionVideo] SetMode found at offset 0x{result.Offset:X}");
            _logger.WriteLine($"[VisionVideo] Absolute address: 0x{_hookAddress:X}");

            InstallHook();
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[VisionVideo] Failed to scan: {ex.Message}");
            _logger.WriteLine(ex.StackTrace ?? "");
        }
    }

    private void InstallHook()
    {
        try
        {
            _setModeHook = _hooks
                .CreateHook<SetModeDelegate>(SetModeHookImpl, (long)_hookAddress)
                .Activate();

            _logger.WriteLine("[VisionVideo] SetMode hook installed successfully");
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[VisionVideo] Failed to install hook: {ex.Message}");
            _logger.WriteLine(ex.StackTrace ?? "");
        }
    }

    private int SetModeHookImpl(IntPtr thisPtr, IntPtr videoConfig)
    {
        var config = Mod.Configuration;

        // Read original values from VVideoConfig
        int* configAsInt = (int*)videoConfig;
        byte* configAsByte = (byte*)videoConfig;

        int originalWidth = configAsInt[2];   // +0x08
        int originalHeight = configAsInt[3];  // +0x0C
        int bpp = configAsInt[7];             // +0x1C
        bool fullscreen = configAsByte[0x48] != 0;

        _logger.WriteLine($"[VisionVideo] SetMode called:");
        _logger.WriteLine($"[VisionVideo]   Original: {originalWidth}x{originalHeight}, {bpp}bpp, fullscreen={fullscreen}");
        _logger.WriteLine($"[VisionVideo]   Config: OverrideResolution={config.OverrideResolution}, Width={config.ResolutionWidth}, Height={config.ResolutionHeight}");

        // Override resolution if configured
        int targetWidth = originalWidth;
        int targetHeight = originalHeight;

        if (config.OverrideResolution && config.ResolutionWidth > 0 && config.ResolutionHeight > 0)
        {
            targetWidth = config.ResolutionWidth;
            targetHeight = config.ResolutionHeight;
            
            _logger.WriteLine($"[VisionVideo]   Overriding to: {targetWidth}x{targetHeight}");

            // Write new resolution to VVideoConfig
            configAsInt[2] = targetWidth;   // +0x08 width
            configAsInt[3] = targetHeight;  // +0x0C height
        }

        // Always force windowed mode (D3D9Ex borderless window)
        if (fullscreen)
        {
            _logger.WriteLine($"[VisionVideo]   Forcing windowed mode for D3D9Ex");
            configAsByte[0x48] = 0;
        }

        // Call original function
        int result = _setModeHook!.OriginalFunction(thisPtr, videoConfig);
        _logger.WriteLine($"[VisionVideo]   Result: {result} ({(result != 0 ? "SUCCESS" : "FAILED")})");

        if (result != 0)
            _onResolutionApplied?.Invoke(targetWidth, targetHeight);

        return result;
    }

    private static (nuint BaseAddress, int Size)? GetModuleInfo(string moduleName)
    {
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return ((nuint)module.BaseAddress, module.ModuleMemorySize);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _setModeHook?.Disable();
    }
}
