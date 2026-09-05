@echo off
del /Q "%USERPROFILE%\Desktop\Buscador VIN Autosur.lnk" 2>nul
taskkill /IM "Buscador Autosur.exe" /F >nul 2>nul
rmdir /S /Q "%LOCALAPPDATA%\Buscador VIN Autosur" 2>nul
echo Buscador VIN desinstalado.
pause
