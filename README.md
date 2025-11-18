# Steam Recording Video Converter

A portable Windows application for converting Steam recordings from 4:3 format with black bars to stretched 16:9 videos with adjustable saturation and automatic YouTube upload.

> **Source Code:** The application source is in the [`VideoConverterApp/`](VideoConverterApp/) folder.

## Features

- **User-friendly GUI** - No need to edit scripts
- **Folder Selection** - Easy browse buttons for input/output folders
- **Configurable Output Resolution**:
  - 1920x1080 (Full HD)
  - 2560x1440 (2K)
  - 3840x2160 (4K)
  - Custom resolution
- **Adjustable Saturation** - Slider to control color saturation (0.0 to 3.0)
- **Quality Settings** - Configure CRF and bitrate
- **YouTube Upload** - Automatically upload converted videos to your YouTube channel
  - Customizable title and description templates
  - Tag management
  - Privacy settings (private, unlisted, public)
  - Category selection
  - OAuth 2.0 authentication
- **Progress Tracking** - Visual progress bar and detailed logs
- **Settings Persistence** - Settings saved to `settings.json` automatically
- **Portable** - Single executable, no installation required

## Requirements

- Windows 64-bit
- .NET 8.0 Runtime (or use self-contained build)
- FFmpeg (must be in PATH or same folder as executable)

## Building the Application

### Prerequisites

1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Build Commands

#### Option 1: Self-Contained Single File (Recommended for Portability)

This creates a single .exe file that includes all dependencies (no .NET runtime required on target machine):

```bash
cd VideoConverterApp
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true
```

The executable will be in: `bin/Release/net8.0-windows/win-x64/publish/VideoConverterApp.exe`

#### Option 2: Framework-Dependent (Smaller file size)

Requires .NET 8.0 Runtime on the target machine:

```bash
cd VideoConverterApp
dotnet publish -c Release -r win-x64 --self-contained false
```

## Usage

1. **Launch the Application** - Double-click `VideoConverterApp.exe`

2. **Configure Settings**:
   - Click "..." button to select input folder (where your .mp4 files are)
   - Click "..." button to select output folder (where converted files will go)
   - Choose output resolution from dropdown or enter custom values
   - Adjust saturation (1.0 = original, higher = more saturated)
   - Optional: Adjust CRF (18-23 recommended) and bitrate
   - Optional: Enable "Move original files to processed folder"

3. **YouTube Upload (Optional)**:
   - Check "Upload converted videos to YouTube"
   - Configure title template, description, tags, privacy, and category
   - Click "Authenticate with YouTube" and follow the OAuth flow
   - See [YOUTUBE_SETUP.md](VideoConverterApp/YOUTUBE_SETUP.md) for detailed setup instructions

4. **Start Conversion** - Click "Start Conversion" button

5. **Monitor Progress** - Watch the progress bar and log window

## Settings File

Settings are automatically saved to `settings.json` in the same folder as the executable.

**Note:** Input and output folders are blank by default. You must select them using the "..." buttons before converting videos.

Example `settings.json`:
```json
{
  "InputFolder": "",
  "OutputFolder": "",
  "OutputWidth": 1920,
  "OutputHeight": 1080,
  "Saturation": 1.2,
  "CRF": 18,
  "Bitrate": 20000,
  "MoveProcessedFiles": true
}
```

You can edit this file directly if needed.

## FFmpeg Installation

The application requires FFmpeg to be available. You have two options:

1. **Install FFmpeg system-wide** and add to PATH
2. **Place ffmpeg.exe** in the same folder as VideoConverterApp.exe

Download FFmpeg from: https://www.gyan.dev/ffmpeg/builds/

## Video Conversion Details

The application uses FFmpeg with the following settings:

- **Codec**: H.265 (libx265) for better compression
- **Aspect Ratio**: Stretches 4:3 to 16:9 using `setdar`
- **Saturation**: Applied using `eq` filter
- **Default CRF**: 18 (high quality)
- **Default Bitrate**: 20000 kbps

## Troubleshooting

### "FFmpeg not found" error
- Ensure ffmpeg.exe is in your PATH or in the same folder as the app

### Settings not saving
- Check that the application has write permissions in its directory
- The `settings.json` file should be created automatically

### Conversion fails
- Check that input files are valid MP4 files
- Ensure you have write permissions to the output folder
- Review the log window for specific error messages

## License

Free to use and modify.
