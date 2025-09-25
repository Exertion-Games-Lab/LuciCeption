@echo off
cd /d "%~dp0"

start "" python .\Python\LuciController.py

start "" .\Build\LuciCeption.exe

pause
