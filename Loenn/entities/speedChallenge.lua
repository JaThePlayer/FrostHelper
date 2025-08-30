local utils = require("utils")
---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")

local speedRingChallenge = {}
speedRingChallenge.name = "FrostHelper/SpeedRingChallenge"
speedRingChallenge.nodeLineRenderType = "line"

local sharedPlacementData = {
    { "timeLimit", 1 },
    { "name", "fh_test" },
    { "flagOnWin", "" },
    { "colorUnbeaten", "87cefa", "color" },
    { "colorBeaten", "0000ff", "color" },
    { "progressCounter", "" },
    { "playbackName", "", "playback" },
    { "playbackOffsetX", 0.0 },
    { "playbackOffsetY", 0.0 },
    { "playbackStartTrim", 0.0 },
    { "playbackEndTrim", 0.0 },
    { "recordPlayback", false },
    { "spawnBerry", true },
}

jautils.createPlacementsPreserveOrder(speedRingChallenge, "normal", sharedPlacementData, true)

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

---@type EntityHandler<UnknownEntity>
local speedRingChallenge3d = {}
speedRingChallenge3d.name = "FrostHelper/SpeedRingChallenge3d"
speedRingChallenge3d.nodeLineRenderType = "none"
speedRingChallenge3d.associatedMods = { "FrostHelper", "CommunalHelper" }

local data3d = utils.deepcopy(sharedPlacementData)
table.insert(data3d, { "showAllRings", false })

jautils.createPlacementsPreserveOrder(speedRingChallenge3d, "normal", data3d, true)

local dotTexture = "objects/CommunalHelper/elytraRing/dot" -- TODO: replace

local ringColorMainNode = { 0.8, 0, 0, 1.0 }
local ringColorLastNode = { 0, 0, 0.8, 1.0 }
local ringColorMissingNode = { 0, 0.8, 0, 1.0 }
local ringColor = {0.8, 0.8, 0.8, 1.0}

speedRingChallenge3d.nodeLimits = function (room, entity)
    if entity.spawnBerry then
        return 4, math.huge
    end
    return 3, math.huge
end

local function createIcon(texture, atx, aty, scale, rot, color, angle)
    local icon = drawableSprite.fromTexture(texture, {x = atx, y = aty})
    icon.rotation = rot + angle
    icon:setJustification(0.5, 0.5)
    icon:setScale(scale, scale)
    icon.color = color

    return icon
end

function speedRingChallenge3d.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}

    local points = { }
    for i = 0, #nodes - 1, 2 do
        local a = nodes[i] or { x = x, y = y }
        local b = nodes[i+1]
        local cx, cy = (a.x + b.x) / 2, (a.y + b.y) / 2

        table.insert(points, cx)
        table.insert(points, cy)
        table.insert(sprites, drawableLine.fromPoints({a.x, a.y, b.x, b.y}, ringColor))

        local dx, dy = b.y - a.y, a.x - b.x -- perpendicular (-y, x)
        local angle = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi) + math.pi / 4
        table.insert(sprites, createIcon(dotTexture, a.x, a.y, 2, 0, i == 0 and ringColorMainNode or ringColor, angle))
        table.insert(sprites, createIcon(dotTexture, b.x, b.y, 2, 0, i + 2 > #nodes - 1 and ringColorLastNode or ringColor, angle))
    end

    if #nodes % 2 == 0 then
        local n = nodes[#nodes]
        table.insert(sprites, createIcon(dotTexture, n.x, n.y, 2, 0, ringColorMissingNode, 0))
    end

    local line = drawableLine.fromPoints(points, ringColor)

    table.insert(sprites, line)

    return sprites
end

function speedRingChallenge3d.nodeSprite()
    return {}
end

function speedRingChallenge3d.selection(room, entity)
    local main = utils.rectangle(entity.x - 4, entity.y - 4, 8, 8)

    if entity.nodes then
        local nodeSelections = {}
        for _, node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x - 4, node.y - 4, 8, 8))
        end
        return main, nodeSelections
    end

    return main, { }
end

return {
    speedRingChallenge,
    speedRingChallenge3d,
}