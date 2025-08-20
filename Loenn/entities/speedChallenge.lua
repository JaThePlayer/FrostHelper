local utils = require("utils")
---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local speedRingChallenge = {}
speedRingChallenge.name = "FrostHelper/SpeedRingChallenge"
speedRingChallenge.nodeLineRenderType = "line"

jautils.createPlacementsPreserveOrder(speedRingChallenge, "normal", {
    { "timeLimit", 1 },
    { "name", "fh_test" },
    { "flagOnWin", "" },
    { "playbackName", "", "playback" },
    { "playbackOffsetX", 0.0 },
    { "playbackOffsetY", 0.0 },
    { "playbackStartTrim", 0.0 },
    { "playbackEndTrim", 0.0 },
    { "recordPlayback", false },
    { "spawnBerry", true },
}, true)

local ellipseColor = { 1, 1, 1, 1 }

speedRingChallenge.nodeLimits = { 1, 255 }

function speedRingChallenge.sprite(room, entity)
    return jautils.getEllipseSprite(entity.x + entity.width / 2, entity.y + entity.height / 2, entity.width / 2, entity.height / 2, ellipseColor)
end

function speedRingChallenge.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    if entity.nodes then
        local nodeSelections = {}
        for _, node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x, node.y, entity.width, entity.height))
        end
        return main, nodeSelections
    end

    return main, { }
end

return speedRingChallenge