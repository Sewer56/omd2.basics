using System.ComponentModel;
using Reloaded.Mod.Interfaces.Structs;
using System.ComponentModel.DataAnnotations;
using omd2.basics.Template.Configuration;

namespace omd2.basics.Configuration;

public class Config : Configurable<Config>
{
    /*
        Resolution and Graphics Settings for Orcs Must Die 2
    */

    // --- Resolution Override ---
    
    [Category("Resolution")]
    [DisplayName("Override Resolution")]
    [Description("Enable custom resolution override for the game renderer.")]
    [DefaultValue(true)]
    public bool OverrideResolution { get; set; } = true;

    [Category("Resolution")]
    [DisplayName("Width")]
    [Description("Render resolution width. Set to 0 to use desktop resolution.")]
    [DefaultValue(0)]
    public int ResolutionWidth { get; set; } = 0;

    [Category("Resolution")]
    [DisplayName("Height")]
    [Description("Render resolution height. Set to 0 to use desktop resolution.")]
    [DefaultValue(0)]
    public int ResolutionHeight { get; set; } = 0;

    [Category("Resolution")]
    [DisplayName("Window Dock Position")]
    [Description("Where to position the game window on screen when using a custom resolution.")]
    [DefaultValue(WindowDockPosition.Center)]
    public WindowDockPosition DockPosition { get; set; } = WindowDockPosition.Center;

    // --- Aspect Ratio Fix ---

    [Category("Aspect Ratio")]
    [DisplayName("Enable Aspect Ratio Fix")]
    [Description("Fix FOV for non-16:9 aspect ratios using Hor+ scaling. Wider screens will see more of the game world.")]
    [DefaultValue(true)]
    public bool EnableAspectRatioFix { get; set; } = true;

    [Category("Aspect Ratio")]
    [DisplayName("Additional FOV")]
    [Description("Additional FOV adjustment in degrees.\nGame default is 85, so a value of 5 gives 90 FOV.\nSprint in-game for change to take effect.")]
    [DefaultValue(0f)]
    [SliderControlParams(
        minimum: -20.0,
        maximum: 35.0,
        smallChange: 1,
        largeChange: 5,
        tickFrequency: 1,
        isSnapToTickEnabled: true,
        tickPlacement: SliderControlTickPlacement.Both,
        showTextField: true,
        isTextFieldEditable: true)]
    public float AdditionalFOV { get; set; } = 0f;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}

/// <summary>
/// Specifies where the game window should be positioned on screen.
/// </summary>
public enum WindowDockPosition
{
    [Display(Name = "Center")]
    Center,
    
    [Display(Name = "Top Left")]
    TopLeft,
    
    [Display(Name = "Top Center")]
    TopCenter,
    
    [Display(Name = "Top Right")]
    TopRight,
    
    [Display(Name = "Middle Left")]
    MiddleLeft,
    
    [Display(Name = "Middle Right")]
    MiddleRight,
    
    [Display(Name = "Bottom Left")]
    BottomLeft,
    
    [Display(Name = "Bottom Center")]
    BottomCenter,
    
    [Display(Name = "Bottom Right")]
    BottomRight
}
