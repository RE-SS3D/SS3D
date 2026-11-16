@echo off
rem Dev shortcut: joins 127.0.0.1:1151 as Frenchman, skipping the main menu. Not shipped in release zips.
cd /d "%~dp0Game"
start "" SS3D.exe -ip=127.0.0.1 -port=1151 -ckey=Frenchman -skipintro
