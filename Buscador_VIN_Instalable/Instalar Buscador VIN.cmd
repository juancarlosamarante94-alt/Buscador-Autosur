@echo off
set "SOURCE_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SOURCE_DIR%Instalar.ps1"
if errorlevel 1 pause
