# Scrappy Text Editor Icons

## Required Icons

This directory should contain the puppy-themed application icons in multiple formats for cross-platform support.

### Source Icon (T046)
- **File**: `scrappy-icon.svg` (not yet created)
- **Design**: Stylized puppy character with pen/notepad
- **Requirements**:
  - Must be recognizable at 16x16 (taskbar/tab icon size)
  - Puppy should be stylized, not photorealistic
  - Should convey "friendly" and "text editing" concepts

### Generated Icons (T047-T048)

Once `scrappy-icon.svg` is created, generate multi-resolution icons using:

```bash
# Install electron-icon-maker
npm install -g electron-icon-maker

# Generate all formats
electron-icon-maker --input=scrappy-icon.svg --output=./icons
```

This will generate:
- **Windows**: `icon.ico` (16x16, 32x32, 48x48, 256x256)
- **macOS**: `icon.icns` (16x16 to 512x512 @1x and @2x)
- **Linux**: Multiple PNG sizes

### electron.manifest.json Configuration (T049)

After icons are generated, update `src/TextEdit.App/electron.manifest.json`:

```json
{
  "build": {
    "win": {
      "icon": "wwwroot/icons/icon.ico"
    },
    "mac": {
      "icon": "wwwroot/icons/icon.icns"
    },
    "linux": {
      "icon": "wwwroot/icons/icon.png"
    }
  }
}
```

## Current Status

- ⏳ T046: Pending - Icon design/commission needed
- ⏳ T047: Blocked by T046 - Icon generation
- ⏳ T048: Blocked by T047 - Icon placement
- ⏳ T049: Blocked by T048 - Manifest configuration

## Temporary Workaround

The application will use Electron.NET's default icon until the puppy icon is ready.
