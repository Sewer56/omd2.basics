using omd2.basics.Configuration;
using omd2.basics.DirectX;
using omd2.basics.Graphics;
using omd2.basics.Template;
using omd2.basics.Window;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Mod.Interfaces;
#if DEBUG
using System.Diagnostics;
#endif

namespace omd2.basics;

/// <summary>
/// Orcs Must Die 2 mod - Resolution override, D3D9Ex upgrade, and aspect ratio fix.
/// </summary>
public class Mod : ModBase
{
    private readonly IModLoader _modLoader;
    private readonly IReloadedHooks? _hooks;
    private readonly ILogger _logger;
    private readonly IMod _owner;
    private readonly IModConfig _modConfig;

    /// <summary>
    /// Current mod configuration. Updated when user changes settings.
    /// </summary>
    public static Config Configuration { get; private set; } = null!;

    // Controllers
    private VisionVideoController? _visionVideoController;
    private D3D9Controller? _d3d9Controller;
    private AspectRatioController? _aspectRatioController;
    private WindowController? _windowController;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        Configuration = context.Configuration;
        _modConfig = context.ModConfig;

#if DEBUG
        Debugger.Launch();
#endif

        _logger.WriteLine($"[{_modConfig.ModId}] Initializing...");

        InitializeControllers();
    }

    private void InitializeControllers()
    {
        if (_hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Reloaded.Hooks not available!");
            return;
        }

        try
        {
            // Get IScannerFactory for creating scanners
            var scannerFactoryController = _modLoader.GetController<IScannerFactory>();
            if (scannerFactoryController == null || !scannerFactoryController.TryGetTarget(out var scannerFactory))
            {
                _logger.WriteLine($"[{_modConfig.ModId}] ERROR: IScannerFactory not available! Make sure Reloaded.Memory.SigScan.ReloadedII is installed.");
                return;
            }

            // Create window controller
            _windowController = new WindowController(_logger);

            // Create Vision video controller (hooks SetMode to override resolution at engine init)
            _visionVideoController = new VisionVideoController(_hooks, scannerFactory, _logger, OnResolutionChanged);

            // Create aspect ratio controller (hooks SetFOV for aspect ratio fix)
            _aspectRatioController = new AspectRatioController(_hooks, scannerFactory, _logger);

            // Create D3D9 controller with resolution change callback
            _d3d9Controller = new D3D9Controller(_hooks, _logger, OnResolutionChanged);

            _logger.WriteLine($"[{_modConfig.ModId}] All controllers initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Failed to initialize controllers: {ex.Message}");
            _logger.WriteLine(ex.StackTrace ?? "");
        }
    }

    /// <summary>
    /// Called when the D3D9 resolution changes (on device creation or reset).
    /// </summary>
    private void OnResolutionChanged(int width, int height)
    {
        _logger.WriteLine($"[{_modConfig.ModId}] Resolution changed to {width}x{height}");

        // Update aspect ratio controller
        _aspectRatioController?.OnResolutionChanged(width, height);

        // Resize window to match
        _windowController?.ResizeGameWindow(width, height);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        Configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying changes");

        // Update aspect ratio settings (FOV slider)
        _aspectRatioController?.OnConfigUpdated();

        // Note: Resolution changes require device reset, which the game handles
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
