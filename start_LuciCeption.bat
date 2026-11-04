@echo off
setlocal enabledelayedexpansion

REM --- Go to the folder of this BAT ---
cd /d "%~dp0"


echo Run exe
start "" .\Build\LuciCeption.exe
echo caffeine64
start "" caffeine64.exe

REM --- Prefer Python via launcher; fallback to direct python ---
where py >nul 2>&1 && (set "PYCMD=py -3") || (set "PYCMD=python")



echo Running python...
REM --- %PYCMD% .\Python\LuciController.py 1>run.out.log 2>run.err.log
[Your python exe location] .\Python\LuciController.py 


REM --- timeout /t 10 
