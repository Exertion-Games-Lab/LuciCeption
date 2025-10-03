@echo off
setlocal enabledelayedexpansion

REM --- Go to the folder of this BAT ---
cd /d "%~dp0"


REM --- Prefer Python via launcher; fallback to direct python ---
where py >nul 2>&1 && (set "PYCMD=py -3") || (set "PYCMD=python")



echo Running python...
REM --- %PYCMD% .\Python\LuciController.py 1>run.out.log 2>run.err.log
C:/Users/pwan0076/AppData/Local/Microsoft/WindowsApps/python3.11.exe .\LR_signal_practice.py



pause