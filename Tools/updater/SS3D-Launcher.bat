@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SS3D-Launcher.ps1" %*
if errorlevel 1 pause
