using System.Diagnostics;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Mod.Interfaces;

namespace omd2.basics.Graphics;

/// <summary>
/// Hooks the game's configGetInt function to intercept ForceFPS reads.
/// 
/// configGetInt (sub_405700) is a __fastcall function:
///   - ECX: config handle index
///   - EDX: pointer to output value (int*)
///   - Returns: AL = 1 if successful, 0 if not
/// 
/// When the ForceFPS config is queried, we intercept and return our own value.
/// Setting FPS to 0 effectively disables the frame limiter (returning 0/failure skips it).
/// </summary>
public unsafe class FpsLimitController : IDisposable
{
    // Signature for configGetInt function (sub_405700)
    private const string ConfigGetIntSignature = 
        "A1 ?? ?? ?? ?? ?? ?? ?? 8B 80 ?? ?? ?? ?? 83 F8 ?? 74 ?? ?? ?? ?? A1 ?? ?? ?? ?? F6 44 C8 ?? ?? ?? ?? ?? 75 ?? 85 C0 75 ?? 32 C0 C3 8B 48 ?? 83 E9 ?? 74 ?? 83 E9 ?? 74 ?? 83 E9 ?? 75 ?? 8B 40 ?? 85 C0 75 ?? B8 ?? ?? ?? ?? ?? ?? ?? ?? ?? B0";

    // Expected address for configGetInt (no ASLR)
    private const nuint ExpectedConfigGetIntAddress = 0x405700;

    // Address of ForceFPS handle storage (set after config registration)
    private const nuint ConfigForceFpsHandleAddress = 0x8cf260;

    private readonly ILogger _logger;
    private readonly IReloadedHooks _hooks;

    private IHook<ConfigGetIntDelegate>? _configGetIntHook;
    private int _forceFpsHandle = -1;
    private bool _isHookActive;

    /// <summary>
    /// Delegate for configGetInt function (sub_405700)
    /// char __fastcall configGetInt(int configHandle, int* outValue)
    /// </summary>
    [Function(CallingConventions.Fastcall)]
    private delegate int ConfigGetIntDelegate(int configHandle, int* outValue);

    public FpsLimitController(
        IReloadedHooks hooks,
        IStartupScanner startupScanner,
        ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;

        startupScanner.AddMainModuleScan(ConfigGetIntSignature, OnConfigGetIntFound);
    }

    private void OnConfigGetIntFound(PatternScanResult result)
    {
        if (!result.Found)
        {
            _logger.WriteLine("[FpsLimit] configGetInt pattern not found");
            return;
        }

        var baseAddress = (nuint)Process.GetCurrentProcess().MainModule!.BaseAddress;
        var foundAddress = baseAddress + (nuint)result.Offset;

        if (foundAddress != ExpectedConfigGetIntAddress)
        {
            _logger.WriteLine($"[FpsLimit] configGetInt at unexpected address 0x{foundAddress:X} (expected 0x{ExpectedConfigGetIntAddress:X})");
            return;
        }

        _configGetIntHook = _hooks
            .CreateHook<ConfigGetIntDelegate>(ConfigGetIntHookImpl, (long)foundAddress)
            .Activate();

        _isHookActive = true;
        _logger.WriteLine($"[FpsLimit] Hook installed at 0x{foundAddress:X}");
    }

    /// <summary>
    /// Called when configuration is updated. Changes take effect immediately
    /// since the game calls configGetInt every frame.
    /// </summary>
    public void OnConfigUpdated()
    {
        if (!_isHookActive)
            return;

        var config = Mod.Configuration;
        if (!config.OverrideFpsLimit)
        {
            _logger.WriteLine("[FpsLimit] Override disabled");
            return;
        }

        _logger.WriteLine(config.FpsLimit <= 0
            ? "[FpsLimit] Uncapped"
            : $"[FpsLimit] Limit: {config.FpsLimit}");
    }

    private int ConfigGetIntHookImpl(int configHandle, int* outValue)
    {
        // Lazily read the ForceFPS handle (set at runtime during config init)
        if (_forceFpsHandle == -1)
            _forceFpsHandle = *(int*)ConfigForceFpsHandleAddress;

        // Only intercept ForceFPS queries
        if (configHandle != _forceFpsHandle || _forceFpsHandle == -1)
            return _configGetIntHook!.OriginalFunction(configHandle, outValue);

        var config = Mod.Configuration;
        if (!config.OverrideFpsLimit)
            return _configGetIntHook!.OriginalFunction(configHandle, outValue);

        // FPS <= 0 means disable frame limiting (return failure)
        if (config.FpsLimit <= 0)
            return 0;

        *outValue = config.FpsLimit;
        return 1;
    }

    public void Dispose()
    {
        _configGetIntHook?.Disable();
    }
}
