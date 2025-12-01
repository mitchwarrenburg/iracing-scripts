# Icon Placeholder

The application icon file `icon.ico` should be placed in this directory.

## Creating an Icon

You can create an icon using:
- **Online tools**: favicon.io, iconarchive.com
- **Design software**: Adobe Illustrator, Figma
- **Icon converters**: PNG to ICO converters

## Requirements

- Format: `.ico`
- Recommended sizes: 16x16, 32x32, 48x48, 256x256
- Filename: `icon.ico`

## Temporary Solution

Until you add a custom icon, Windows will use the default .NET application icon.

## Quick Icon Creation

```powershell
# Using ImageMagick (if installed)
magick convert -background transparent -fill "#00ff00" -font Arial -pointsize 72 label:"iR" -resize 256x256 icon.png
magick convert icon.png -define icon:auto-resize=256,128,96,64,48,32,16 icon.ico
```

Or use this PowerShell script to download a free racing icon:

```powershell
# Download a temporary racing-themed icon
Invoke-WebRequest -Uri "https://icons8.com/icon/download/racing/windows" -OutFile "icon.ico"
```

## Recommended Icon Design

- Racing-themed (checkered flag, speedometer, etc.)
- Simple and recognizable at small sizes
- Works well on light and dark backgrounds
- Brand colors: Green (#00ff00) and dark gray (#1a1a1a)

