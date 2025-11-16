@echo off
echo Building Video Converter App (x86, Self-Contained)...
echo.

dotnet publish -c Release -r win-x86 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Build successful!
    echo ========================================
    echo.
    echo Executable location:
    echo bin\Release\net6.0-windows\win-x86\publish\VideoConverterApp.exe
    echo.
) else (
    echo.
    echo Build failed!
    echo.
)

pause
