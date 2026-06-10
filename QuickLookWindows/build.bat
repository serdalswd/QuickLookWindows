@echo off
echo QuickLook for Windows - Build Script
echo =====================================

REM .NET 8 SDK kurulu olmasi gerekiyor
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo HATA: .NET SDK bulunamadi. https://dotnet.microsoft.com adresinden indirin.
    pause
    exit /b 1
)

echo Derleniyor...
dotnet publish QuickLookWindows.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o ..\dist

if %errorlevel% neq 0 (
    echo HATA: Derleme basarisiz oldu.
    pause
    exit /b 1
)

echo.
echo Basarili! EXE dosyasi: ..\dist\QuickLookWindows.exe
echo.
pause
