local compat = require("mods").requireFromPlugin("libraries.compat")

local loadedState = nil
local loadedState_getSelectedRoom = nil
if compat.inLonn or compat.inRysy then
    loadedState = require("loaded_state")
    loadedState_getSelectedRoom = loadedState.getSelectedRoom
end

local mapScanHelper = {}

local function _findAllTagsScan(into, entities, tagPropName)
    if not entities then
        return
    end

    for _, en in ipairs(entities) do
        local tagStr = en[tagPropName]
        if tagStr then
            -- tags can be split by ','
            local tags = tagStr:split(',')()
            for _, tag in ipairs(tags) do
                into[tag] = true
            end
        end
    end
end

---Finds all cloud tags used in entities and triggers in the room
---@param room table
---@return table<string>
function mapScanHelper.findAllCloudTagsInRoom(room)
    local ret = {}
    _findAllTagsScan(ret, room.entities, "cloudTag")
    _findAllTagsScan(ret, room.triggers, "cloudTag")

    -- convert from a table<string, bool>, to a list<string>
    local list = {}
    for tag, _ in pairs(ret) do
        table.insert(list, tag)
    end

    return list
end

---Finds all tags used in FG and BG stylegrounds in the given map
---@param map table
---@return table<string>
function mapScanHelper.findAllStylegroundTagsInMap(map)
    local ret = {}
    _findAllTagsScan(ret, map.stylesFg, "tag")
    _findAllTagsScan(ret, map.stylesBg, "tag")

    -- convert from a table<string, bool>, to a list<string>
    local list = {}
    for tag, _ in pairs(ret) do
        table.insert(list, tag)
    end

    return list
end

local entitiesUsingCounters = {
    ["FrostHelper/SessionCounterTrigger"] = "value",
    ["FrostHelper/OnCounterActivator"] = "target",
    ["FrostHelper/SetCounterActivator"] = "target",
    ["FrostHelper/FlagCounterController"] = "",
}

local function _findAllCountersScan(into, entities)
    if not entities then
        return
    end

    for _, en in ipairs(entities) do
        local targetValKey = entitiesUsingCounters[en._name]
        if targetValKey then
            local counter = en.counter
            if counter and counter ~= "" then
                into[counter] = true
            end

            local targetVal = en[targetValKey]
            if targetVal and not tonumber(targetVal) then
                into[targetVal] = true
            end
        end
    end
end

---Finds all session counters used in entities and triggers in the room
---@param room table
---@return table<string>
function mapScanHelper.findAllSessionCounters(room)
    room = room or (loadedState_getSelectedRoom and loadedState_getSelectedRoom() or nil)
    if not room then
        return {}
    end

    local ret = {}

    _findAllCountersScan(ret, room.entities)
    _findAllCountersScan(ret, room.triggers)

    -- convert from a table<string, bool>, to a list<string>
    local list = {}
    for tag, _ in pairs(ret) do
        table.insert(list, tag)
    end

    return list
end

return mapScanHelper