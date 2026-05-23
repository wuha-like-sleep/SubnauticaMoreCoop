# MoreCoop - 深海迷航2 多人人数解锁

把官方 4 人上限改成 8 人（可在 config 里改到 4–64）。

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

> **派生作品声明**: 本 mod 基于 [Zeusfail/Too-Many-Divers](https://github.com/Zeusfail/Too-Many-Divers) v1.2.0 的核心架构（UE5 反射路径、CDO 补丁、HostSessionAsync hook）精简而来，遵循其 GPL-3.0 协议同样以 GPL-3.0 发布。

## 前置条件

需要先装 UE4SS（Unreal 引擎注入器），深海迷航 2 的 UE4SS 在 Nexus 上：
https://www.nexusmods.com/subnautica2/mods/36

按 Nexus 上的说明装好 UE4SS（通常是把 `dwmapi.dll` 之类的文件丢到游戏 Win64 目录），先确认 UE4SS 本身能加载（按 `Insert` 键能弹出控制台就算成功）。

## 安装本 mod

1. 把整个 `MoreCoop/` 文件夹复制到：
   ```
   <Steam 库>\steamapps\common\Subnautica 2\Subnautica2\Binaries\Win64\ue4ss\Mods\
   ```
   （这是 UE4SS 默认的 Mods 目录，如果你装的时候改过路径就放对应位置）

2. 编辑 UE4SS 的 `mods.txt`（在 `ue4ss\Mods\` 目录下），加一行：
   ```
   MoreCoop : 1
   ```

3. 启动游戏，按 `Insert` 打开 UE4SS 控制台，应该能看到 `[MoreCoop] 加载完成, 人数上限 = 8` 这样的日志。

## 修改人数

编辑 `MoreCoop/config/settings.json`：
```json
{
  "MaxPlayers": 16
}
```

范围 4–64。**只有房主需要装这个 mod**，其他玩家用原版就能加入。

## 推荐人数

- **8**：最稳，社区实测推荐
- **16**：可用，房主带宽/CPU 会有压力
- **32+**：实验性质，预期会卡

## 工作原理

UE4SS 在游戏运行时把 Lua 脚本注入到 UE5 进程里。脚本通过 UE5 的反射系统找到 4 个跟人数相关的类（`SN2GameSession`、`UWEOnlineSessionSubsystem`、`UWEHostSessionRequest`、`GameSession`），把它们的 `MaxPlayers` / `MaxSessionPlayerCount` 字段从 4 改成你设的值。同时 hook `HostSessionAsync` 函数，在每次创建房间的时候再改一次确保生效。

不修改任何游戏文件，删掉 `MoreCoop/` 文件夹就完全卸载。

## 不工作怎么办

如果游戏更新了，类名或字段名可能变化，需要：

1. 按 `Insert` 打开 UE4SS 控制台，看有没有 `[MoreCoop]` 报错
2. 如果 `CDO ... not found` 或类似错误，说明 UE5 反射路径变了
3. 这时建议直接用社区维护的 Too Many Divers，他们会跟随游戏版本更新

社区版本（更全功能，带游戏内 UI 调节）：
- https://www.nexusmods.com/subnautica2/mods/73
- https://github.com/Zeusfail/Too-Many-Divers

## 致谢

补丁原理参考自 Zeusfail/Too-Many-Divers (v1.2.0)，本版本是精简后的最小可用实现。
