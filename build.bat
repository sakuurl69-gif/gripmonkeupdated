@echo off
setlocal enabledelayedexpansion
title ExtraGrip builder
cd /d "%~dp0"

echo ==== ExtraGrip build script ====

REM ---- 1. Find Gorilla Tag ----
set "GT="
for %%P in (
 "C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag"
 "C:\Program Files\Steam\steamapps\common\Gorilla Tag"
 "D:\SteamLibrary\steamapps\common\Gorilla Tag"
 "E:\SteamLibrary\steamapps\common\Gorilla Tag"
 "D:\Steam\steamapps\common\Gorilla Tag"
 "C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag"
) do if exist "%%~P\Gorilla Tag.exe" if not defined GT set "GT=%%~P"

if not defined GT (
  echo Could not find Gorilla Tag automatically.
  set /p GT=Paste your Gorilla Tag folder path ^(the one with Gorilla Tag.exe^): 
  set "GT=!GT:"=!"
)
if not exist "!GT!\Gorilla Tag.exe" (
  echo ERROR: Gorilla Tag.exe not found in "!GT!"
  pause & exit /b 1
)
echo Game: !GT!

set "MANAGED=!GT!\Gorilla Tag_Data\Managed"
set "BEPCORE=!GT!\BepInEx\core"
if not exist "!BEPCORE!\BepInEx.dll" (
  echo ERROR: BepInEx is not installed. Install BepInEx 5 ^(x64^) into the game folder and run the game once, then re-run this.
  pause & exit /b 1
)

REM ---- 2. Find the C# compiler that ships with Windows (no downloads) ----
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "!CSC!" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "!CSC!" (
  echo ERROR: Built-in csc.exe not found.
  pause & exit /b 1
)

REM ---- 3. Compile ----
set "REFS=/r:"!BEPCORE!\BepInEx.dll" /r:"!MANAGED!\UnityEngine.dll" /r:"!MANAGED!\UnityEngine.CoreModule.dll" /r:"!MANAGED!\UnityEngine.XRModule.dll" /r:"!MANAGED!\UnityEngine.PhysicsModule.dll""
if exist "!MANAGED!\netstandard.dll" set "REFS=!REFS! /r:"!MANAGED!\netstandard.dll""
if exist "!MANAGED!\mscorlib.dll" set "REFS=!REFS! /r:"!MANAGED!\mscorlib.dll""

if exist ExtraGrip.dll del ExtraGrip.dll
"!CSC!" /nologo /target:library /out:ExtraGrip.dll /nostdlib+ !REFS! ExtraGrip.cs
if errorlevel 1 (
  echo.
  echo BUILD FAILED - see errors above.
  pause & exit /b 1
)

REM ---- 4. Install ----
if not exist "!GT!\BepInEx\plugins" mkdir "!GT!\BepInEx\plugins"
copy /y ExtraGrip.dll "!GT!\BepInEx\plugins\ExtraGrip.dll" >nul
echo.
echo SUCCESS! Installed to !GT!\BepInEx\plugins
echo Launch the game. Settings are in BepInEx\config\com.extragrip.gtag.cfg
pause
