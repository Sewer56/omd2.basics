# Orcs Must Die 2 Basics

A [Reloaded-II](https://reloaded-project.github.io/Reloaded-II/) mod for Orcs Must Die 2.

![Preview](https://github.com/user-attachments/assets/1216afc5-daf2-4bf5-95ef-4c4905b4d859)

*Preview at 64:9 (7680x1080)*

## Features

- **Resolution Override** - Force any resolution (defaults to desktop resolution)
- **Window Dock Position** - Position window at screen edges or center (top-left, bottom-center, etc.)
- **D3D9Ex Upgrade** - Upgrades the game from D3D9 to D3D9Ex
- **Aspect Ratio Fix** - Hor+ FOV scaling for ultrawide (21:9, 32:9, etc.)
- **Additional FOV Slider** - Fine-tune field of view
- **VSync Override** - Force VSync on/off (default: ON)
- **FPS Limit Override** - Set a custom FPS limit (default: 0 = no limit, use VSync)

## Non-Features

- **Window Resizing** - The game window cannot be resized after launch
- **HUD Repositioning** - HUD elements are not moved

## Known Issues

- **In-game resolution menu is broken** - Changing resolution from the game's video settings menu does not work correctly. Use the mod's config instead.

## Disclaimers

- **Borderless Windowed Only** - This mod forces borderless windowed mode because:
  - It's the default/intended D3D9Ex mode (on Windows 10+ it has the same optimizations as exclusive fullscreen)
  - The game has performance problems in exclusive fullscreen for some reason on my end

## Installation

1. Install [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II/releases/latest/download/Setup.exe)
2. Add Orcs Must Die 2 to Reloaded-II
   - Plus icon, bottom left.
   - Browse to `C:\Program Files (x86)\Steam\steamapps\common\Orcs Must Die 2\build\game\OrcsMustDie2.exe`
3. Download from releases
   - 1 click, paste into browser URL bar `r2:https://github.com/Sewer56/omd2.basics/releases/download/1.1.0/omd2.basics1.1.0.7z`
4. Enable for Orcs Must Die 2
   - Tick the box beside the game so it becomes a plus.
5. Configure
   - Click the 'Configure Mod' button after highlighting the mod row.
   - Adjust settings as desired.
6. Launch the game from Reloaded-II

## Note

This is not purroduction quality software, nya~! I made this within 4-ish hours so I can play co-op with someone very pawsitively precious to me, whose monitor has the bottom half broken.

The game normally restricts you to resolutions in its preset list, so without a non-standard aspect ratio and resolution, part of the game would render on the broken half of the screen, making it unpurrlayable.

## License

GPLv3
