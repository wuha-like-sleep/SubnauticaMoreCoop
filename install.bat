@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title MoreCoop - Subnautica 2 - Install

echo ====================================================
echo   MoreCoop - 深海迷航 2 多人人数解锁
echo   安装程序 v1.0.0
echo ====================================================
echo.

REM ------------------------------------------------------------
REM  Step 1: Locate Subnautica 2 install path
REM ------------------------------------------------------------
echo [1/4] 正在查找深海迷航 2 安装目录...

set "GAME_PATH="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$steam=(Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -ErrorAction SilentlyContinue).InstallPath; if(-not $steam){$steam=(Get-ItemProperty 'HKLM:\SOFTWARE\Valve\Steam' -ErrorAction SilentlyContinue).InstallPath}; if(-not $steam){exit}; $p=Join-Path $steam 'steamapps\common\Subnautica 2'; if(Test-Path $p){Write-Output $p; exit}; $vdf=Join-Path $steam 'steamapps\libraryfolders.vdf'; if(Test-Path $vdf){$c=Get-Content $vdf -Raw; foreach($m in [regex]::Matches($c,'\"path\"\s+\"([^\"]+)\"')){$lib=$m.Groups[1].Value -replace '\\\\','\'; $p=Join-Path $lib 'steamapps\common\Subnautica 2'; if(Test-Path $p){Write-Output $p; exit}}}"`) do set "GAME_PATH=%%i"

if "%GAME_PATH%"=="" (
    echo.
    echo 未通过 Steam 注册表自动找到游戏。
    echo 请手动输入深海迷航 2 的安装路径
    echo 示例: C:\Program Files ^(x86^)\Steam\steamapps\common\Subnautica 2
    echo.
    set /p "GAME_PATH=路径: "
)

if not exist "%GAME_PATH%" (
    echo.
    echo [错误] 路径不存在: %GAME_PATH%
    pause
    exit /b 1
)

echo   找到游戏: %GAME_PATH%
echo.

REM ------------------------------------------------------------
REM  Step 2: Check UE4SS is installed
REM ------------------------------------------------------------
echo [2/4] 检查 UE4SS 是否已安装...

set "UE4SS_DIR=%GAME_PATH%\Subnautica2\Binaries\Win64\ue4ss"
if not exist "%UE4SS_DIR%" (
    echo.
    echo [错误] 未检测到 UE4SS。本 mod 依赖 UE4SS 才能运行。
    echo.
    echo 请先安装 UE4SS:
    echo   https://www.nexusmods.com/subnautica2/mods/36
    echo.
    echo 装完 UE4SS 后再次运行本安装程序。
    pause
    exit /b 1
)
echo   UE4SS 已就绪
echo.

REM ------------------------------------------------------------
REM  Step 3: Copy mod files
REM ------------------------------------------------------------
echo [3/4] 复制 mod 文件...

set "MOD_DEST=%UE4SS_DIR%\Mods\MoreCoop"
if exist "%MOD_DEST%" (
    echo   检测到已存在的 MoreCoop, 正在覆盖...
)
xcopy /E /I /Y /Q "%~dp0MoreCoop" "%MOD_DEST%" >nul
if errorlevel 1 (
    echo [错误] 复制失败, 可能需要以管理员身份运行。
    pause
    exit /b 1
)
echo   已复制到 %MOD_DEST%
echo.

REM ------------------------------------------------------------
REM  Step 4: Register mod in mods.txt
REM ------------------------------------------------------------
echo [4/4] 注册 mod...

set "MODS_TXT=%UE4SS_DIR%\Mods\mods.txt"
if not exist "%MODS_TXT%" (
    echo MoreCoop : 1> "%MODS_TXT%"
    echo   创建新的 mods.txt
) else (
    findstr /B /C:"MoreCoop" "%MODS_TXT%" >nul 2>&1
    if errorlevel 1 (
        echo.>> "%MODS_TXT%"
        echo MoreCoop : 1>> "%MODS_TXT%"
        echo   已添加到 mods.txt
    ) else (
        echo   mods.txt 已包含 MoreCoop 条目, 跳过
    )
)
echo.

echo ====================================================
echo   安装成功！
echo ====================================================
echo.
echo   - 启动深海迷航 2 即可生效
echo   - 按 Insert 键打开 UE4SS 控制台查看日志
echo   - 改人数: 编辑 %MOD_DEST%\config\settings.json
echo   - 只有房主需要装, 朋友用原版可直接加入
echo.
echo   卸载: 双击 uninstall.bat 即可完全清理
echo.
pause
endlocal
