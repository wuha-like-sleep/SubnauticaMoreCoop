using System.Diagnostics;

namespace MoreCoopManager;

/// <summary>
/// Single-window GUI. The form auto-detects state on launch and after
/// every install/uninstall, and pushes MaxPlayers slider changes to
/// settings.json live (UE4SS hot-reloads the value, so no restart needed).
/// </summary>
internal sealed class MainForm : Form
{
    private const string Title = "MoreCoop Manager - 深海迷航 2 多人解锁 v1.1.0";
    private const string GithubUrl = "https://github.com/wuha-like-sleep/SubnauticaMoreCoop";
    private const string UE4SSUrl = "https://www.nexusmods.com/subnautica2/mods/36";

    private readonly Label _lblGame, _lblUE4SS, _lblMod;
    private readonly TrackBar _trkPlayers;
    private readonly NumericUpDown _numPlayers;
    private readonly Button _btnInstall, _btnUninstall, _btnFolder, _btnAbout;
    private readonly TextBox _txtLog;

    private ModInstaller? _installer;
    private bool _suppressSliderEvents;

    public MainForm()
    {
        Text = Title;
        ClientSize = new Size(560, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        // --- Status group ---
        var grpStatus = new GroupBox { Text = "状态", Location = new Point(12, 8), Size = new Size(536, 110) };
        _lblGame = MakeStatusLabel(15, 28);
        _lblUE4SS = MakeStatusLabel(15, 53);
        _lblMod = MakeStatusLabel(15, 78);
        grpStatus.Controls.AddRange([_lblGame, _lblUE4SS, _lblMod]);
        Controls.Add(grpStatus);

        // --- Player count group ---
        var grpPlayers = new GroupBox
        {
            Text = "人数上限  (拖动滑块即可立即生效, 无需重启游戏)",
            Location = new Point(12, 126),
            Size = new Size(536, 90),
        };
        _trkPlayers = new TrackBar
        {
            Location = new Point(15, 28),
            Size = new Size(390, 45),
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            TickFrequency = 4,
        };
        _numPlayers = new NumericUpDown
        {
            Location = new Point(420, 35),
            Size = new Size(100, 30),
            Minimum = 4,
            Maximum = 64,
            Value = 8,
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
        };
        _trkPlayers.ValueChanged += (_, _) => OnPlayerCountChanged(_trkPlayers.Value);
        _numPlayers.ValueChanged += (_, _) => OnPlayerCountChanged((int)_numPlayers.Value);
        grpPlayers.Controls.AddRange([_trkPlayers, _numPlayers]);
        Controls.Add(grpPlayers);

        // --- Action buttons ---
        _btnInstall = MakeButton("一键安装 / 更新", 12, 225, Color.FromArgb(76, 175, 80), Color.White);
        _btnInstall.Click += async (_, _) => await InstallAsync();
        _btnUninstall = MakeButton("卸载", 144, 225);
        _btnUninstall.Click += async (_, _) => await UninstallAsync();
        _btnFolder = MakeButton("打开 Mod 目录", 276, 225);
        _btnFolder.Click += (_, _) => OpenModsFolder();
        _btnAbout = MakeButton("关于", 408, 225);
        _btnAbout.Click += (_, _) => ShowAbout();
        Controls.AddRange([_btnInstall, _btnUninstall, _btnFolder, _btnAbout]);

        // --- Log box ---
        var grpLog = new GroupBox { Text = "日志", Location = new Point(12, 275), Size = new Size(536, 170) };
        _txtLog = new TextBox
        {
            Location = new Point(10, 22),
            Size = new Size(516, 140),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Font = new Font("Consolas", 9F),
        };
        grpLog.Controls.Add(_txtLog);
        Controls.Add(grpLog);

        Load += (_, _) => RefreshAll();
    }

    // ----------------------------------------------------------------
    // State refresh
    // ----------------------------------------------------------------
    private void RefreshAll()
    {
        var gamePath = SteamFinder.FindGamePath();
        if (gamePath is null)
        {
            _installer = null;
            SetStatus(_lblGame, "游戏: ✗ 未找到 (请确认深海迷航 2 已通过 Steam 安装)", false);
            SetStatus(_lblUE4SS, "UE4SS: — (先要找到游戏)", null);
            SetStatus(_lblMod, "MoreCoop: — (先要找到游戏)", null);
            _btnInstall.Enabled = false;
            _btnUninstall.Enabled = false;
            _btnFolder.Enabled = false;
            return;
        }

        _installer = new ModInstaller(gamePath);
        SetStatus(_lblGame, $"游戏: ✓ {gamePath}", true);
        SetStatus(_lblUE4SS,
            _installer.UE4SSInstalled
                ? "UE4SS: ✓ 已安装"
                : "UE4SS: ✗ 未安装 (本 mod 依赖 UE4SS, 点 [一键安装] 会提示下载)",
            _installer.UE4SSInstalled);

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

        _btnInstall.Enabled = true;
        _btnUninstall.Enabled = _installer.ModInstalled;
        _btnFolder.Enabled = _installer.UE4SSInstalled;
    }

    // ----------------------------------------------------------------
    // Slider live save
    // ----------------------------------------------------------------
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

    // ----------------------------------------------------------------
    // Install / Uninstall
    // ----------------------------------------------------------------
    private async Task InstallAsync()
    {
        if (_installer is null) return;

        if (!_installer.UE4SSInstalled)
        {
            var result = MessageBox.Show(this,
                "UE4SS 未安装。本 mod 依赖 UE4SS 才能运行。\r\n\r\n点击 [是] 打开 Nexus 下载页面 (需要 Nexus 账号, 免费)。\r\n下载装好 UE4SS 后再回来点 [一键安装]。",
                "需要先装 UE4SS", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) OpenUrl(UE4SSUrl);
            return;
        }

        _btnInstall.Enabled = false;
        var target = (int)_numPlayers.Value;
        Log($"正在安装 MoreCoop (人数上限 {target})...");

        try
        {
            await Task.Run(() => _installer.Install(target));
            Log("✓ 安装成功! 启动游戏后, 按 Insert 打开 UE4SS 控制台应看到 [MoreCoop] 加载日志");
            RefreshAll();
        }
        catch (Exception ex)
        {
            Log($"✗ 安装失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _btnInstall.Enabled = true;
        }
    }

    private async Task UninstallAsync()
    {
        if (_installer is null || !_installer.ModInstalled) return;

        var confirm = MessageBox.Show(this,
            "确认卸载 MoreCoop?\r\n\r\n本操作只删除 mod 文件, 不影响 UE4SS 本身和游戏文件。\r\n卸载后游戏将恢复 4 人上限。",
            "确认卸载", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _btnUninstall.Enabled = false;
        Log("正在卸载...");
        try
        {
            await Task.Run(() => _installer.Uninstall());
            Log("✓ 已卸载, 游戏恢复原版状态");
            RefreshAll();
        }
        catch (Exception ex)
        {
            Log($"✗ 卸载失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _btnUninstall.Enabled = true;
        }
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
        var msg = """
                  MoreCoop Manager v1.1.0

                  深海迷航 2 多人人数解锁补丁
                  把官方 4 人上限改成可调 (4–64 人)

                  许可: GPL-3.0
                  派生自: Zeusfail/Too-Many-Divers v1.2.0

                  - 只有房主需要装本 mod, 朋友用原版即可加入
                  - 改人数后无需重启, UE4SS 会热生效
                  - 完全可逆, 不修改任何游戏原文件

                  点 [是] 打开 GitHub 仓库页面
                  """;
        var result = MessageBox.Show(this, msg, "关于 MoreCoop Manager",
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (result == DialogResult.Yes) OpenUrl(GithubUrl);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------
    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        if (_txtLog.InvokeRequired) _txtLog.Invoke(() => _txtLog.AppendText(line));
        else _txtLog.AppendText(line);
    }

    private static Label MakeStatusLabel(int x, int y) => new()
    {
        Location = new Point(x, y),
        Size = new Size(510, 22),
        Text = "(检测中...)",
        AutoEllipsis = true,
    };

    private static Button MakeButton(string text, int x, int y, Color? bg = null, Color? fg = null)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(128, 40),
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
