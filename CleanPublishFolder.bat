@echo off
echo ================================================
echo  Cleaning Publish Folder for Deployment
echo ================================================
echo.

cd /d "%~dp0"

if not exist "Publish" (
    echo ERROR: Publish folder not found!
    echo Please run this script from the project root directory.
    pause
    exit /b 1
)

echo Cleaning up temporary files...
echo.

:: Remove debug logs
if exist "Publish\debug_logs" (
    echo [1/3] Removing debug_logs folder...
    rmdir /s /q "Publish\debug_logs" 2>nul
    if exist "Publish\debug_logs" (
        echo WARNING: Could not delete debug_logs folder
    ) else (
        echo       ✓ Removed debug_logs/
    )
) else (
    echo [1/3] debug_logs/ not found (already clean)
)

:: Remove last request file
if exist "Publish\last_request.json" (
    echo [2/3] Removing last_request.json...
    del /q "Publish\last_request.json" 2>nul
    if exist "Publish\last_request.json" (
        echo WARNING: Could not delete last_request.json
    ) else (
        echo       ✓ Removed last_request.json
    )
) else (
    echo [2/3] last_request.json not found (already clean)
)

:: Optional: Remove PDB files (debug symbols)
echo [3/3] Checking for .pdb files...
if exist "Publish\*.pdb" (
    echo       Found .pdb files
    echo       Do you want to remove them? (reduces size, but harder to debug)
    choice /c YN /n /m "       Remove .pdb files? (Y/N): "
    if errorlevel 2 (
        echo       ✓ Kept .pdb files
    ) else (
        del /q "Publish\*.pdb" 2>nul
        echo       ✓ Removed .pdb files
    )
) else (
    echo       No .pdb files found
)

echo.
echo ================================================
echo  Cleanup Complete!
echo ================================================
echo.
echo The Publish folder is now ready for deployment.
echo You can:
echo   1. ZIP the Publish folder and share it
echo   2. Copy the Publish folder to a network drive
echo   3. Copy the Publish folder to target computers
echo.
echo Files in Publish folder:
dir /b "Publish\WasfatyInvoiceProcessor.exe" 2>nul
if errorlevel 1 (
    echo ERROR: WasfatyInvoiceProcessor.exe not found!
    echo Please rebuild the application first.
) else (
    echo ✓ WasfatyInvoiceProcessor.exe
)

dir /b "Publish\appsettings.json" 2>nul
if errorlevel 1 (
    echo WARNING: appsettings.json not found!
) else (
    echo ✓ appsettings.json
)

echo.
pause

