<div align="center">

![MoreCoop Manager](docs/banner.png)

# MoreCoop Manager

**深海迷航 2 多人人数解锁补丁** · 一键安装 · 内嵌 UE4SS · 4–64 人可调 · 完全可逆

[![License](https://img.shields.io/badge/license-GPL--3.0-blue.svg?style=flat-square)](LICENSE)
[![Release](https://img.shields.io/github/v/release/wuha-like-sleep/SubnauticaMoreCoop?style=flat-square&color=00bcd4)](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/wuha-like-sleep/SubnauticaMoreCoop/total?style=flat-square&color=43a047)](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases)
[![Build](https://img.shields.io/github/actions/workflow/status/wuha-like-sleep/SubnauticaMoreCoop/build-release.yml?style=flat-square)](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/actions)
[![Platform](https://img.shields.io/badge/platform-Windows-0078d4?style=flat-square)](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest)

[**📥 下载最新版**](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest/download/MoreCoopManager.exe) &nbsp;·&nbsp; [📦 所有版本](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases) &nbsp;·&nbsp; [🛠 源码](manager-app) &nbsp;·&nbsp; [❓ 常见问题](#-常见问题)

</div>

---

## ✨ 一句话

把官方 4 人联机上限解锁到 **可调 4–64 人**，**房主装就行**，朋友用原版客户端能直接加入。

> **派生作品声明**：本 mod 基于 [Zeusfail/Too-Many-Divers](https://github.com/Zeusfail/Too-Many-Divers) v1.2.0 的核心架构（UE5 反射路径、CDO 补丁、HostSessionAsync hook）精简而来，遵循 GPL-3.0 同步发布。

## 🚀 30 秒上手

1. [下载 `MoreCoopManager.exe`](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest/download/MoreCoopManager.exe)（75 MB，单文件）
2. 双击 → 允许 UAC → 看到 GUI
3. 拖滑块到想要的人数 → 点 **[一键安装]**
4. 进游戏建房，朋友加入

**就完了**。UE4SS 已经内嵌在 .exe 里，全程不用联网装别的东西。

## 📦 三种安装方式

| 文件 | 大小 | 适合 |
|---|---|---|
| 🟢 **`MoreCoopManager.exe`** | 75 MB | 想要 GUI 管理器、可调人数、可启动游戏、可应用内更新 |
| 🔵 `SubnauticaMoreCoop-Setup.exe` | 5 MB | 想要 Windows 标准安装向导、写入"程序和功能"列表 |
| ⚪ `SubnauticaMoreCoop.zip` | 20 KB | 命令行党，解压后双击 `install.bat` |

不带 UE4SS 的纯 mod：[`MoreCoop-mod-only.zip`](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest/download/MoreCoop-mod-only.zip)（2.6 KB，给已经有 UE4SS 的高级用户）

## 🎮 功能矩阵

|  | GUI (`MoreCoopManager.exe`) | NSIS (`Setup.exe`) | bat |
|---|:---:|:---:|:---:|
| 内嵌 UE4SS（无需另外下载） | ✅ | ✅ | — |
| 自动检测 Steam 库 + 游戏目录 | ✅ | ✅ | ✅ |
| 手选游戏目录回退 | ✅ | ✅ | ✅ |
| 一键装 / 卸（含 UE4SS 选择性卸载） | ✅ | ✅ | ✅ |
| 滑块改人数热生效 | ✅ | — | — |
| 游戏运行检测（防文件锁错误） | ✅ 3s 轮询 | ✅ 启动时 | — |
| Steam 一键启动游戏 | ✅ | — | — |
| 应用内 GitHub 更新检查 | ✅ | — | — |
| 仅热替换 mod 脚本（不重下管理器） | ✅ | — | — |
| 文件日志（`%APPDATA%\MoreCoop\manager.log`） | ✅ | — | — |
| 注册"程序和功能"列表 | — | ✅ | — |

## 🖼 界面预览

```
┌─ MoreCoop Manager · 深海迷航 2 多人解锁          v1.7.0 ─ ☐ × ─┐
│                                                                  │
│  状态                                                            │
│   ● 游戏:    [自动] D:\Steam\steamapps\common\Subnautica 2 [浏览] │
│   ● UE4SS:   已安装 (本程序装的)                                │
│   ● MoreCoop: 已安装 (当前 8 人)                                │
│                                                                  │
│  人数上限   (拖滑块即刻生效, 不用关游戏)                         │
│   [════●════════════════════════════]            [    8    ]    │
│                                                                  │
│  [一键安装]  [卸载]  [启动游戏]  [Mod 目录]  [关于/更新]          │
│                                                                  │
│  日志   (同时写到 %APPDATA%\MoreCoop\manager.log)               │
│  [15:30:01] MoreCoop Manager v1.7.0 启动                        │
│  [15:30:02] ✓ 检测到游戏: D:\Steam\steamapps\common\Subnaut...  │
│  [15:30:02] ✓ UE4SS 已就绪                                      │
│  [15:30:15] 人数已改为 12 (UE4SS 热生效, 下次创建房间即用)      │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

深色"深海"主题，窗口可自由调整尺寸。

## 🎯 推荐人数

| 人数 | 评价 |
|---|---|
| **8** | ⭐ 默认值，社区实测最稳 |
| **16** | ✅ 可用，房主带宽/CPU 会有压力 |
| 32 | ⚠️ 实验性，预期会卡 |
| 64 | 🧪 极限测试 |

P2P 联机，人数越多房主负担越重。建议 8–16。

## 🛠 修改器 / 作弊命令

**本工具不另写修改器**——但我们打包的 UE4SS 已经启用了游戏自带的开发者控制台，按 **F2** 打开（不是 F3）。

常用命令：

| 命令 | 效果 |
|---|---|
| `god` | 无敌（无伤害 + 无饥渴 + 无氧气消耗） |
| `nocost` | 免材料合成 |
| `attr oxygen 9999` | 锁定氧气值 |
| `attr food 100` / `attr water 100` | 锁定饱食/口渴 |
| `unlockall` | 解锁所有蓝图 |
| `item <name> <qty>` | 给自己物品 |
| `freecam` | 第三人称自由相机 |
| `fastbuild` | 即时建造 |

更完整的修改器推荐：

- [**Cheat Toggles**](https://www.nexusmods.com/subnautica2/mods/64) - Nexus 社区作品，UE4SS Lua mod，可视化开关，跟 MoreCoop 兼容
- [**FLiNG Trainer**](https://flingtrainer.com/trainer/subnautica-2-trainer/) - 124+ 选项外挂，免费
- [**WeMod**](https://community.wemod.com/t/subnautica-2-cheats-and-trainer-for-steam/376686) - 跨游戏修改器平台

## ❓ 常见问题

<details>
<summary><b>Windows 弹"已保护你的电脑"怎么办？</b></summary>

正常现象。任何**未签名的 .exe** 都会被 SmartScreen 拦。代码签名证书一年 $200+，本项目用爱发电没买。

点 **"更多信息"** → **"仍要运行"** 即可。

如果你在意，[读源码](manager-app)然后自己编译。

</details>

<details>
<summary><b>UAC 弹"是否允许此应用进行更改"，安全吗？</b></summary>

要管理员权限是因为 Steam 默认装在 `Program Files (x86)`，需要写权限。Steam 在 D 盘自定义目录的话其实可以不要 admin，但代码统一要求免得分情况。

整个程序就做四件事：扫 Steam 注册表、解压 zip 到游戏目录、写 settings.json、改 mods.txt。**不联网（除了你点检查更新）、不收集数据、不改注册表（除了存你手选的游戏路径）**。

</details>

<details>
<summary><b>朋友也要装吗？</b></summary>

**只有房主装**。朋友用原版客户端就能加入。是 P2P 联机所以房主的修改决定了房间人数上限。

</details>

<details>
<summary><b>游戏更新了 mod 还能用吗？</b></summary>

我们 hook 的是 UE5 反射路径（`SN2GameSession.MaxPlayers` 等）。游戏更新如果改了这些类名/字段名，mod 会失效。

发现失效后：
1. 按 Insert 打开 UE4SS 控制台看 `[MoreCoop]` 报错
2. 在 GUI 点 [关于/更新] 检查有没有新版补丁，有就点 [仅更新 mod 脚本]
3. 还不行就到 [Issues](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/issues) 报告，或临时改用社区维护的 [Too Many Divers](https://www.nexusmods.com/subnautica2/mods/73)

</details>

<details>
<summary><b>跟其他 UE4SS mod 冲突吗？</b></summary>

不冲突。我们的 mod 只 hook 跟人数相关的 4 个类。如果你已经装过 UE4SS（比如装过 Cheat Toggles 或 Console Commands），管理器会检测到并跳过 UE4SS 安装，只追加 MoreCoop。

卸载时会问你是否一起卸 UE4SS（防止误删别的 mod 依赖的 UE4SS）。

</details>

<details>
<summary><b>怎么完全卸载？</b></summary>

- **用 GUI 装的**：打开 MoreCoopManager.exe → [卸载]
- **用 NSIS 装的**：控制面板 → 程序和功能 → MoreCoop → 卸载
- **用 bat 装的**：双击 `uninstall.bat`
- **手动**：删 `<游戏目录>\Subnautica2\Binaries\Win64\ue4ss\Mods\MoreCoop\` 文件夹，把 `mods.txt` 里 `MoreCoop : 1` 行删掉

不管哪种方式都**不动游戏原文件**，恢复 4 人原版。

</details>

## 🔧 工作原理

UE4SS 在游戏运行时把 Lua 脚本注入 UE5 进程。脚本通过 UE5 的反射系统找到 4 个跟人数相关的类（`SN2GameSession`、`UWEOnlineSessionSubsystem`、`UWEHostSessionRequest`、`GameSession`），把 `MaxPlayers` / `MaxSessionPlayerCount` 字段从 4 改成你设的值。同时 hook `HostSessionAsync` 函数，每次创建房间再改一次确保生效。

完整代码在 [`MoreCoop/Scripts/main.lua`](MoreCoop/Scripts/main.lua)（约 80 行 Lua）。

## 🏗 自己编译

### GUI 管理器（C# WinForms）
```bash
cd manager-app
dotnet publish -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true \
  -o publish
```
需要 .NET 8 SDK。可从 Mac/Linux 交叉编译（项目里设了 `EnableWindowsTargeting`）。

### NSIS 安装包
```bash
# 先解压 UE4SS 到 ue4ss-extracted/
unzip manager-app/Resources/UE4SS_SN2.zip -d ue4ss-extracted
# 编译（需要 NSIS 3.x）
makensis installer.nsi
```

GitHub Actions Workflow 在 Windows runner 上自动做这两件事——参考 `.github/workflows/build-release.yml`。

## 📁 仓库结构

```
SubnauticaMoreCoop/
├── MoreCoop/                     # mod 本体 (UE4SS Lua)
│   ├── Scripts/main.lua          # 补丁核心
│   ├── config/settings.json      # 人数配置
│   └── enabled.txt
├── manager-app/                  # C# WinForms GUI 管理器
│   ├── MoreCoopManager.csproj
│   ├── Program.cs                # 入口
│   ├── MainForm.cs               # GUI
│   ├── Theme.cs                  # 深色主题色板
│   ├── ModernControls.cs         # CardPanel / ModernButton / StatusRow
│   ├── SteamFinder.cs            # 找游戏路径
│   ├── ModInstaller.cs           # 装/卸/改人数
│   ├── UpdateChecker.cs          # GitHub API 检查更新
│   ├── FileLog.cs                # %APPDATA%\MoreCoop\manager.log
│   ├── icon.ico                  # 应用图标 (7 尺寸)
│   └── Resources/                # 内嵌资源
│       ├── main.lua              # mod 脚本副本
│       ├── settings.json
│       ├── LICENSE
│       └── UE4SS_SN2.zip         # 内嵌的 UE4SS (6.8 MB)
├── installer.nsi                 # NSIS 安装包源码
├── install.bat / uninstall.bat   # bat 命令行
├── .github/workflows/            # Windows runner CI
├── docs/banner.png               # README banner
├── LICENSE                       # GPL-3.0
└── README.md
```

## 📜 许可与归属

- 本程序: **GPL-3.0** © wuha-like-sleep
- 补丁原理: [Zeusfail/Too-Many-Divers](https://github.com/Zeusfail/Too-Many-Divers) v1.2.0 (GPL-3.0)
- 内嵌 UE4SS: [Subnautica2Modding/Subnautica2-UE4SS](https://github.com/Subnautica2Modding/Subnautica2-UE4SS) 1.0.0-pre.1 (**MIT**, © Narknon)
- 上游框架: [UE4SS-RE/RE-UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) (MIT)
- 游戏: 深海迷航 2 by [Unknown Worlds](https://unknownworlds.com/)

---

<div align="center">

[**永久最新版下载**](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest/download/MoreCoopManager.exe) · [Issues](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/issues) · [发送 Pull Request](https://github.com/wuha-like-sleep/SubnauticaMoreCoop/pulls)

Made with ☕ for friends who got tired of "sorry only 4 of us can play"

</div>
