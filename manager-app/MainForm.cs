using System.Diagnostics;

namespace MoreCoopManager;

/// <summary>
/// Single-window GUI for installing MoreCoop.
///
/// v1.4 bundled UE4SS, v1.5 added game-running guard / launch button / file log,
/// v1.6 enlarges the window from 560×470 to 880×600 with bigger fonts so it
/// stops feeling cramped on modern displays.
/// </summary>
internal sealed class MainForm : Form
{
    private const string Title = "MoreCoop Manager - 深海迷航 2 多人解锁 v1.6.0";
    private const string GithubUrl = "https://github.com/wuha-like-sleep/SubnauticaMoreCoop";
    private const string LatestReleaseUrl = "https://github.com/wuha-like-sleep/SubnauticaMoreCoop/releases/latest";
    private const int SubnauticaSteamAppId = 1962700;
    private const string GameProcessName = "Subnautica2-Win64-Shipping";

    private readonly Label _lblGame, _lblUE4SS, _lblMod;
    private readonly Button _btnBrowseGame;
    private readonly TrackBar _trkPlayers;
    private readonly NumericUpDown _numPlayers;
    private readonly Button _btnInstall, _btnUninstall, _btnLaunch, _btnFolder, _btnAbout;
    private readonly TextBox _txtLog;
    private readonly System.Windows.Forms.Timer _processCheckTimer;

    private ModInstaller? _installer;
    private bool _suppressSliderEvents;

    public MainForm()
    {
        Text = Title;
        ClientSize = new Size(880, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 10F);

        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { /* fall back to default */ }

        var tt = new ToolTip();

        // ──────────────────────────────────────────────────────────────
        // Status group  — (12, 12) → 856×140
        // ──────────────────────────────────────────────────────────────
        var grpStatus = new GroupBox
        {
            Text = "状态",
            Location = new Point(12, 12),
            Size = new Size(856, 140),
        };

        _lblGame   = MakeStatusLabel(18, 32, width: 620);
        _btnBrowseGame = new Button
        {
            Text = "浏览...",
            Location = new Point(670, 30),
            Size = new Size(170, 32),
            FlatStyle = FlatStyle.System,
        };
        _btnBrowseGame.Click += (_, _) => OnBrowseGame();
        tt.SetToolTip(_btnBrowseGame, "如果自动检测没找到游戏 (或找错了), 点这里手动选游戏根目录");

        _lblUE4SS = MakeStatusLabel(18, 70, width: 822);
        _lblMod   = MakeStatusLabel(18, 102, width: 822);

        grpStatus.Controls.AddRange([_lblGame, _btnBrowseGame, _lblUE4SS, _lblMod]);
        Controls.Add(grpStatus);

        // ──────────────────────────────────────────────────────────────
        // Player count group  — (12, 160) → 856×115
        // ──────────────────────────────────────────────────────────────
        var grpPlayers = new GroupBox
        {
            Text = "人数上限  (拖动滑块即可立即生效, 无需重启游戏)",
            Location = new Point(12, 160),
            Size = new Size(856, 115),
        };
        _trkPlayers = new TrackBar
        {
            Location = new Point(18, 32),
            Size = new Size(640, 50),
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            TickFrequency = 4,
            LargeChange = 4,
        };
        _numPlayers = new NumericUpDown
        {
            Location = new Point(680, 42),
            Size = new Size(160, 36),
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
        };
        _trkPlayers.ValueChanged += (_, _) => OnPlayerCountChanged(_trkPlayers.Value);
        _numPlayers.ValueChanged += (_, _) => OnPlayerCountChanged((int)_numPlayers.Value);
        grpPlayers.Controls.AddRange([_trkPlayers, _numPlayers]);
        Controls.Add(grpPlayers);

        // ──────────────────────────────────────────────────────────────
        // Action button row  — y=290, 5 buttons of 156×44 with 12px gaps
        // ──────────────────────────────────────────────────────────────
        const int ButtonY = 290;
        const int ButtonW = 156;
        const int ButtonH = 44;
        const int ButtonGap = 12;
        const int FirstX = 12;
        int X(int idx) => FirstX + idx * (ButtonW + ButtonGap);

        _btnInstall   = MakeButton("一键安装 / 更新", X(0), ButtonY, ButtonW, ButtonH, Color.FromArgb(76, 175, 80), Color.White);
        _btnInstall.Click   += async (_, _) => await InstallAsync();
        _btnUninstall = MakeButton("卸载", X(1), ButtonY, ButtonW, ButtonH);
        _btnUninstall.Click += async (_, _) => await UninstallAsync();
        _btnLaunch    = MakeButton("启动游戏", X(2), ButtonY, ButtonW, ButtonH, Color.FromArgb(33, 150, 243), Color.White);
        _btnLaunch.Click    += (_, _) => LaunchGame();
        _btnFolder    = MakeButton("打开 Mod 目录", X(3), ButtonY, ButtonW, ButtonH);
        _btnFolder.Click    += (_, _) => OpenModsFolder();
        _btnAbout     = MakeButton("关于 / 更新", X(4), ButtonY, ButtonW, ButtonH);
        _btnAbout.Click     += (_, _) => ShowAbout();
        Controls.AddRange([_btnInstall, _btnUninstall, _btnLaunch, _btnFolder, _btnAbout]);

        tt.SetToolTip(_btnInstall,   "把 UE4SS (如果没装) 和 MoreCoop mod 装到游戏目录");
        tt.SetToolTip(_btnUninstall, "卸载 MoreCoop (可选一起卸 UE4SS)");
        tt.SetToolTip(_btnLaunch,    $"通过 Steam 启动深海迷航 2 (steam://rungameid/{SubnauticaSteamAppId})");
        tt.SetToolTip(_btnFolder,    "在文件资源管理器里打开 ue4ss\\Mods 目录");
        tt.SetToolTip(_btnAbout,     "版本信息、查看 GitHub 最新版、清除手选游戏路径、日志文件位置");

        // ──────────────────────────────────────────────────────────────
        // Log group  — (12, 348) → 856×240
        // ──────────────────────────────────────────────────────────────
        var grpLog = new GroupBox
        {
            Text = "日志  (同时写到 %APPDATA%\\MoreCoop\\manager.log)",
            Location = new Point(12, 348),
            Size = new Size(856, 240),
        };
        _txtLog = new TextBox
        {
            Location = new Point(12, 26),
            Size = new Size(832, 204),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Font = new Font("Consolas", 10F),
        };
        grpLog.Controls.Add(_txtLog);
        Controls.Add(grpLog);

        // ──────────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────────
        _processCheckTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _processCheckTimer.Tick += (_, _) => UpdateGameRunningState();

        Load += (_, _) =>
        {
            FileLog.Init();
            Log($"MoreCoop Manager v1.6.0 启动");
            Log($"日志文件: {FileLog.LogPath}");
            RefreshAll();
            _processCheckTimer.Start();
        };
        FormClosing += (_, _) => _processCheckTimer.Stop();
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
            SetStatus(_lblGame, "游戏: ✗ 未找到 — 请点右边 [浏览...] 手动选游戏目录", false);
            SetStatus(_lblUE4SS, "UE4SS: — (先选游戏目录)", null);
            SetStatus(_lblMod, "MoreCoop: — (先选游戏目录)", null);
            _btnInstall.Enabled = false;
            _btnUninstall.Enabled = false;
            _btnFolder.Enabled = false;
            UpdateGameRunningState();
            return;
        }

        _installer = new ModInstaller(gamePath);
        var sourceTag = SteamFinder.LoadSavedPath() == gamePath ? "手选" : "自动检测";
        SetStatus(_lblGame, $"游戏: ✓ [{sourceTag}] {gamePath}", true);

        if (_installer.UE4SSInstalled)
        {
            var origin = _installer.UE4SSInstalledByUs ? "本程序装的" : "你自己装的";
            SetStatus(_lblUE4SS, $"UE4SS: ✓ 已安装 ({origin})", true);
        }
        else
        {
            SetStatus(_lblUE4SS, "UE4SS: ○ 未安装 — 点 [一键安装] 时会自动装上 (内嵌, 不联网)", null);
        }

        if (_installer.ModInstalled)
        {
            var current = _installer.ReadCurrentMaxPlayers();
            SetStatus(_lblMod, $"MoreCoop: ✓ 已安装 (当前 {current} 人)", true);
            _suppressSliderEvents = true;
            _trkPlayers.Value = Math.Clamp(current, 4, 64);
            _numPlayers.Value = Math.Clamp(current, 4, 64);
            _suppressSliderEvents = false;
        }
        else
        {
            SetStatus(_lblMod, "MoreCoop: ✗ 未安装", false);
        }

        UpdateGameRunningState();
    }

    /// <summary>
    /// Polled every 3 seconds. If the game is running we lock install/uninstall
    /// to prevent file-lock errors. Launch button also reflects state.
    /// </summary>
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
            {
                picked = deeper;
            }
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
                SetStatus(_lblMod, $"MoreCoop: ✓ 已安装 (当前 {value} 人)", true);
            }
            catch (Exception ex)
            {
                Log($"修改失败: {ex.Message}");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Install / Uninstall (refuse if game is running)
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
            Log("✓ 全部完成! 启动游戏后, 按 Insert 打开 UE4SS 控制台应看到 [MoreCoop] 加载日志");
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
                "确认卸载 MoreCoop?\r\n\r\n" +
                "UE4SS 是本程序之前安装的。是否一起卸掉?\r\n\r\n" +
                "[是] 一起卸 (MoreCoop + UE4SS, 游戏完全恢复原版)\r\n" +
                "[否] 只卸 MoreCoop (保留 UE4SS, 以后装其他 UE4SS mod 用)\r\n" +
                "[取消] 不卸",
                "确认卸载", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return;
            alsoRemoveUE4SS = (result == DialogResult.Yes);
        }
        else
        {
            var result = MessageBox.Show(this,
                "确认卸载 MoreCoop?\r\n\r\n本操作会删除 mod 文件, 游戏将恢复 4 人上限。",
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
                ? "✓ 已卸载, MoreCoop + UE4SS 都已移除, 游戏完全恢复原版"
                : "✓ MoreCoop 已卸载, 游戏恢复 4 人原版 (UE4SS 保留)");
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

    private void ShowAbout()
    {
        var savedPath = SteamFinder.LoadSavedPath();
        var savedNote = savedPath is null
            ? ""
            : $"\r\n\r\n当前手选的游戏目录:\r\n{savedPath}";

        var msg = $"""
                   MoreCoop Manager v1.6.0

                   深海迷航 2 多人人数解锁补丁 (真·一键安装)

                   ▸ 自动检测游戏 + UE4SS + mod 状态
                   ▸ 内嵌 UE4SS, 全程不联网, 不用单独下载
                   ▸ 人数 4–64, 拖滑块热生效, 不用关游戏
                   ▸ 检测游戏是否运行, 防止文件锁错误
                   ▸ 一键 Steam 启动游戏验证
                   ▸ 完全可逆, 卸载干净

                   ─── 永久最新版链接 ───
                   {LatestReleaseUrl}
                   (这个 URL 永远指向 GitHub 上最新版, 保存这个分享给朋友)

                   ─── 许可与归属 ───
                   本程序: GPL-3.0  (wuha-like-sleep)
                   补丁原理: Zeusfail/Too-Many-Divers v1.2.0 (GPL-3.0)
                   UE4SS: MIT  (Narknon, Subnautica2Modding)

                   日志文件:
                   {FileLog.LogPath}{savedNote}

                   [是] 打开 GitHub 上的最新版页面 (检查更新)
                   {(savedPath is null ? "[否] 关闭" : "[否] 清除手选路径, 改回自动检测")}
                   """;
        var result = MessageBox.Show(this, msg, "关于 MoreCoop Manager",
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (result == DialogResult.Yes)
        {
            OpenUrl(LatestReleaseUrl);
        }
        else if (savedPath is not null)
        {
            SteamFinder.ClearSavedPath();
            Log("已清除手选游戏路径, 将重新自动检测");
            RefreshAll();
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

    private static Label MakeStatusLabel(int x, int y, int width = 822) => new()
    {
        Location = new Point(x, y),
        Size = new Size(width, 26),
        Text = "(检测中...)",
        AutoEllipsis = true,
    };

    private static Button MakeButton(string text, int x, int y, int width = 156, int height = 44,
                                     Color? bg = null, Color? fg = null)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.System,
        };
        if (bg.HasValue) { b.BackColor = bg.Value; b.FlatStyle = FlatStyle.Flat; }
        if (fg.HasValue) b.ForeColor = fg.Value;
        return b;
    }

    private static void SetStatus(Label label, string text, bool? good)
    {
        label.Text = text;
        label.ForeColor = good switch
        {
            true => Color.FromArgb(46, 125, 50),
            false => Color.FromArgb(198, 40, 40),
            null => SystemColors.GrayText,
        };
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore — user can copy from About dialog */ }
    }
}
