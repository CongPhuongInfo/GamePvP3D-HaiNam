@echo off
setlocal enabledelayedexpansion

set VBC=

for %%v in (
    "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe"
    "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\vbc.exe"
) do (
    if exist %%v (
        set "VBC=%%~v"
        goto :found
    )
)

:found
if "%VBC%"=="" (
    echo [LOI] Khong tim thay vbc.exe. Vui long cai .NET Framework 4.x
    pause
    exit /b 1
)

echo Dang bien dich bang: %VBC%
echo.

"%VBC%" /target:winexe /out:GamePvP3D_HaiNam.exe /optimize+ /optionstrict+ /optionexplicit+ ^
    /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll ^
    Form1.vb

if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai.
    pause
    exit /b 1
)

echo.
echo [OK] Bien dich thanh cong: GamePvP3D_HaiNam.exe
endlocal
