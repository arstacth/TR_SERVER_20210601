@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo.
echo ========================================
echo   TR SERVER - Clean Rebuild
echo ========================================
echo.

:: --- Find MSBuild (VS 2017/2019/2022 or Build Tools) ---
set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
  for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%I"
)
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" set "MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

if not defined MSBUILD (
  echo [ERROR] MSBuild was not found.
  echo Install Visual Studio or "Build Tools for Visual Studio" with:
  echo   - .NET desktop development
  echo   - .NET Framework 4.6.1 targeting pack
  echo.
  pause
  exit /b 1
)

:: Incomplete 4.6.1 packs (XML-only) make MSBuild fail; pick a real targeting pack.
set "REFROOT=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework"
set "TFV="
if exist "%REFROOT%\v4.6.1\mscorlib.dll" set "TFV=v4.6.1"
if not defined TFV if exist "%REFROOT%\v4.8\mscorlib.dll" set "TFV=v4.8"
if not defined TFV if exist "%REFROOT%\v4.7.2\mscorlib.dll" set "TFV=v4.7.2"
if not defined TFV if exist "%REFROOT%\v4.8.1\mscorlib.dll" set "TFV=v4.8.1"

echo Using MSBuild:
echo   %MSBUILD%
if defined TFV (
  echo Targeting pack: %TFV%
) else (
  echo [WARN] No .NET Framework targeting pack with mscorlib.dll was found.
  echo Install ".NET Framework 4.8 Developer Pack" if the build fails.
)
echo.

:: --- Stop running servers so EXE files are not locked ---
echo [1/4] Stopping running server processes...
taskkill /F /IM AgentServer.exe >nul 2>&1
taskkill /F /IM CommunityAgentServer.exe >nul 2>&1
taskkill /F /IM LoadBalanceServer.exe >nul 2>&1
taskkill /F /IM RelayServer.exe >nul 2>&1
taskkill /F /IM RoomServer.exe >nul 2>&1

:: --- Delete previous compiled EXEs only (keep settings, data, third-party DLLs) ---
echo [2/4] Deleting old compiled EXEs...
for %%E in (AgentServer CommunityAgentServer LoadBalanceServer RelayServer RoomServer) do (
  del /f /q "bin\%%E.exe" >nul 2>&1
  del /f /q "bin\%%E.pdb" >nul 2>&1
  del /f /q "packages\%%E.exe" >nul 2>&1
  del /f /q "packages\%%E.pdb" >nul 2>&1
  del /f /q "packages\compile\%%E.exe" >nul 2>&1
  del /f /q "packages\compile\%%E.pdb" >nul 2>&1
  del /f /q "packages\compile\%%E.exe.config" >nul 2>&1
)
del /f /q "packages\compile\LocalCommons.dll" >nul 2>&1
del /f /q "packages\compile\LocalCommons.pdb" >nul 2>&1

for %%P in (AgentServer CommunityAgentServer LoadBalanceServer RelayServer RoomServer LocalCommons) do (
  if exist "%%P\bin" rd /s /q "%%P\bin"
  if exist "%%P\obj" rd /s /q "%%P\obj"
)

if not exist "packages\compile" mkdir "packages\compile"
if not exist "bin" mkdir "bin"

:: --- Rebuild from source ---
echo [3/4] Rebuilding TR_SERVER.sln (Release)...
echo.
set "TFPROP="
if defined TFV set "TFPROP=/p:TargetFrameworkVersion=%TFV%"
"%MSBUILD%" "%~dp0TR_SERVER.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" %TFPROP% /m /nologo /v:minimal
if errorlevel 1 (
  echo.
  echo [ERROR] Build failed.
  pause
  exit /b 1
)

:: --- Install fresh EXEs into the runtime folder (bin\) ---
echo.
echo [4/4] Copying new EXEs into bin\ ...
set "MISSING=0"
for %%E in (AgentServer CommunityAgentServer LoadBalanceServer RelayServer RoomServer) do (
  if exist "packages\compile\%%E.exe" (
    copy /y "packages\compile\%%E.exe" "bin\%%E.exe" >nul
    echo   OK  %%E.exe
  ) else (
    echo   MISSING  packages\compile\%%E.exe
    set "MISSING=1"
  )
)

echo.
if "!MISSING!"=="1" (
  echo [ERROR] One or more EXEs were not produced.
  pause
  exit /b 1
)

echo ========================================
echo   Build finished.
echo ========================================
echo.
echo Runtime folder : %~dp0bin
echo Start servers  : START_SERVER.bat
echo Stop servers   : STOP_SERVER.bat
echo Settings       : bin\settings.ini
echo.
echo AgentServer needs hash.ini, settings.ini, and bin\TRServer.dll
echo next to the EXE (already expected under bin\).
echo.
pause
exit /b 0
