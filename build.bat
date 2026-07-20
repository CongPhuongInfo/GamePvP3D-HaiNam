@echo off
setlocal enabledelayedexpansion

rem =====================================================================
rem  Build script cho GamePvP3D_HaiNam
rem  Cau truc thu muc:
rem    src\    -> toan bo ma nguon .vb (khong dong cham gi den o day)
rem    bin\    -> noi xuat ra file .exe SAU KHI build, kem theo ban sao
rem               thu muc Assets\ (de exe chay duoc ma khong can chinh
rem               sua gi them, chi can mo bin\GamePvP3D_HaiNam.exe)
rem    Assets\ -> texture/sprite goc, KHONG sua/xoa, build.bat tu dong
rem               copy sang bin\Assets\ moi lan build
rem =====================================================================

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

if not exist bin mkdir bin

"%VBC%" /target:winexe /out:bin\GamePvP3D_HaiNam.exe /optimize+ /optionstrict+ /optionexplicit+ ^
    /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll ^
    src\Form1.vb src\GameInput.vb src\GameCombat.vb src\GameHub.vb src\GameAssets.vb src\GameWorld.vb src\GameRender.vb src\GameHud.vb ^
    src\GameMaps.vb src\ConnectForm.vb src\GameModels.vb src\NetworkHub.vb src\NetworkPeer.vb

if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai.
    pause
    exit /b 1
)

if not exist Assets (
    echo [CANH BAO] Khong thay thu muc Assets\ o thu muc goc - bin\ se khong co texture.
) else (
    echo Dang copy Assets\ vao bin\Assets\ ...
    xcopy /E /I /Y /Q Assets bin\Assets >nul
    echo Da copy xong.
)

echo.
echo [OK] Bien dich thanh cong: bin\GamePvP3D_HaiNam.exe
echo [OK] Chay game bang cach mo file bin\GamePvP3D_HaiNam.exe
endlocal
