# MoreCoop - 深海迷航 2 多人人数解锁

把深海迷航 2 官方 4 人联机上限解锁到 4–64 人（默认 8 人），房主一键安装。

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Release](https://img.shields.io/github/v/release/wuha-like-sleep/SubnauticaMoreCoop)](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest)

> **派生作品声明**：本 mod 基于 [Zeusfail/Too-Many-Divers](https://github.com/Zeusfail/Too-Many-Divers) v1.2.0 的核心架构（UE5 反射路径、CDO 补丁、HostSessionAsync hook）精简而来，遵循其 GPL-3.0 协议同样以 GPL-3.0 发布。

## 一键使用（推荐）

到 [Releases](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest) 下载 **`MoreCoopManager.exe`**，双击运行。

界面里：
- 自动检测游戏路径、UE4SS、mod 安装状态
- **拖动滑块改人数** → 立即生效，不用关游戏
- 一个按钮装、一个按钮卸、一个按钮打开 Mod 目录

唯一前置：必须先装 [UE4SS for Subnautica 2](https://www.nexusmods.com/subnautica2/mods/36)（GUI 会检测并提示）。

## 其他安装方式

如果不想用 GUI，Releases 里也有：

- **`SubnauticaMoreCoop.zip`** — 解压后双击 `install.bat` 也行
- **`MoreCoop-mod-only.zip`** — 只有 mod 本体，手动拖到 `ue4ss/Mods/`

## 人数推荐

| 人数 | 评价 |
|------|------|
| 8 | 最稳，社区实测推荐（默认） |
| 16 | 可用，房主带宽/CPU 会有压力 |
| 32+ | 实验性质，预期会卡 |

**只有房主需要装本 mod**，其他玩家用原版客户端就能加入。P2P 联机，人数越多房主负担越重。

## 工作原理

UE4SS 在游戏运行时把 Lua 脚本注入 UE5 进程。脚本通过 UE5 的反射系统找到 4 个跟人数相关的类（`SN2GameSession`、`UWEOnlineSessionSubsystem`、`UWEHostSessionRequest`、`GameSession`），把它们的 `MaxPlayers` / `MaxSessionPlayerCount` 字段从 4 改成你设的值。同时 hook `HostSessionAsync` 函数，每次创建房间再改一次确保生效。

**完全可逆**：不修改任何游戏原文件，卸载 = 删 `ue4ss/Mods/MoreCoop/` 目录 + 改回 `mods.txt`。GUI 卸载按钮一键做完。

## 不工作怎么办

如果游戏更新了，类名或字段名可能变化：

1. 按 `Insert` 打开 UE4SS 控制台，看 `[MoreCoop]` 有没有报错
2. 看到 `CDO ... not found` 之类错误说明 UE5 反射路径变了
3. 这时建议改用社区维护的 [Too Many Divers](https://www.nexusmods.com/subnautica2/mods/73)，他们会跟随游戏版本更新

## 源码结构

```
SubnauticaMoreCoop/
├── MoreCoop/                  ← mod 本体 (UE4SS Lua)
│   ├── Scripts/main.lua       ← 补丁核心逻辑
│   ├── config/settings.json   ← 人数配置
│   └── enabled.txt
├── manager-app/               ← C# WinForms GUI 管理器源码
│   ├── MoreCoopManager.csproj
│   ├── Program.cs, MainForm.cs, ModInstaller.cs, SteamFinder.cs
│   └── Resources/             ← mod 文件作为内嵌资源
├── install.bat / uninstall.bat ← 命令行安装（备选）
├── installer.nsi              ← NSIS 安装包源码（备选）
└── LICENSE                    ← GPL-3.0
```

## 自己编译 GUI 管理器

```bash
cd manager-app
dotnet publish -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true \
  -o ./publish
```

需要 .NET 8 SDK。可以从 Mac/Linux 交叉编译 Windows .exe（项目里加了 `EnableWindowsTargeting`）。

## 致谢

- [Zeusfail/Too-Many-Divers](https://github.com/Zeusfail/Too-Many-Divers) — 补丁原理来源
- [UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) — UE5 注入框架
- Unknown Worlds — 深海迷航 2 开发商
