# Steam Recording Utility

A portable Windows application for converting Steam recordings from 4:3 format with black bars to stretched 16:9 videos with per-video brightness, contrast, and saturation adjustments, preview functionality, and automatic YouTube upload.

> **Source Code:** The application source is in the [`SteamRecUtility/`](SteamRecUtility/) folder.

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
- **Encoder-Specific Quality Settings** - Configure in Settings dialog
  - **libx265 (CPU encoder)**:
    - CRF (0-51, default: 23) - Lower = better quality
    - Preset (ultrafast to veryslow, default: medium)
    - Tune options (film, animation, grain, etc.)
  - **hevc_nvenc (GPU encoder)** - Requires NVIDIA GPU:
    - CQ Level (0-51, default: 21)
    - Preset options (default, slow, medium, fast, hq, etc.)
    - Rate Control (constqp, vbr, cbr)
    - Spatial and Temporal Adaptive Quantization
  - Automatic fallback to CPU encoder if GPU unavailable
  - Single-pass encoding for optimal performance
- **Processing Options** - Toggle conversion features:
  - Enable/disable video conversion
  - Enable/disable resolution scaling
  - Enable/disable color adjustments
- **YouTube Upload** - Automatically upload converted videos to your YouTube channel
  - Customizable title and description templates with variables
  - Date removal option (removes yyyy-MM-dd patterns)
  - Custom text removal (comma-separated patterns)
  - Automatic whitespace trimming
  - Tag management
  - Privacy settings (private, unlisted, public)
  - Category selection
  - Made for Kids and Age Restriction options
  - OAuth 2.0 authentication
  - Uploaded videos organized in `output/uploaded/` folder
- **Settings Dialog** - Centralized configuration for:
  - Default video adjustments (brightness, contrast, saturation)
  - Output resolution presets
  - Encoder selection and quality parameters
  - File handling options
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
cd SteamRecUtility
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true
```

The executable will be in: `bin/Release/net8.0-windows/win-x64/publish/SteamRecUtility.exe`

#### Option 2: Framework-Dependent (Smaller file size)

Requires .NET 8.0 Runtime on the target machine:

```bash
cd SteamRecUtility
dotnet publish -c Release -r win-x64 --self-contained false
```

## Usage

1. **Launch the Application** - Double-click `SteamRecUtility.exe`

2. **Configure Folders**:
   - Click "..." button to select input folder (where your .mp4 files are)
   - Click "..." button to select output folder (where converted files will go)

3. **Configure Settings** (Optional):
   - Click "Settings" to open the settings dialog
   - Set default video adjustments (brightness, contrast, saturation)
   - Choose output resolution preset or custom size
   - Select encoder (libx265 CPU or hevc_nvenc GPU)
   - Configure encoder-specific quality parameters with helpful tooltips
   - Click OK to save settings

4. **Load Videos**:
   - Click "Load Videos" to scan the input folder
   - All videos will appear in the list on the left side
   - Click on any video to view its preview and adjustment controls

5. **Adjust Settings Per Video**:
   - Select a video from the list
   - View before/after preview (2 frames at 40% and 60% through the video)
   - Adjust brightness, contrast, and saturation using the sliders
   - Preview automatically refreshes 0.8 seconds after you stop adjusting
   - Click "Refresh Preview" to manually regenerate preview
   - Click "Apply to All" to copy current settings to all videos
   - Click "Reset" to restore default values

6. **YouTube Upload** (Optional):
   - Click "YouTube Settings" to open the YouTube configuration dialog
   - Configure title and description templates (use variables like {filename}, {date})
   - Enable date removal to strip yyyy-MM-dd patterns from titles
   - Add custom text patterns to remove (comma-separated)
   - Set tags, privacy, category, and other metadata
   - Click "Authenticate with YouTube" and follow the OAuth flow
   - See [YOUTUBE_SETUP.md](SteamRecUtility/YOUTUBE_SETUP.md) for detailed setup instructions

7. **Processing Options**:
   - Use checkboxes to enable/disable features:
     - Video conversion (if disabled, just copies files)
     - Resolution scaling
     - Color adjustments

8. **Start Conversion** - Click "Convert All" button

9. **Monitor Progress** - Watch the progress bar and log window

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
  "VideoEncoder": "libx265",
  "X265CRF": 23,
  "X265Preset": "medium",
  "X265Tune": "",
  "NvencCQ": 21,
  "NvencPreset": "hq",
  "NvencRateControl": "constqp",
  "NvencSpatialAQ": true,
  "NvencTemporalAQ": true,
  "MoveProcessedFiles": true,
  "EnableVideoConversion": true,
  "EnableScaling": true,
  "EnableColorAdjustments": true,
  "EnableYouTubeUpload": false,
  "YouTubeRemoveDateFromFilename": false,
  "YouTubeRemoveTextPatterns": ""
}
```

**Note:** Per-video settings (brightness, contrast, saturation, resolution) are not saved to the settings file. They are configured individually for each video during the session. The settings above represent defaults for new videos.

You can edit this file directly if needed.

## FFmpeg Installation

The application requires FFmpeg to be available. You have two options:

1. **Install FFmpeg system-wide** and add to PATH
2. **Place ffmpeg.exe** in the same folder as SteamRecUtility.exe

Download FFmpeg from: https://www.gyan.dev/ffmpeg/builds/

## Video Conversion Details

The application uses FFmpeg with optimized settings for quality and performance:

### Encoder Options

**libx265 (CPU Encoder)**:
- **Codec**: H.265/HEVC using x265 library
- **Quality Control**: CRF (Constant Rate Factor, 0-51, default: 23)
  - Lower values = better quality, larger files
  - Recommended range: 18-28
- **Preset**: Encoding speed/quality tradeoff (default: medium)
  - Slower presets = better compression at same quality
- **Tune**: Optional optimization for specific content types
- **Single-pass encoding** for optimal performance

**hevc_nvenc (GPU Encoder)** - Requires NVIDIA GPU:
- **Codec**: H.265/HEVC using NVIDIA hardware encoder
- **Quality Control**: CQ Level (0-51, default: 21)
- **Rate Control**: constqp (constant quality, recommended), vbr, or cbr
- **Adaptive Quantization**: Spatial and Temporal AQ for quality improvement
- **Automatic fallback** to CPU encoder if GPU unavailable
- **Single-pass encoding** for maximum speed

### Processing Pipeline

- **Pixel Format**: yuv420p for maximum compatibility
- **Scaling**: Lanczos algorithm for high-quality resampling (when enabled)
- **Aspect Ratio**: Stretches 4:3 to 16:9 using `setdar` (when enabled)
- **Color Adjustments**: Applied using `eq` filter with per-video settings (when enabled):
  - Brightness: -1.0 to 1.0 (default 0.0 = no change)
  - Contrast: 0.0 to 4.0 (default 1.0 = no change)
  - Saturation: 0.0 to 3.0 (default 1.2 = slightly enhanced)

**Quality Optimization**: All video processing (scaling, aspect ratio, color adjustments) is done in a single filter chain pass to avoid multiple resampling operations that degrade quality.

### Example FFmpeg Commands

**libx265 (CPU)**:
```bash
ffmpeg -y -i "input.mp4" -vf "scale=1920:1080:flags=lanczos,setdar=16/9,eq=brightness=0.00:contrast=1.00:saturation=1.20" -c:v libx265 -crf 23 -preset medium -pix_fmt yuv420p "output.mp4"
```

**hevc_nvenc (GPU)**:
```bash
ffmpeg -y -i "input.mp4" -vf "scale=1920:1080:flags=lanczos,setdar=16/9,eq=brightness=0.00:contrast=1.00:saturation=1.20" -c:v hevc_nvenc -preset hq -rc constqp -cq 21 -spatial-aq 1 -temporal-aq 1 -pix_fmt yuv420p "output.mp4"
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
