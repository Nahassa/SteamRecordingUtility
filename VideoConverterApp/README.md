# VideoConverterApp

This folder contains the source code for the Steam Recording Utility application.

For full documentation, build instructions, and usage guide, see the [main README](../README.md).

## Quick Links

- **Main Documentation:** [../README.md](../README.md)
- **YouTube Setup Guide:** [YOUTUBE_SETUP.md](YOUTUBE_SETUP.md)
- **Build Instructions:** See main README

## Building

```bash
cd VideoConverterApp
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true
```

Executable will be at: `bin/Release/net8.0-windows/win-x64/publish/VideoConverterApp.exe`
