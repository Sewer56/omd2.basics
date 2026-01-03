using System.Diagnostics;
using omd2.basics.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Mod.Interfaces;

namespace omd2.basics.Graphics;

/// <summary>
/// Fixes aspect ratio for non-16:9 resolutions.
/// 
/// The game uses "Vert-" scaling: on wider screens, vertical FOV shrinks,
/// causing players to see less on top/bottom compared to 16:9.
/// 
/// This patch hooks the SetFOV function and applies a trigonometric correction
/// so that wider screens see more horizontally while preserving the 16:9 vertical view.
/// </summary>
public unsafe class AspectRatioController : IDisposable
{
    private const float DefaultAspectRatio = 16f / 9f; // 1.777777791
    private const string TargetModuleName = "Vision90.dll";

    // Signature to find VisRenderContext_cl::SetFOV(float fovDegrees, float unknown)
    // CC = INT3 padding before function start
    // The first argument is the FOV in degrees (e.g., 85.0)
    private const string FovSignature = "CC F3 0F 10 44 ?? ?? ?? ?? ?? ?? ?? ?? F3 0F 11 86 ?? ?? ?? ?? F3 0F 10 ?? ?? ?? F3 0F 11 86";

    private readonly ILogger _logger;
    private readonly IReloadedHooks _hooks;
    private readonly IScannerFactory _scannerFactory;

    private float _fovMultiplier = 1.0f;
    private IHook<SetFovDelegate>? _setFovHook;
    private nuint _hookAddress;
    
    private int _currentWidth;
    private int _currentHeight;
    private bool _initialized;

    public AspectRatioController(
        IReloadedHooks hooks, 
        IScannerFactory scannerFactory,
        ILogger logger)
    {
        _hooks = hooks;
        _scannerFactory = scannerFactory;
        _logger = logger;

        // Vision90.dll should already be loaded
        var moduleInfo = GetModuleInfo(TargetModuleName);
        if (moduleInfo.HasValue)
        {
            _logger.WriteLine($"[AspectRatio] {TargetModuleName} found at 0x{moduleInfo.Value.BaseAddress:X}");
            ScanAndInstallHook(moduleInfo.Value.BaseAddress, moduleInfo.Value.Size);
        }
        else
        {
            _logger.WriteLine($"[AspectRatio] ERROR: {TargetModuleName} not loaded!");
        }
    }

    private void ScanAndInstallHook(nuint baseAddress, int size)
    {
        try
        {
            _logger.WriteLine($"[AspectRatio] Scanning {TargetModuleName} (size: {size})...");
            
            using var scanner = _scannerFactory.CreateScanner((byte*)baseAddress, size);
            var result = scanner.FindPattern(FovSignature);

            if (!result.Found)
            {
                _logger.WriteLine("[AspectRatio] FOV pattern not found - aspect ratio fix disabled");
                _logger.WriteLine($"[AspectRatio] Pattern: {FovSignature}");
                return;
            }

            // Skip the CC byte to get to function start
            _hookAddress = baseAddress + (nuint)result.Offset + 1;
            
            _logger.WriteLine($"[AspectRatio] SetFOV found at offset 0x{result.Offset:X}");
            _logger.WriteLine($"[AspectRatio] Hooking at function entry 0x{_hookAddress:X}");
            InstallFovHook();
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[AspectRatio] Failed to scan: {ex.Message}");
        }
    }

    private void InstallFovHook()
    {
        if (!Mod.Configuration.EnableAspectRatioFix)
        {
            _logger.WriteLine("[AspectRatio] Aspect ratio fix disabled in config");
            return;
        }

        try
        {
            // Create function hook for SetFOV
            _setFovHook = _hooks.CreateHook<SetFovDelegate>(SetFovHookImpl, (long)_hookAddress).Activate();
            _initialized = true;
            
            _logger.WriteLine("[AspectRatio] FOV hook installed successfully");
            
            if (_currentWidth > 0 && _currentHeight > 0)
            {
                UpdateFovMultiplier();
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[AspectRatio] Failed to install FOV hook: {ex.Message}");
        }
    }
    
    private void SetFovHookImpl(IntPtr thisPtr, float fovDegrees, float fov2)
    {
        // ===========================================
        // FOV CORRECTION FOR VERT- SCALING
        // ===========================================
        //
        // Problem: The game uses "Vert-" (vertical minus) scaling.
        // On wider screens, the vertical FOV shrinks to fit the wider aspect ratio,
        // causing you to see LESS on top/bottom compared to 16:9.
        //
        // Solution: Scale the FOV so that wider screens see MORE horizontally
        // while keeping the same vertical view as 16:9.
        //
        // Why can't we just multiply the FOV angle directly?
        // ---------------------------------------------------
        // FOV describes viewing angles, but the relationship between
        // screen width and viewing angle is non-linear (trigonometric).
        //
        // The screen half-width is proportional to: tan(FOV / 2)
        //
        // So to scale the FOV for a new aspect ratio:
        //   1. Convert FOV to "screen space" using tan(FOV/2)
        //   2. Scale that by the aspect ratio change
        //   3. Convert back to an angle using atan
        //
        // Formula:
        //   newFOV = 2 * atan(tan(oldFOV / 2) * aspectMultiplier)
        //
        // where aspectMultiplier = currentAspect / defaultAspect
        //
        // Example (32:9 ultrawide, 3.556 aspect ratio):
        //   - aspectMultiplier = 3.556 / 1.778 = 2.0
        //   - Original FOV: 85°
        //   - tan(85° / 2) = tan(42.5°) = 0.9163
        //   - Scaled: 0.9163 * 2.0 = 1.8326  
        //   - New FOV: 2 * atan(1.8326) = 2 * 61.38° = 122.76°
        //
        // ===========================================

        // Apply additional user-configurable FOV adjustment to the base FOV
        float adjustedFovDegrees = fovDegrees + Mod.Configuration.AdditionalFOV;
        
        double fovRad = adjustedFovDegrees * Math.PI / 180.0; // Convert degrees to radians
        double halfFovTan = Math.Tan(fovRad / 2.0);           // Get tan(fov/2) - proportional to screen half-width
        double scaledHalfFovTan = halfFovTan * _fovMultiplier; // Scale for new aspect ratio
        double newFovRad = 2.0 * Math.Atan(scaledHalfFovTan); // Convert back to angle
        float newFovDegrees = (float)(newFovRad * 180.0 / Math.PI); // Convert radians to degrees

        _setFovHook!.OriginalFunction(thisPtr, newFovDegrees, fov2);
    }

    public void OnResolutionChanged(int width, int height)
    {
        _currentWidth = width;
        _currentHeight = height;
        
        if (_initialized)
            UpdateFovMultiplier();
    }

    public void OnConfigUpdated()
    {
        if (!_initialized) return;
        UpdateFovMultiplier();
    }

    private void UpdateFovMultiplier()
    {
        if (_currentWidth <= 0 || _currentHeight <= 0) return;
        float currentAspect = (float)_currentWidth / _currentHeight;
        
        // The multiplier represents how much wider the screen is compared to 16:9.
        // For 32:9 (3.556): multiplier = 3.556 / 1.778 = 2.0 (twice as wide)
        // For 21:9 (2.333): multiplier = 2.333 / 1.778 = 1.31
        // For 16:9 (1.778): multiplier = 1.0 (no change needed)
        _fovMultiplier = currentAspect / DefaultAspectRatio;
        _logger.WriteLine($"[AspectRatio] Resolution: {_currentWidth}x{_currentHeight}, Aspect: {currentAspect:F4}, Multiplier: {_fovMultiplier:F4}");
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

    public void Dispose() => _setFovHook?.Disable();

    // Delegate for SetFOV function: void __thiscall SetFOV(float fov1, float fov2)
    // In Reloaded, thiscall is represented as fastcall with 'this' in ecx (edx unused)
    [Function(CallingConventions.MicrosoftThiscall)]
    private delegate void SetFovDelegate(IntPtr thisPtr, float fov1, float fov2);
}
