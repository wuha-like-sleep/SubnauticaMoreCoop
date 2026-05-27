-- QuickCheats — companion mod to MoreCoop
-- Watches %APPDATA%\MoreCoop\cheats.cmd for a one-line console command and
-- executes it via PlayerController:ConsoleCommand on the game thread.
--
-- MoreCoopManager.exe writes to that file when the user presses one of the
-- registered global hotkeys (default Ctrl+F1..F6). The Lua side is 100%
-- passive: if the user never enables hotkeys, the file stays empty and
-- nothing ever runs.
--
-- Copyright (C) 2026 wuha-like-sleep, GPL-3.0

local TAG = "[QuickCheats]"
local POLL_MS = 250

local CMD_FILE = (os.getenv("APPDATA") or "C:\\Users\\Default\\AppData\\Roaming")
                 .. [[\MoreCoop\cheats.cmd]]

local function log(fmt, ...) print(string.format(TAG .. " " .. fmt .. "\n", ...)) end

log("loaded, polling %s every %dms", CMD_FILE, POLL_MS)

local function trim(s)
    return (s or ""):gsub("^%s+", ""):gsub("%s+$", "")
end

local function read_and_clear()
    local f = io.open(CMD_FILE, "r")
    if not f then return nil end
    local content = f:read("*a")
    f:close()

    local cmd = trim(content)
    if cmd == "" then return nil end

    -- Atomically clear (write empty file) so we don't re-execute on next poll
    local fw = io.open(CMD_FILE, "w")
    if fw then fw:close() end

    return cmd
end

local function safe_pcall_log(name, fn)
    local ok, err = pcall(fn)
    if not ok then log("%s error: %s", name, tostring(err)) end
end

LoopAsync(POLL_MS, function()
    safe_pcall_log("poll", function()
        local cmd = read_and_clear()
        if not cmd then return end

        ExecuteInGameThread(function()
            -- Find any player controller. SN2 uses a subclass but generic works.
            local pc = FindFirstOf("PlayerController")
            local ok, valid = pcall(function() return pc and pc:IsValid() end)
            if not (ok and valid) then
                log("no valid PlayerController, skipping: %s", cmd)
                return
            end

            local ok2, err = pcall(function() pc:ConsoleCommand(cmd, false) end)
            if ok2 then
                log("executed: %s", cmd)
            else
                log("ConsoleCommand failed for '%s': %s", cmd, tostring(err))
            end
        end)
    end)
end)
