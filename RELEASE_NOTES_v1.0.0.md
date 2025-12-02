# Steam Recording Video Converter v1.0.0

First official release of the Steam Recording Video Converter!

## What's Included

### GUI Application (Steam Recording Utility)
- **Portable x64 Windows executable** (build required - see instructions below)
- User-friendly interface with folder selection
- Configurable output resolution (1080p, 2K, 4K, or custom)
- Adjustable saturation control (0.0 to 3.0)
- Quality settings (CRF and bitrate)
- YouTube upload integration with OAuth 2.0
- Customizable video metadata (titles, descriptions, tags)
- Real-time progress tracking with visual feedback
- Settings automatically saved to JSON file

## Requirements

- Windows 64-bit
- FFmpeg (in PATH or same folder as executable)
- .NET 8.0 Runtime (if using framework-dependent build)

## Building the Executable

To build the portable executable:

```bash
cd SteamRecUtility
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true
```

The executable will be in: `bin/Release/net8.0-windows/win-x64/publish/SteamRecUtility.exe`

## Features

- Converts 4:3 videos with black bars to stretched 16:9 format
- H.265 (libx265) encoding for efficient compression
- Adjustable color saturation
- Batch processing support
- Moves processed files to separate folder (optional)

## Quick Start

1. Download the release
2. Build the executable (see instructions below)
3. Ensure FFmpeg is installed
4. Launch the app and select your folders
5. Adjust settings and start converting!

For detailed instructions, see the [README](README.md).

## Changelog

### Added
- GUI application with folder selection and progress tracking
- Saturation adjustment capability
- YouTube upload integration with OAuth 2.0
- Customizable video metadata templates
- JSON settings persistence
- Multiple resolution presets (1080p, 2K, 4K, custom)
- Quality controls (CRF and bitrate)
- Optional "move to processed folder" feature
- Automatic file organization (uploaded videos moved to processed/uploaded)

### Changed
- Application now targets x64 architecture only
- Folder paths default to empty (user must select)
- Browse buttons changed to standard "..." style
