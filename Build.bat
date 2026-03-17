@echo off
echo ================================================
echo Wasfaty Invoice Processor - Build Script
echo ================================================
echo.

echo [1/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b %errorlevel%
)
echo.

echo [2/4] Building Release configuration...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b %errorlevel%
)
echo.

echo [3/4] Publishing application...
dotnet publish WasfatyInvoiceProcessor\WasfatyInvoiceProcessor.csproj --configuration Release --output Publish --no-build
if %errorlevel% neq 0 (
    echo ERROR: Publish failed
    pause
    exit /b %errorlevel%
)
echo.

echo [4/4] Build completed successfully!
echo.
echo ================================================
echo The application is ready to run!
echo ================================================
echo.
echo Output location: %cd%\Publish
echo Executable: WasfatyInvoiceProcessor.exe
echo.
echo To run the application:
echo   cd Publish
echo   WasfatyInvoiceProcessor.exe
echo.
echo ================================================

pause
