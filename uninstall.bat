@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title MoreCoop - Subnautica 2 - Uninstall

echo ====================================================
echo   MoreCoop - 卸载程序
echo ====================================================
echo.

echo 正在查找深海迷航 2 安装目录...

set "GAME_PATH="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$steam=(Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -ErrorAction SilentlyContinue).InstallPath; if(-not $steam){$steam=(Get-ItemProperty 'HKLM:\SOFTWARE\Valve\Steam' -ErrorAction SilentlyContinue).InstallPath}; if(-not $steam){exit}; $p=Join-Path $steam 'steamapps\common\Subnautica 2'; if(Test-Path $p){Write-Output $p; exit}; $vdf=Join-Path $steam 'steamapps\libraryfolders.vdf'; if(Test-Path $vdf){$c=Get-Content $vdf -Raw; foreach($m in [regex]::Matches($c,'\"path\"\s+\"([^\"]+)\"')){$lib=$m.Groups[1].Value -replace '\\\\','\'; $p=Join-Path $lib 'steamapps\common\Subnautica 2'; if(Test-Path $p){Write-Output $p; exit}}}"`) do set "GAME_PATH=%%i"

if "%GAME_PATH%"=="" (
    echo 未自动找到游戏。请手动输入安装路径:
    set /p "GAME_PATH=路径: "
)

set "MOD_DIR=%GAME_PATH%\Subnautica2\Binaries\Win64\ue4ss\Mods\MoreCoop"
set "MODS_TXT=%GAME_PATH%\Subnautica2\Binaries\Win64\ue4ss\Mods\mods.txt"

if not exist "%MOD_DIR%" (
    echo.
    echo MoreCoop 未安装或已被删除, 无需操作。
    echo.
    pause
    exit /b 0
)

echo.
echo 即将删除:
echo   - %MOD_DIR%
echo   - mods.txt 中的 MoreCoop 条目
echo.
echo 注意: 本操作只删除本 mod 的文件, 不影响 UE4SS 本身和游戏文件。
echo       游戏将完全恢复原版状态 (4 人上限)。
echo.
set /p "CONFIRM=确认卸载? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo 已取消。
    pause
    exit /b 0
)

echo.
echo 正在删除 mod 目录...
rd /s /q "%MOD_DIR%"
if exist "%MOD_DIR%" (
    echo [错误] 无法删除目录, 可能需要管理员权限或游戏正在运行。
    pause
    exit /b 1
)
echo   已删除

if exist "%MODS_TXT%" (
    echo 正在清理 mods.txt...
    findstr /V /B /C:"MoreCoop" "%MODS_TXT%" > "%MODS_TXT%.tmp" 2>nul
    move /Y "%MODS_TXT%.tmp" "%MODS_TXT%" >nul
    echo   已清理
)

echo.
echo ====================================================
echo   卸载完成！游戏已恢复原版。
echo ====================================================
echo.
pause
endlocal
