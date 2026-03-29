local compat = require("mods").requireFromPlugin("libraries.compat")

local tracker = {}

tracker.rainbowControllerTag = "#rainbowController"

---@type {[string]: string }
local trackedTypes = {
}

---Tracks the given sid
---@param sid string
function tracker.track(sid)
    trackedTypes[sid] = sid
end

---Tracks the given sid
---@param sid string
---@param trackedAs string
function tracker.trackAs(sid, trackedAs)
    trackedTypes[sid] = trackedAs
end

tracker.track("FrostHelper/IceSpinner")
tracker.track("FrostHelper/TriggerSpinner")

tracker.trackAs("MaxHelpingHand/RainbowSpinnerColorController", tracker.rainbowControllerTag)
tracker.trackAs("MaxHelpingHand/FlagRainbowSpinnerColorController", tracker.rainbowControllerTag)
tracker.trackAs("MaxHelpingHand/RainbowSpinnerColorAreaController", tracker.rainbowControllerTag)
tracker.trackAs("MaxHelpingHand/FlagRainbowSpinnerColorAreaController", tracker.rainbowControllerTag)

---@class Room
---@field __fh_tracker {[string]: Entity[]}

---@param room Room
local function initTracker(room)
    room.__fh_tracker = {}
    local t = room.__fh_tracker

    for index, e in ipairs(room.entities) do
        local name = e._name
        local as = trackedTypes[name]
        if as then
            local trackerEntry = t[as]
            if not trackerEntry then
                t[as] = { e }
            else
                table.insert(trackerEntry, e)
            end
        end
    end
end

---@param room Room
local function initTrackerIfNeeded(room)
    if not room.__fh_tracker then
        initTracker(room)
    end
end

---@param map Map
local function resetTrackerInAllRooms(map)
    for i, room in ipairs(map.rooms) do
        room.__fh_tracker = nil
    end
end


---Gets all entities in the given room with the provided sid.
---@param room Room
---@param sid string
---@return Entity[]
function tracker.getAll(room, sid)
    initTrackerIfNeeded(room)

    local t = room.__fh_tracker
    if t then
        return t[sid] or {}
    end

    -- SLOW PATH
    return {}
end

if compat.inLonn then
    -- this can be removed if a tracker gets added to lonn
    local history = require("history")
    local loadedState = require("loaded_state")

    local old_history_reset = history.reset
    function history.reset()
        old_history_reset()
        resetTrackerInAllRooms(loadedState.map)
    end

    local old_history_undo = history.undo
    function history.undo()
        old_history_undo()
        resetTrackerInAllRooms(loadedState.map)
    end

    local old_history_redo = history.redo
    function history.redo()
        old_history_redo()
        resetTrackerInAllRooms(loadedState.map)
    end

    local old_history_addSnapshot = history.addSnapshot
    function history.addSnapshot(snapshot)
        old_history_addSnapshot(snapshot)
        resetTrackerInAllRooms(loadedState.map)
    end
end

return tracker