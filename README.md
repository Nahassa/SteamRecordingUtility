# Steam Recording Video Converter

A portable Windows application for converting Steam recordings from 4:3 format with black bars to stretched 16:9 videos with per-video brightness, contrast, and saturation adjustments, preview functionality, and automatic YouTube upload.

> **Source Code:** The application source is in the [`VideoConverterApp/`](VideoConverterApp/) folder.

## Features

- **User-friendly GUI** - No need to edit scripts
- **Folder Selection** - Easy browse buttons for input/output folders
- **Per-Video Preview & Adjustments**:
  - Load all videos before conversion
  - Before/after preview with 2 frames per video (40% and 60% through)
  - Individual adjustment controls for each video
  - Brightness adjustment (-1.0 to 1.0, default 0.0)
  - Contrast adjustment (0.0 to 4.0, default 1.0)
  - Saturation adjustment (0.0 to 3.0, default 1.2)
  - "Apply to All" button to copy settings across videos
  - Real-time filtered preview regeneration
- **Configurable Output Resolution**:
  - 1920x1080 (Full HD)
  - 2560x1440 (2K)
  - 3840x2160 (4K)
  - Custom resolution
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

2. **Configure Folders**:
   - Click "..." button to select input folder (where your .mp4 files are)
   - Click "..." button to select output folder (where converted files will go)

3. **Load Videos**:
   - Click "Load Videos" to scan the input folder
   - All videos will appear in the list on the left side
   - Click on any video to view its preview and adjustment controls

4. **Adjust Settings Per Video**:
   - Select a video from the list
   - View before/after preview (2 frames at 40% and 60% through the video)
   - Adjust brightness, contrast, and saturation using the sliders
   - Click "Refresh Preview" to see changes in the after preview
   - Click "Apply to All" to copy current settings to all videos
   - Click "Reset" to restore default values
   - Choose output resolution from dropdown or enter custom values
   - Optional: Adjust CRF (18-23 recommended) and bitrate

5. **YouTube Upload (Optional)**:
   - Check "Upload converted videos to YouTube"
   - Configure title template, description, tags, privacy, and category
   - Click "Authenticate with YouTube" and follow the OAuth flow
   - See [YOUTUBE_SETUP.md](VideoConverterApp/YOUTUBE_SETUP.md) for detailed setup instructions

6. **Start Conversion** - Click "Convert All" button

7. **Monitor Progress** - Watch the progress bar and log window

> **Note:** Screenshots will be added in a future update once the application is built and tested.

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
  "Brightness": 0.0,
  "Contrast": 1.0,
  "Saturation": 1.2,
  "CRF": 18,
  "Bitrate": 20000,
  "MoveProcessedFiles": true
}
```

**Note:** Per-video settings (brightness, contrast, saturation) are not saved to the settings file. They are configured individually for each video during the session.

You can edit this file directly if needed.

## FFmpeg Installation

The application requires FFmpeg to be available. You have two options:

1. **Install FFmpeg system-wide** and add to PATH
2. **Place ffmpeg.exe** in the same folder as VideoConverterApp.exe

Download FFmpeg from: https://www.gyan.dev/ffmpeg/builds/

## Video Conversion Details

The application uses FFmpeg with the following settings:

- **Codec**: H.265 (libx265) for better compression
- **Pixel Format**: yuv420p for maximum compatibility
- **Aspect Ratio**: Stretches 4:3 to 16:9 using `setdar`
- **Color Adjustments**: Applied using `eq` filter with per-video settings:
  - Brightness: -1.0 to 1.0 (default 0.0 = no change)
  - Contrast: 0.0 to 4.0 (default 1.0 = no change)
  - Saturation: 0.0 to 3.0 (default 1.2 = slightly enhanced)
- **Default CRF**: 18 (high quality)
- **Default Bitrate**: 20000 kbps

Example FFmpeg command:
```bash
ffmpeg -i "input.mp4" -vf "setdar=16/9,eq=brightness=0.00:contrast=1.00:saturation=1.20" -c:v libx265 -pix_fmt yuv420p -crf 18 -b:v 20000k -s 1920x1080 "output.mp4"
```

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
