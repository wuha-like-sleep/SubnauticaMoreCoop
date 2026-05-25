-- MoreCoop - Subnautica 2 多人人数解锁补丁（精简版）
-- Copyright (C) 2026 wuha-like-sleep
-- 派生自 Zeusfail/Too-Many-Divers v1.2.0 (https://github.com/Zeusfail/Too-Many-Divers)
-- 本程序在 GNU GPL v3 许可下发布, 详见同级目录 LICENSE 文件。
-- 原理: UE4SS Lua mod, 运行时把游戏里所有跟人数上限相关的字段改大

local TAG = "[MoreCoop]"
local MIN_PLAYERS, MAX_PLAYERS = 4, 64
local DEFAULT_PLAYERS = 8

local function log(fmt, ...) print(string.format(TAG .. " " .. fmt .. "\n", ...)) end

-- 从 config/settings.json 读取人数
local function load_max_players()
    local src = debug.getinfo(1, "S").source
    local dir = src:match("^@?(.+)[/\\][^/\\]+$")
    if not dir then return DEFAULT_PLAYERS end
    local f = io.open(dir .. "\\..\\config\\settings.json", "r")
    if not f then return DEFAULT_PLAYERS end
    local content = f:read("*a"); f:close()
    local n = tonumber((content:match('"MaxPlayers"%s*:%s*(%d+)')))
    if not n or n < MIN_PLAYERS or n > MAX_PLAYERS then
        log("settings.json 里的 MaxPlayers 无效, 用默认 %d", DEFAULT_PLAYERS)
        return DEFAULT_PLAYERS
    end
    return n
end

local TARGET = load_max_players()

-- 需要打补丁的游戏类。class_name/cdo_name 是 UE5 反射路径,
-- 如果哪天游戏更新导致字段或类改名, 这里要跟着改。
local CLASSES = {
    { short = "SN2GameSession",            class = "/Script/Subnautica2.SN2GameSession",            cdo = "/Script/Subnautica2.Default__SN2GameSession",            fields = {"MaxPlayers"} },
    { short = "UWEOnlineSessionSubsystem", class = "/Script/UWESonar.UWEOnlineSessionSubsystem",    cdo = "/Script/UWESonar.Default__UWEOnlineSessionSubsystem",    fields = {"MaxSessionPlayerCount"} },
    { short = "UWEHostSessionRequest",     class = "/Script/UWESonar.UWEHostSessionRequest",        cdo = "/Script/UWESonar.Default__UWEHostSessionRequest",        fields = {"MaxPlayers", "MaxSessionPlayerCount"} },
    { short = "GameSession",               class = "/Script/Engine.GameSession",                    cdo = "/Script/Engine.Default__GameSession",                    fields = {"MaxPlayers"} },
}

local function is_valid(o)
    if not o then return false end
    local ok, v = pcall(function() return o.IsValid and o:IsValid() end)
    return ok and v == true
end

local function patch_object(obj, fields, scope)
    if not is_valid(obj) then return end
    for _, field in ipairs(fields) do
        local ok, cur = pcall(function() return obj[field] end)
        if ok and type(cur) == "number" and cur ~= TARGET then
            if pcall(function() obj[field] = TARGET end) then
                log("%s.%s: %d -> %d", scope, field, cur, TARGET)
            end
        end
    end
end

-- 给 CDO (类的默认对象) 打补丁: 影响以后所有新创建的实例
local function patch_cdo(c)
    local ok, obj = pcall(function() return StaticFindObject(c.cdo) end)
    if ok and is_valid(obj) then patch_object(obj, c.fields, "CDO " .. c.short) end
end

-- HostSessionAsync 是房主点"创建房间"时调用的, 在这里改请求里的人数最稳
local function on_host_session_pre(self_param, ...)
    local function unwrap(p)
        if not p then return nil end
        local ok, t = pcall(function() return p:type() end)
        if ok and (t == "RemoteUnrealParam" or t == "LocalUnrealParam") then
            local ok2, v = pcall(function() return p:get() end)
            if ok2 then return v end
        end
        return p
    end
    local self_obj = unwrap(self_param)
    if self_obj then patch_object(self_obj, {"MaxSessionPlayerCount"}, "HostSession self") end
    for i = 1, select("#", ...) do
        local arg = unwrap(select(i, ...))
        if is_valid(arg) then
            local ok, is_req = pcall(function() return arg:IsA("/Script/UWESonar.UWEHostSessionRequest") end)
            if ok and is_req then
                patch_object(arg, {"MaxPlayers", "MaxSessionPlayerCount"}, "HostSession req")
                break
            end
        end
    end
end

local function bootstrap()
    -- 1. 给所有 CDO 打补丁
    for _, c in ipairs(CLASSES) do patch_cdo(c) end

    -- 2. 给已经存在的实例打补丁 (UWEOnlineSessionSubsystem 通常游戏启动时就有了)
    for _, c in ipairs(CLASSES) do
        local ok, obj = pcall(function() return FindFirstOf(c.short) end)
        if ok and is_valid(obj) then patch_object(obj, c.fields, "Existing " .. c.short) end
    end

    -- 3. 监听以后新创建的实例
    for _, c in ipairs(CLASSES) do
        pcall(function()
            NotifyOnNewObject(c.class, function(new_obj)
                patch_object(new_obj, c.fields, "New " .. c.short)
            end)
        end)
    end

    -- 4. 拦截创建房间的调用, 在请求发出前改人数
    local ok = pcall(function()
        RegisterHook("/Script/UWESonar.UWEOnlineSessionSubsystem:HostSessionAsync",
            on_host_session_pre, function() end)
    end)
    if ok then log("HostSessionAsync hook 已注册") end

    log("加载完成, 人数上限 = %d", TARGET)
end

ExecuteInGameThread(bootstrap)

-- 引擎暖机时部分 CDO 可能还没就绪, 1 秒后再补一次
ExecuteWithDelay(1000, function()
    ExecuteInGameThread(function()
        for _, c in ipairs(CLASSES) do patch_cdo(c) end
    end)
end)
