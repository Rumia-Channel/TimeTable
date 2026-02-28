@echo off
setlocal

set "CSC="

if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
) else if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not defined CSC (
    echo [ERROR] csc.exe was not found under the standard .NET Framework path.
    exit /b 1
)

set "ROOT=%~dp0"
set "OUTDIR=%ROOT%bin"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo Using compiler: "%CSC%"

"%CSC%" ^
  /nologo ^
  /target:winexe ^
  /platform:anycpu ^
  /optimize+ ^
  /out:"%OUTDIR%\TimeTableApp.exe" ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  "%ROOT%Program.cs" ^
  "%ROOT%MainForm.cs" ^
  "%ROOT%TimetableStorage.cs" ^
  "%ROOT%TimetableRenderer.cs"

if errorlevel 1 (
    echo [ERROR] Build failed.
    exit /b 1
)

if exist "%ROOT%Fonts" (
    xcopy "%ROOT%Fonts" "%OUTDIR%\Fonts\" /E /I /Y >nul
)

if exist "%ROOT%Data" (
    xcopy "%ROOT%Data" "%OUTDIR%\Data\" /E /I /Y >nul
)

echo [OK] Build succeeded.
echo Output: "%OUTDIR%\TimeTableApp.exe"
endlocal
