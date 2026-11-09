@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe nicht gefunden: %CSC%
  pause
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:OrchestratorLock.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll OrchestratorLock.cs
if errorlevel 1 (
  echo.
  echo BUILD FEHLGESCHLAGEN
) else (
  echo.
  echo BUILD OK: %~dp0OrchestratorLock.exe
)
pause
