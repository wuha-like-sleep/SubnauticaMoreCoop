// MoreCoop Manager - Subnautica 2 多人人数解锁补丁管理器
// Copyright (C) 2026 wuha-like-sleep, GPL-3.0
// Derived from Zeusfail/Too-Many-Divers

namespace MoreCoopManager;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new MainForm());
    }
}
