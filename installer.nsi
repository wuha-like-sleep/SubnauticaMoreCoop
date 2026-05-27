; MoreCoop - Subnautica 2 多人人数解锁 - NSIS Installer
; Build with: makensis installer.nsi   (NSIS 3.x)
; License: GPL-3.0

Unicode true
SetCompressor /SOLID lzma

; ----------------------------------------------------------------
; App metadata
; ----------------------------------------------------------------
!define APP_NAME      "MoreCoop"
!define APP_VERSION   "1.9.1"
!define APP_PUBLISHER "wuha-like-sleep"
!define APP_URL       "https://github.com/wuha-like-sleep/SubnauticaMoreCoop"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

Name "MoreCoop - 深海迷航 2 多人解锁"
OutFile "SubnauticaMoreCoop-Setup.exe"
RequestExecutionLevel admin
BrandingText "MoreCoop v${APP_VERSION} - GPL-3.0"

; ----------------------------------------------------------------
; Variables  (MUST be declared before MUI macros that reference them)
; ----------------------------------------------------------------
Var GAME_DIR
Var UE4SS_DIR
Var MOD_DIR
Var MODS_TXT

; ----------------------------------------------------------------
; Modern UI 2
; ----------------------------------------------------------------
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "WordFunc.nsh"   ; provides ${WordFind}, ${WordReplace}

; Use our app icon if present, otherwise NSIS default
!define MUI_ICON   "manager-app\icon.ico"
!define MUI_UNICON "manager-app\icon.ico"
!define MUI_ABORTWARNING

; Installer pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"

!define MUI_DIRECTORYPAGE_TEXT_TOP "请确认深海迷航 2 的安装目录 (已自动从 Steam 注册表检测; 不对就手动改)。$\r$\n$\r$\n路径应指向游戏根目录, 含 Subnautica2 子目录, 例如:$\r$\nC:\Program Files (x86)\Steam\steamapps\common\Subnautica 2"
!define MUI_DIRECTORYPAGE_VARIABLE $GAME_DIR
!insertmacro MUI_PAGE_DIRECTORY

!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "English"

; ----------------------------------------------------------------
; Auto-detect Subnautica 2 install path via Steam registry +
; libraryfolders.vdf scan
; ----------------------------------------------------------------
Function .onInit
    ; Try detecting via Steam registry (32-bit or 64-bit hive)
    ReadRegStr $0 HKLM "SOFTWARE\WOW6432Node\Valve\Steam" "InstallPath"
    ${If} $0 == ""
        ReadRegStr $0 HKLM "SOFTWARE\Valve\Steam" "InstallPath"
    ${EndIf}

    ${If} $0 != ""
        ; Check default library
        ${If} ${FileExists} "$0\steamapps\common\Subnautica 2\*.*"
            StrCpy $GAME_DIR "$0\steamapps\common\Subnautica 2"
            Return
        ${EndIf}

        ; Walk libraryfolders.vdf for other Steam libraries
        ${If} ${FileExists} "$0\steamapps\libraryfolders.vdf"
            ClearErrors
            FileOpen $1 "$0\steamapps\libraryfolders.vdf" r
            ${IfNot} ${Errors}
                vdf_loop:
                    FileRead $1 $2
                    ${If} ${Errors}
                        FileClose $1
                        Goto fallback
                    ${EndIf}
                    ; Looking for: "path"   "C:\\some\\path"
                    StrCpy $3 $2 7
                    ${If} $3 == '$\t"path"'
                    ${OrIf} $3 == '  "path'
                        ${WordFind} $2 '"' "+3{*+1" $4
                        ${WordFind} $4 '"' "+1{"   $4
                        ${WordReplace} $4 "\\" "\" "+" $4
                        ${If} ${FileExists} "$4\steamapps\common\Subnautica 2\*.*"
                            StrCpy $GAME_DIR "$4\steamapps\common\Subnautica 2"
                            FileClose $1
                            Return
                        ${EndIf}
                    ${EndIf}
                    Goto vdf_loop
            ${EndIf}
        ${EndIf}
    ${EndIf}

    fallback:
    ${If} ${FileExists} "$PROGRAMFILES32\Steam\steamapps\common\Subnautica 2\*.*"
        StrCpy $GAME_DIR "$PROGRAMFILES32\Steam\steamapps\common\Subnautica 2"
    ${ElseIf} ${FileExists} "$PROGRAMFILES64\Steam\steamapps\common\Subnautica 2\*.*"
        StrCpy $GAME_DIR "$PROGRAMFILES64\Steam\steamapps\common\Subnautica 2"
    ${Else}
        StrCpy $GAME_DIR "C:\Program Files (x86)\Steam\steamapps\common\Subnautica 2"
    ${EndIf}
FunctionEnd

; ----------------------------------------------------------------
; Install Section
; ----------------------------------------------------------------
Section "Install" SecInstall

    !define WIN64_DIR "$GAME_DIR\Subnautica2\Binaries\Win64"
    StrCpy $UE4SS_DIR "${WIN64_DIR}\ue4ss"
    StrCpy $MOD_DIR   "$UE4SS_DIR\Mods\MoreCoop"
    StrCpy $MODS_TXT  "$UE4SS_DIR\Mods\mods.txt"

    ; Refuse if the game is running (would lock the dwmapi.dll proxy)
    nsExec::ExecToStack 'cmd /c tasklist /FI "IMAGENAME eq Subnautica2-Win64-Shipping.exe" | findstr /I /C:"Subnautica2-Win64-Shipping"'
    Pop $0
    ${If} $0 == "0"
        MessageBox MB_ICONSTOP|MB_OK "深海迷航 2 正在运行, 无法安装。$\r$\n$\r$\n请先退出游戏 (退到桌面), 再运行安装程序。"
        Abort
    ${EndIf}

    ; Verify game directory looks right
    ${IfNot} ${FileExists} "${WIN64_DIR}\*.*"
        MessageBox MB_ICONSTOP|MB_OK "游戏目录看起来不对, 找不到:$\r$\n${WIN64_DIR}$\r$\n$\r$\n请确认你选的是深海迷航 2 的根目录。"
        Abort
    ${EndIf}

    ; Install UE4SS if not already present (extracted from this installer)
    ${IfNot} ${FileExists} "$UE4SS_DIR\*.*"
        DetailPrint "未检测到 UE4SS, 正在安装内嵌的 UE4SS 7 MB..."
        SetOutPath "${WIN64_DIR}"
        File "ue4ss-extracted\dwmapi.dll"
        File /r "ue4ss-extracted\ue4ss"
        ; Marker so uninstall knows we installed UE4SS
        FileOpen $0 "$UE4SS_DIR\installed-by-morecoop.txt" w
        FileWrite $0 "Installed by SubnauticaMoreCoop-Setup.exe on ${__TIMESTAMP__}$\r$\nRemove this file to prevent uninstaller from also removing UE4SS.$\r$\n"
        FileClose $0
    ${Else}
        DetailPrint "UE4SS 已存在, 跳过 UE4SS 安装"
    ${EndIf}

    ; Backup mods.txt before modifying (so uninstall can restore)
    ${If} ${FileExists} "$MODS_TXT"
        CopyFiles /SILENT "$MODS_TXT" "$UE4SS_DIR\Mods\mods.txt.morecoop-backup"
    ${EndIf}

    ; Copy mod payload
    CreateDirectory "$MOD_DIR"
    SetOutPath "$MOD_DIR"
    File "MoreCoop\enabled.txt"

    CreateDirectory "$MOD_DIR\Scripts"
    SetOutPath "$MOD_DIR\Scripts"
    File "MoreCoop\Scripts\main.lua"

    CreateDirectory "$MOD_DIR\config"
    SetOutPath "$MOD_DIR\config"
    File "MoreCoop\config\settings.json"

    SetOutPath "$MOD_DIR"
    File "LICENSE"
    File "README.md"

    ; Register in mods.txt (idempotent)
    ${If} ${FileExists} "$MODS_TXT"
        nsExec::ExecToStack 'findstr /B /C:"MoreCoop" "$MODS_TXT"'
        Pop $0
        ${If} $0 != "0"
            FileOpen $1 "$MODS_TXT" a
            FileSeek $1 0 END
            FileWrite $1 "$\r$\nMoreCoop : 1$\r$\n"
            FileClose $1
        ${EndIf}
    ${Else}
        FileOpen $1 "$MODS_TXT" w
        FileWrite $1 "MoreCoop : 1$\r$\n"
        FileClose $1
    ${EndIf}

    ; Write uninstaller into the mod directory
    WriteUninstaller "$MOD_DIR\uninstall.exe"

    ; Register in "Programs and Features"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName"     "MoreCoop - 深海迷航 2 多人解锁"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion"  "${APP_VERSION}"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher"       "${APP_PUBLISHER}"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "URLInfoAbout"    "${APP_URL}"
    WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" '"$MOD_DIR\uninstall.exe"'
    WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$MOD_DIR"
    WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1

SectionEnd

; ----------------------------------------------------------------
; Uninstall Section
; ----------------------------------------------------------------
Section "Uninstall"

    ; Refuse if the game is running
    nsExec::ExecToStack 'cmd /c tasklist /FI "IMAGENAME eq Subnautica2-Win64-Shipping.exe" | findstr /I /C:"Subnautica2-Win64-Shipping"'
    Pop $0
    ${If} $0 == "0"
        MessageBox MB_ICONSTOP|MB_OK "深海迷航 2 正在运行, 无法卸载。$\r$\n$\r$\n请先退出游戏, 再运行卸载程序。"
        Abort
    ${EndIf}

    ; $INSTDIR is the dir where uninstall.exe lives = the mod folder
    StrCpy $MODS_TXT "$INSTDIR\..\mods.txt"

    ; Remove mod files
    Delete "$INSTDIR\enabled.txt"
    Delete "$INSTDIR\Scripts\main.lua"
    Delete "$INSTDIR\config\settings.json"
    Delete "$INSTDIR\LICENSE"
    Delete "$INSTDIR\README.md"
    Delete "$INSTDIR\uninstall.exe"
    RMDir  "$INSTDIR\Scripts"
    RMDir  "$INSTDIR\config"
    RMDir  "$INSTDIR"

    ; Restore mods.txt from backup if present, otherwise strip the MoreCoop line
    ${If} ${FileExists} "$INSTDIR\..\mods.txt.morecoop-backup"
        Delete "$MODS_TXT"
        Rename "$INSTDIR\..\mods.txt.morecoop-backup" "$MODS_TXT"
    ${ElseIf} ${FileExists} "$MODS_TXT"
        nsExec::Exec 'powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content \"$MODS_TXT\") | Where-Object { $_ -notmatch \"^MoreCoop\" } | Set-Content \"$MODS_TXT\""'
    ${EndIf}

    ; If we installed UE4SS (marker file present), offer to remove it too.
    ; $INSTDIR\..\..  =  Win64\ue4ss  →  marker at $INSTDIR\..\..\installed-by-morecoop.txt
    ${If} ${FileExists} "$INSTDIR\..\..\installed-by-morecoop.txt"
        MessageBox MB_ICONQUESTION|MB_YESNO "UE4SS 是本程序之前装的, 是否一起卸掉?$\r$\n$\r$\n[是] 一起卸 (游戏完全恢复原版)$\r$\n[否] 只卸 MoreCoop, 保留 UE4SS (以后装其他 UE4SS mod 用)" IDNO skip_ue4ss
            ; Yes — remove UE4SS folder and proxy DLL
            RMDir /r "$INSTDIR\..\.."                                   ; ue4ss\
            Delete  "$INSTDIR\..\..\..\dwmapi.dll"                       ; Win64\dwmapi.dll
        skip_ue4ss:
    ${EndIf}

    DeleteRegKey HKLM "${UNINSTALL_KEY}"

    MessageBox MB_ICONINFORMATION|MB_OK "MoreCoop 已卸载, 游戏已恢复原版状态 (4 人上限)。"

SectionEnd
