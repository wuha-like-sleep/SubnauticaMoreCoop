using System.Diagnostics;
using Microsoft.Win32;

namespace MoreCoopManager;

/// <summary>
/// Main window. v1.9 adds the Quick Cheats hotkey card:
///   - Off by default (privacy + safety: doesn't hijack global keys uninvited)
///   - When user toggles ON: registers Ctrl+F1..F6 system-wide.
///   - Each hotkey writes its console command to %APPDATA%\MoreCoop\cheats.cmd
///   - The QuickCheats Lua mod (bundled and installed alongside MoreCoop)
///     polls the file and executes the command via PlayerController:ConsoleCommand
///   - Enable/disable state persists to HKCU\Software\MoreCoop\HotkeysEnabled
/// </summary>
internal sealed class MainForm : Form
{
    private const string Title = "MoreCoop Manager · 深海迷航 2 多人解锁";
    private const string GithubUrl = "https://github.com/wuha-like-sleep/SubnauticaMoreCoop";
    private const int SubnauticaSteamAppId = 1962700;
    private const string GameProcessName = "Subnautica2-Win64-Shipping";

    // Hotkey persistence
    private const string HotkeyRegKey = @"Software\MoreCoop";
    private const string HotkeyRegValue = "HotkeysEnabled";

    /// <summary>Default cheat mappings. Order = display order.</summary>
    private static readonly (HotkeyManager.Mods Mods, Keys Key, string Command, string Desc)[] CheatMappings =
    {
        (HotkeyManager.Mods.Control, Keys.F1, "god",                "无敌 (无伤害 + 无饥渴 + 无氧气消耗)"),
        (HotkeyManager.Mods.Control, Keys.F2, "nocost",             "免材料合成"),
        (HotkeyManager.Mods.Control, Keys.F3, "unlockall",          "解锁所有蓝图"),
        (HotkeyManager.Mods.Control, Keys.F4, "fastbuild",          "即时建造"),
        (HotkeyManager.Mods.Control, Keys.F5, "freecam",            "切换自由相机"),
        (HotkeyManager.Mods.Control, Keys.F6, "attr oxygen 9999",   "氧气补满"),
    };

    private readonly StatusRow _rowGame, _rowUE4SS, _rowMod;
    private readonly ModernButton _btnBrowseGame;
    private readonly TrackBar _trkPlayers;
    private readonly NumericUpDown _numPlayers;
    private readonly ModernButton _btnInstall, _btnUninstall, _btnLaunch, _btnCheats, _btnFolder, _btnAbout;
    private readonly ModernButton _btnHotkeyToggle;
    private readonly Label _lblHotkeyStatus;
    private readonly TextBox _txtLog;
    private readonly Color _aboutBaseColor;
    private readonly System.Windows.Forms.Timer _processCheckTimer;
    private readonly HotkeyManager _hotkeys;

    private ModInstaller? _installer;
    private bool _suppressSliderEvents;
    private UpdateChecker.UpdateInfo? _availableUpdate;
    private bool _hotkeysEnabled;

    public MainForm()
    {
        Text = $"{Title}  v{UpdateChecker.CurrentVersion}";
        ClientSize = new Size(880, 760);
        MinimumSize = new Size(820, 660);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font = Theme.BodyFont;
        Padding = new Padding(14);

        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { /* fall back */ }

        var tt = new ToolTip();

        // ──────────────────────────────────────────────────────────────
        // STATUS card  y=14 h=130
        // ──────────────────────────────────────────────────────────────
        var cardStatus = new CardPanel
        {
            CardTitle = "状态",
            Location = new Point(14, 14),
            Size = new Size(ClientSize.Width - 28, 130),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _rowGame = new StatusRow
        {
            Label = "游戏:",
            Value = "(检测中...)",
            Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 8),
            Size = new Size(cardStatus.Width - 2 * CardPanel.InnerPadding - 130, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _btnBrowseGame = ModernButton.Secondary("浏览...");
        _btnBrowseGame.Size = new Size(110, 30);
        _btnBrowseGame.Location = new Point(cardStatus.Width - CardPanel.InnerPadding - 110, CardPanel.HeaderHeight + 6);
        _btnBrowseGame.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnBrowseGame.Click += (_, _) => OnBrowseGame();
        tt.SetToolTip(_btnBrowseGame, "如果自动检测没找到游戏 (或找错了), 点这里手动选游戏根目录");

        _rowUE4SS = new StatusRow
        {
            Label = "UE4SS:",
            Value = "(检测中...)",
            Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 42),
            Size = new Size(cardStatus.Width - 2 * CardPanel.InnerPadding, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _rowMod = new StatusRow
        {
            Label = "MoreCoop:",
            Value = "(检测中...)",
            Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 70),
            Size = new Size(cardStatus.Width - 2 * CardPanel.InnerPadding, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        cardStatus.Controls.AddRange([_rowGame, _btnBrowseGame, _rowUE4SS, _rowMod]);
        Controls.Add(cardStatus);

        // ──────────────────────────────────────────────────────────────
        // PLAYER COUNT card  y=156 h=115
        // ──────────────────────────────────────────────────────────────
        var cardPlayers = new CardPanel
        {
            CardTitle = "人数上限   (拖动滑块即可立即生效, 无需重启游戏)",
            Location = new Point(14, 156),
            Size = new Size(ClientSize.Width - 28, 115),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _trkPlayers = new TrackBar
        {
            Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 8),
            Size = new Size(cardPlayers.Width - 2 * CardPanel.InnerPadding - 180, 50),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            TickFrequency = 4,
            LargeChange = 4,
            BackColor = Theme.CardBackground,
        };

        _numPlayers = new NumericUpDown
        {
            Location = new Point(cardPlayers.Width - CardPanel.InnerPadding - 160, CardPanel.HeaderHeight + 18),
            Size = new Size(160, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            Font = Theme.BigFont,
            TextAlign = HorizontalAlignment.Center,
            BackColor = Theme.InputBackground,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };

        _trkPlayers.ValueChanged += (_, _) => OnPlayerCountChanged(_trkPlayers.Value);
        _numPlayers.ValueChanged += (_, _) => OnPlayerCountChanged((int)_numPlayers.Value);
        cardPlayers.Controls.AddRange([_trkPlayers, _numPlayers]);
        Controls.Add(cardPlayers);

        // ──────────────────────────────────────────────────────────────
        // QUICK CHEATS card  y=283 h=185
        // ──────────────────────────────────────────────────────────────
        var cardCheats = new CardPanel
        {
            CardTitle = "快捷键修改器   (全局快捷键 → 自动输入控制台命令)",
            Location = new Point(14, 283),
            Size = new Size(ClientSize.Width - 28, 185),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _btnHotkeyToggle = ModernButton.Secondary("启用快捷键");
        _btnHotkeyToggle.Size = new Size(180, 36);
        _btnHotkeyToggle.Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 8);
        _btnHotkeyToggle.Click += (_, _) => ToggleHotkeys();
        tt.SetToolTip(_btnHotkeyToggle,
            "开启后, Ctrl+F1..F6 全局快捷键会自动输入对应控制台命令到游戏。\r\n" +
            "需要游戏已运行并加载了 MoreCoop + QuickCheats mod。");

        _lblHotkeyStatus = new Label
        {
            Text = "状态: 已禁用 (默认)",
            Location = new Point(CardPanel.InnerPadding + 195, CardPanel.HeaderHeight + 16),
            Size = new Size(cardCheats.Width - CardPanel.InnerPadding - 200, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = Theme.TextSecondary,
            Font = Theme.BodyFont,
        };

        // Mappings list (read-only labels). Two columns: combo (left) / desc (right).
        int mapY = CardPanel.HeaderHeight + 56;
        for (int i = 0; i < CheatMappings.Length; i++)
        {
            var (mods, key, cmd, desc) = CheatMappings[i];
            var combo = HotkeyManager.FormatCombo(mods, key);
            var row = new Label
            {
                Text = $"{combo,-10}  →   {cmd,-22}   {desc}",
                Location = new Point(CardPanel.InnerPadding + 4, mapY + i * 18),
                Size = new Size(cardCheats.Width - 2 * CardPanel.InnerPadding - 4, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Theme.TextSecondary,
                Font = Theme.MonoFont,
                AutoEllipsis = true,
            };
            cardCheats.Controls.Add(row);
        }

        cardCheats.Controls.Add(_btnHotkeyToggle);
        cardCheats.Controls.Add(_lblHotkeyStatus);
        Controls.Add(cardCheats);

        // ──────────────────────────────────────────────────────────────
        // BUTTON row  y=480 h=50
        // ──────────────────────────────────────────────────────────────
        var btnPanel = new Panel
        {
            Location = new Point(14, 480),
            Size = new Size(ClientSize.Width - 28, 50),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Background,
        };

        const int btnW = 130, btnH = 44, btnGap = 10;
        _btnInstall   = ModernButton.Success("一键安装 / 更新");
        _btnUninstall = ModernButton.Secondary("卸载");
        _btnLaunch    = ModernButton.Accent("启动游戏");
        _btnCheats    = ModernButton.Secondary("修改器 / 作弊");
        _btnFolder    = ModernButton.Secondary("Mod 目录");
        _btnAbout     = ModernButton.Secondary("关于 / 更新");
        _aboutBaseColor = _btnAbout.BackColor;

        var buttons = new ModernButton[] { _btnInstall, _btnUninstall, _btnLaunch, _btnCheats, _btnFolder, _btnAbout };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Size = new Size(btnW, btnH);
            buttons[i].Location = new Point(i * (btnW + btnGap), 3);
            btnPanel.Controls.Add(buttons[i]);
        }

        _btnInstall.Click   += async (_, _) => await InstallAsync();
        _btnUninstall.Click += async (_, _) => await UninstallAsync();
        _btnLaunch.Click    += (_, _) => LaunchGame();
        _btnCheats.Click    += (_, _) => ShowCheats();
        _btnFolder.Click    += (_, _) => OpenModsFolder();
        _btnAbout.Click     += (_, _) => ShowAbout();

        tt.SetToolTip(_btnInstall,   "把 UE4SS (如果没装), MoreCoop mod 和 QuickCheats 装到游戏目录");
        tt.SetToolTip(_btnUninstall, "卸载 MoreCoop + QuickCheats (可选一起卸 UE4SS)");
        tt.SetToolTip(_btnLaunch,    $"通过 Steam 启动深海迷航 2 (steam://rungameid/{SubnauticaSteamAppId})");
        tt.SetToolTip(_btnCheats,    "游戏控制台命令清单 + 社区修改器推荐");
        tt.SetToolTip(_btnFolder,    "在文件资源管理器里打开 ue4ss\\Mods 目录");
        tt.SetToolTip(_btnAbout,     "版本信息、检查 GitHub 最新版、更新 mod 脚本、日志文件位置");

        Controls.Add(btnPanel);

        // ──────────────────────────────────────────────────────────────
        // LOG card  y=542 h=remaining
        // ──────────────────────────────────────────────────────────────
        var cardLog = new CardPanel
        {
            CardTitle = "日志   (同时写到 %APPDATA%\\MoreCoop\\manager.log)",
            Location = new Point(14, 542),
            Size = new Size(ClientSize.Width - 28, ClientSize.Height - 542 - 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };

        _txtLog = new TextBox
        {
            Location = new Point(CardPanel.InnerPadding, CardPanel.HeaderHeight + 6),
            Size = new Size(cardLog.Width - 2 * CardPanel.InnerPadding,
                            cardLog.Height - CardPanel.HeaderHeight - 6 - CardPanel.InnerPadding),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Theme.InputBackground,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = Theme.MonoFont,
        };
        cardLog.Controls.Add(_txtLog);
        Controls.Add(cardLog);

        // ──────────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────────
        _hotkeys = new HotkeyManager(Handle); // Handle is created lazily; will be re-fetched as needed
        _processCheckTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _processCheckTimer.Tick += (_, _) => UpdateGameRunningState();

        HandleCreated += (_, _) => Theme.ApplyDarkTitleBar(Handle);

        Load += async (_, _) =>
        {
            FileLog.Init();
            Log($"MoreCoop Manager v{UpdateChecker.CurrentVersion} 启动");
            Log($"日志文件: {FileLog.LogPath}");
            RefreshAll();
            _processCheckTimer.Start();

            // Restore hotkey enabled state from registry
            if (LoadHotkeysEnabledPref())
            {
                Log("从设置恢复: 快捷键修改器之前是启用状态, 重新注册");
                TryEnableHotkeys(silent: true);
            }
            UpdateHotkeyButton();

            // Background update check — never blocks UI
            await CheckForUpdatesAsync(silent: true);
        };

        FormClosing += (_, _) =>
        {
            _processCheckTimer.Stop();
            _hotkeys.Dispose();
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Hotkey window message routing
    // ────────────────────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (_hotkeys?.TryHandle(ref m) == true) return;
        base.WndProc(ref m);
    }

    // ────────────────────────────────────────────────────────────────
    // QuickCheats hotkey toggle
    // ────────────────────────────────────────────────────────────────
    private void ToggleHotkeys()
    {
        if (_hotkeysEnabled)
        {
            _hotkeys.UnregisterAll();
            _hotkeysEnabled = false;
            Log("快捷键修改器已禁用, 全局快捷键已释放");
        }
        else
        {
            TryEnableHotkeys(silent: false);
        }
        SaveHotkeysEnabledPref(_hotkeysEnabled);
        UpdateHotkeyButton();
    }

    private void TryEnableHotkeys(bool silent)
    {
        try
        {
            foreach (var (mods, key, cmd, _) in CheatMappings)
            {
                _hotkeys.Register(mods, key, () =>
                {
                    CheatCommandSender.Send(cmd);
                    Log($"⚡ 按下 {HotkeyManager.FormatCombo(mods, key)} → 发送 \"{cmd}\"");
                });
            }
            _hotkeysEnabled = true;
            if (!silent)
                Log("✓ 快捷键修改器已启用: Ctrl+F1..F6 在游戏中按下即触发对应作弊命令");
        }
        catch (Exception ex)
        {
            _hotkeys.UnregisterAll();
            _hotkeysEnabled = false;
            Log($"✗ 启用失败: {ex.Message}");
            MessageBox.Show(this,
                $"无法注册全局快捷键:\r\n{ex.Message}\r\n\r\n" +
                "通常是因为其他程序 (录屏/翻译/语音助手 等) 已经占用了 Ctrl+F1..F6 中的某个组合。\r\n" +
                "请关掉那些程序再试。",
                "快捷键冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateHotkeyButton()
    {
        if (_hotkeysEnabled)
        {
            _btnHotkeyToggle.Text = "已启用 (点击关闭)";
            _btnHotkeyToggle.BackColor = Theme.SuccessBadge;
            _btnHotkeyToggle.ForeColor = Color.White;
            _lblHotkeyStatus.Text = "状态: ✓ 已启用 — 在游戏里按 Ctrl+F1..F6 触发对应命令";
            _lblHotkeyStatus.ForeColor = Theme.StatusGood;
        }
        else
        {
            _btnHotkeyToggle.Text = "启用快捷键";
            _btnHotkeyToggle.BackColor = Theme.ButtonBackground;
            _btnHotkeyToggle.ForeColor = Theme.TextPrimary;
            _lblHotkeyStatus.Text = "状态: 已禁用 (默认; 启用后会全局占用 Ctrl+F1..F6)";
            _lblHotkeyStatus.ForeColor = Theme.TextSecondary;
        }
    }

    private static bool LoadHotkeysEnabledPref()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(HotkeyRegKey);
            var v = k?.GetValue(HotkeyRegValue) as int?;
            return v == 1;
        }
        catch { return false; }
    }

    private static void SaveHotkeysEnabledPref(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(HotkeyRegKey);
            k?.SetValue(HotkeyRegValue, enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { /* ignore */ }
    }

    // ────────────────────────────────────────────────────────────────
    // Update check
    // ────────────────────────────────────────────────────────────────
    private async Task CheckForUpdatesAsync(bool silent)
    {
        var info = await UpdateChecker.CheckAsync();
        _availableUpdate = info;

        if (info is null)
        {
            if (!silent) Log("✗ 无法连接 GitHub 检查更新 (网络问题或被墙)");
            return;
        }

        if (info.IsNewer)
        {
            Log($"✨ 有新版本可用: v{info.LatestVersion} (当前 v{UpdateChecker.CurrentVersion})");
            Log($"   点 [关于 / 更新] 按钮可以下载新版或仅更新 mod 脚本");
            _btnAbout.Text = $"● 有新版本";
            _btnAbout.BackColor = Color.FromArgb(255, 152, 0);
            _btnAbout.ForeColor = Color.White;
        }
        else
        {
            if (!silent) Log($"✓ 已是最新版 (v{UpdateChecker.CurrentVersion})");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // State refresh
    // ────────────────────────────────────────────────────────────────
    private void RefreshAll()
    {
        var gamePath = SteamFinder.FindGamePath();
        if (gamePath is null)
        {
            _installer = null;
            SetRow(_rowGame, "未找到 — 请点右边 [浏览...] 手动选游戏目录", Theme.StatusBad);
            SetRow(_rowUE4SS, "— (先选游戏目录)", Theme.StatusNeutral);
            SetRow(_rowMod,   "— (先选游戏目录)", Theme.StatusNeutral);
            _btnInstall.Enabled = false;
            _btnUninstall.Enabled = false;
            _btnFolder.Enabled = false;
            UpdateGameRunningState();
            return;
        }

        _installer = new ModInstaller(gamePath);
        var sourceTag = SteamFinder.LoadSavedPath() == gamePath ? "手选" : "自动";
        SetRow(_rowGame, $"[{sourceTag}] {gamePath}", Theme.StatusGood);

        if (_installer.UE4SSInstalled)
        {
            var origin = _installer.UE4SSInstalledByUs ? "本程序装的" : "你自己装的";
            SetRow(_rowUE4SS, $"已安装 ({origin})", Theme.StatusGood);
        }
        else
        {
            SetRow(_rowUE4SS, "未安装 — 点 [一键安装] 时会自动装上 (内嵌, 不联网)", Theme.StatusWarn);
        }

        if (_installer.ModInstalled)
        {
            var current = _installer.ReadCurrentMaxPlayers();
            var qc = _installer.QuickCheatsInstalled ? " + QuickCheats" : "";
            SetRow(_rowMod, $"已安装 (当前 {current} 人){qc}", Theme.StatusGood);
            _suppressSliderEvents = true;
            _trkPlayers.Value = Math.Clamp(current, 4, 64);
            _numPlayers.Value = Math.Clamp(current, 4, 64);
            _suppressSliderEvents = false;
        }
        else
        {
            SetRow(_rowMod, "未安装", Theme.StatusBad);
        }

        UpdateGameRunningState();
    }

    private void UpdateGameRunningState()
    {
        var running = IsGameRunning();

        if (running)
        {
            _btnInstall.Enabled = false;
            _btnUninstall.Enabled = false;
            _btnLaunch.Enabled = false;
            _btnLaunch.Text = "游戏运行中";
        }
        else
        {
            _btnInstall.Enabled = _installer is not null;
            _btnUninstall.Enabled = _installer?.ModInstalled == true;
            _btnLaunch.Enabled = true;
            _btnLaunch.Text = "启动游戏";
        }

        _btnFolder.Enabled = _installer?.UE4SSInstalled == true;
    }

    private static bool IsGameRunning()
    {
        try { return Process.GetProcessesByName(GameProcessName).Length > 0; }
        catch { return false; }
    }

    // ────────────────────────────────────────────────────────────────
    // Manual game folder selection
    // ────────────────────────────────────────────────────────────────
    private void OnBrowseGame()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "选择深海迷航 2 的游戏根目录\r\n(包含 Subnautica2 子目录的那一层, 通常叫 'Subnautica 2')",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            SelectedPath = _installer?.GamePath
                ?? SteamFinder.LoadSavedPath()
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var picked = dlg.SelectedPath;
        if (!SteamFinder.IsValidGamePath(picked))
        {
            var deeper = Path.Combine(picked, "Subnautica 2");
            if (SteamFinder.IsValidGamePath(deeper))
                picked = deeper;
            else
            {
                MessageBox.Show(this,
                    $"这不像深海迷航 2 的根目录:\r\n{picked}\r\n\r\n" +
                    "正确的目录里应该有一个名叫 [Subnautica2] 的子文件夹 (注意没有空格)。\r\n" +
                    "通常路径长这样: ...\\steamapps\\common\\Subnautica 2",
                    "选错了", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        SteamFinder.SaveUserPath(picked);
        Log($"已手动设置游戏目录: {picked}");
        RefreshAll();
    }

    // ────────────────────────────────────────────────────────────────
    // Launch game via Steam protocol
    // ────────────────────────────────────────────────────────────────
    private void LaunchGame()
    {
        if (IsGameRunning())
        {
            Log("游戏已经在运行了");
            return;
        }
        try
        {
            Log($"通过 Steam 启动游戏: steam://rungameid/{SubnauticaSteamAppId}");
            Process.Start(new ProcessStartInfo($"steam://rungameid/{SubnauticaSteamAppId}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"启动失败: {ex.Message}");
            MessageBox.Show(this,
                $"无法通过 Steam 启动游戏:\r\n{ex.Message}\r\n\r\n请确认 Steam 已运行, 或者手动启动深海迷航 2。",
                "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Slider live save
    // ────────────────────────────────────────────────────────────────
    private void OnPlayerCountChanged(int value)
    {
        if (_suppressSliderEvents) return;

        _suppressSliderEvents = true;
        if (_trkPlayers.Value != value) _trkPlayers.Value = value;
        if ((int)_numPlayers.Value != value) _numPlayers.Value = value;
        _suppressSliderEvents = false;

        if (_installer?.ModInstalled == true)
        {
            try
            {
                _installer.UpdateMaxPlayers(value);
                Log($"人数已改为 {value} (UE4SS 热生效, 游戏中下次创建房间即用此值)");
                var qc = _installer.QuickCheatsInstalled ? " + QuickCheats" : "";
                SetRow(_rowMod, $"已安装 (当前 {value} 人){qc}", Theme.StatusGood);
            }
            catch (Exception ex)
            {
                Log($"修改失败: {ex.Message}");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Install / Uninstall
    // ────────────────────────────────────────────────────────────────
    private async Task InstallAsync()
    {
        if (_installer is null) return;
        if (RefuseIfGameRunning("安装")) return;

        _btnInstall.Enabled = false;
        _btnUninstall.Enabled = false;
        var target = (int)_numPlayers.Value;
        Log($"开始安装, 目标人数上限 = {target}");

        try
        {
            await Task.Run(() => _installer.Install(target, msg => Log(msg)));
            Log("✓ 全部完成! 启动游戏后, 按 Insert 打开 UE4SS 控制台应看到 [MoreCoop] + [QuickCheats] 加载日志");
            RefreshAll();
        }
        catch (Exception ex)
        {
            Log($"✗ 安装失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshAll();
        }
    }

    private async Task UninstallAsync()
    {
        if (_installer is null || !_installer.ModInstalled) return;
        if (RefuseIfGameRunning("卸载")) return;

        var alsoRemoveUE4SS = false;

        if (_installer.UE4SSInstalledByUs)
        {
            var result = MessageBox.Show(this,
                "确认卸载 MoreCoop + QuickCheats?\r\n\r\n" +
                "UE4SS 是本程序之前安装的。是否一起卸掉?\r\n\r\n" +
                "[是] 一起卸 (MoreCoop + QuickCheats + UE4SS, 游戏完全恢复原版)\r\n" +
                "[否] 只卸 MoreCoop + QuickCheats (保留 UE4SS, 以后装其他 UE4SS mod 用)\r\n" +
                "[取消] 不卸",
                "确认卸载", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return;
            alsoRemoveUE4SS = (result == DialogResult.Yes);
        }
        else
        {
            var result = MessageBox.Show(this,
                "确认卸载 MoreCoop + QuickCheats?\r\n\r\n本操作会删除两个 mod 文件, 游戏将恢复 4 人上限。",
                "确认卸载", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
        }

        _btnInstall.Enabled = false;
        _btnUninstall.Enabled = false;
        Log("开始卸载...");

        try
        {
            await Task.Run(() => _installer.Uninstall(alsoRemoveUE4SS, msg => Log(msg)));
            Log(alsoRemoveUE4SS
                ? "✓ 已卸载, 全部组件移除, 游戏完全恢复原版"
                : "✓ MoreCoop + QuickCheats 已卸载, 游戏恢复 4 人原版 (UE4SS 保留)");
            RefreshAll();
        }
        catch (Exception ex)
        {
            Log($"✗ 卸载失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshAll();
        }
    }

    private bool RefuseIfGameRunning(string action)
    {
        if (!IsGameRunning()) return false;
        MessageBox.Show(this,
            $"深海迷航 2 正在运行, 无法{action}。\r\n\r\n请先退出游戏 (在主菜单按 Alt+F4 或退到桌面), 再操作。\r\n\r\n" +
            $"这是为了防止文件被锁住导致{action}失败。",
            "游戏运行中", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private void OpenModsFolder()
    {
        if (_installer is null) return;
        var target = Directory.Exists(_installer.ModPath)
            ? _installer.ModPath
            : Path.Combine(_installer.UE4SSPath, "Mods");
        if (Directory.Exists(target))
            Process.Start("explorer.exe", target);
        else
            Log("Mods 目录不存在 (UE4SS 可能未安装)");
    }

    // ────────────────────────────────────────────────────────────────
    // Cheats info dialog (separate from QuickCheats hotkey feature)
    // ────────────────────────────────────────────────────────────────
    private void ShowCheats()
    {
        const string CheatTogglesUrl = "https://www.nexusmods.com/subnautica2/mods/64";
        const string FlingUrl        = "https://flingtrainer.com/trainer/subnautica-2-trainer/";

        var msg = """
                  修改器 / 作弊命令

                  本工具已经把"快捷键修改器"内置在主界面上 (默认 OFF, 点开关启用)。
                  启用后, Ctrl+F1..F6 直接触发常用作弊命令, 不用打开控制台。

                  ─── 手动控制台 (F2) 命令清单 ───

                  god                 无敌 (无伤害 + 无饥渴 + 无氧气消耗)
                  nocost              免材料合成
                  attr oxygen 9999    氧气满
                  attr food 100       饱食锁定
                  attr water 100      口渴锁定
                  unlockall           解锁所有蓝图
                  item <name> <qty>   给自己物品
                  freecam             第三人称自由相机
                  fastbuild           即时建造

                  ─── 想要更全的可视化修改器? ───

                  ▸ Cheat Toggles (Nexus mod #64) - 设置菜单里开关切换
                  ▸ FLiNG Trainer - 124+ 选项独立外挂, 免费

                  [是]   打开 Cheat Toggles (Nexus)
                  [否]   打开 FLiNG Trainer 下载页
                  [取消] 关闭对话框
                  """;

        var result = MessageBox.Show(this, msg, "修改器 / 作弊命令",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);

        switch (result)
        {
            case DialogResult.Yes:    OpenUrl(CheatTogglesUrl); break;
            case DialogResult.No:     OpenUrl(FlingUrl);        break;
            default: /* Cancel */     break;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // About dialog (with optional update section)
    // ────────────────────────────────────────────────────────────────
    private void ShowAbout()
    {
        if (_availableUpdate is null)
        {
            Log("正在检查 GitHub 上的最新版...");
            _ = Task.Run(async () =>
            {
                await CheckForUpdatesAsync(silent: false);
                BeginInvoke(() => ShowAbout());
            });
            return;
        }

        var info = _availableUpdate;
        var savedPath = SteamFinder.LoadSavedPath();
        var savedNote = savedPath is null ? "" : $"\r\n\r\n手选的游戏目录: {savedPath}";

        if (info.IsNewer)
        {
            var updateSection = $"\r\n─── ✨ 有新版本 ───\r\n" +
                                $"GitHub 上最新: v{info.LatestVersion}  (本程序: v{UpdateChecker.CurrentVersion})\r\n\r\n" +
                                $"[是] 下载新版管理器 (打开 GitHub 发布页)\r\n" +
                                $"[否] 仅更新 mod 脚本 (热更新 main.lua, 不替换管理器)\r\n" +
                                $"[取消] 关闭";

            var result = MessageBox.Show(this,
                BuildAboutBody(savedNote) + updateSection,
                "关于 MoreCoop Manager",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);

            switch (result)
            {
                case DialogResult.Yes: OpenUrl(info.ReleaseUrl); break;
                case DialogResult.No:  _ = UpdateModScriptAsync(info); break;
            }
        }
        else
        {
            var updateSection = $"\r\n─── 检查更新 ───\r\n" +
                                $"已是最新版 (v{UpdateChecker.CurrentVersion}, GitHub 上也是 v{info.LatestVersion}){savedNote}";
            updateSection += savedPath is null
                ? "\r\n\r\n[是] 打开 GitHub 仓库\r\n[否] 关闭"
                : "\r\n\r\n[是] 打开 GitHub 仓库\r\n[否] 清除手选路径, 改回自动检测";

            var result = MessageBox.Show(this,
                BuildAboutBody("") + updateSection,
                "关于 MoreCoop Manager",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                OpenUrl(UpdateChecker.LatestReleasePageUrl);
            }
            else if (savedPath is not null)
            {
                SteamFinder.ClearSavedPath();
                Log("已清除手选游戏路径, 将重新自动检测");
                RefreshAll();
            }
        }
    }

    private static string BuildAboutBody(string savedNote) => $"""
        MoreCoop Manager v{UpdateChecker.CurrentVersion}

        深海迷航 2 多人人数解锁补丁 (一键安装)

        ▸ 自动检测 + 手选游戏目录
        ▸ 内嵌 UE4SS, 全程不联网装游戏组件
        ▸ 人数 4–64 拖滑块热生效
        ▸ 快捷键修改器 (Ctrl+F1..F6, 用户可开关)
        ▸ 检测游戏运行, 防文件锁错误
        ▸ Steam 一键启动游戏
        ▸ 应用内自动检查更新

        ─── 许可与归属 ───
        本程序: GPL-3.0 (wuha-like-sleep)
        补丁原理: Zeusfail/Too-Many-Divers v1.2.0 (GPL-3.0)
        UE4SS: MIT (Narknon, Subnautica2Modding)

        日志: {FileLog.LogPath}{savedNote}
        """;

    private async Task UpdateModScriptAsync(UpdateChecker.UpdateInfo info)
    {
        if (_installer is null || !_installer.ModInstalled)
        {
            MessageBox.Show(this,
                "MoreCoop mod 还没装在游戏目录, 没有可更新的文件。\r\n请先 [一键安装]。",
                "无法更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (RefuseIfGameRunning("更新")) return;

        Log($"正在从 GitHub 下载 v{info.LatestVersion} 的 main.lua...");
        var targetPath = Path.Combine(_installer.ModPath, "Scripts", "main.lua");
        var (ok, error) = await UpdateChecker.DownloadAndReplaceAsync(info.MainLuaRawUrl, targetPath);

        if (ok)
        {
            Log($"✓ main.lua 已更新到 v{info.LatestVersion}");
            Log($"  注: UE4SS 会在游戏中下次加载时自动用新脚本");
        }
        else
        {
            Log($"✗ 下载失败: {error}");
            MessageBox.Show(this,
                $"下载或写入 main.lua 失败:\r\n{error}\r\n\r\n" +
                "请直接到 GitHub 发布页下载完整新版管理器。",
                "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────
    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        if (_txtLog.InvokeRequired) _txtLog.Invoke(() => _txtLog.AppendText(line));
        else _txtLog.AppendText(line);
        FileLog.Append(message);
    }

    private static void SetRow(StatusRow row, string value, Color dotColor)
    {
        row.Value = value;
        row.DotColor = dotColor;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore — user can copy from About dialog */ }
    }
}
