# Orcs Must Die 2 Basics

A [Reloaded-II](https://reloaded-project.github.io/Reloaded-II/) mod for Orcs Must Die 2 that provides resolution override and aspect ratio fixes.

## Features

- **Resolution Override** - Run at any resolution (defaults to your desktop resolution)
- **D3D9Ex Upgrade** - Converts the game from D3D9 to D3D9Ex for better performance and compatibility
- **Aspect Ratio Fix** - Hor+ FOV scaling for ultrawide monitors (21:9, 32:9, etc.)
- **Additional FOV Slider** - Fine-tune your field of view

## Installation

1. Install [Reloaded-II](https://reloaded-project.github.io/Reloaded-II/)
2. Download this mod from releases
3. Import into Reloaded-II
4. Enable for Orcs Must Die 2

## Configuration

All settings are configurable through the Reloaded-II mod configuration interface:

- **Override Resolution** - Enable/disable custom resolution
- **Width/Height** - Set custom resolution (0 = use desktop resolution)
- **Enable Aspect Ratio Fix** - Fix FOV for non-16:9 displays
- **Additional FOV** - Extra FOV adjustment (-5 to +5 degrees)

## Building

Requires .NET 9.0 SDK.

```powershell
dotnet build src/omd2.basics.csproj
```

For publishing:

```powershell
./Publish.ps1
```

## Dependencies

- [Reloaded.SharedLib.Hooks](https://github.com/Reloaded-Project/Reloaded.SharedLib.Hooks)
- [Reloaded.Memory.SigScan](https://github.com/Reloaded-Project/Reloaded.Memory.SigScan)

## License

See repository for license information.
