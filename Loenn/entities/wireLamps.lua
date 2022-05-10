local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawing = require("utils.drawing")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local wireLamps = {
    name = "FrostHelper/WireLamps",
    nodeLimits = {1, 1},
    nodeLineRenderType = "none"
}

function wireLamps.depth(room, entity)
    return entity.above and -8500 or 2000
end

jautils.createPlacementsPreserveOrder(wireLamps, "default", {
    { "above", false },
    { "wireColor", "595866", "color" },
    { "lightCount", 3, "integer" },
    { "colors", "Red,Yellow,Blue,Green,Orange" },
    { "lightAlpha", 1.0 },
    { "lightStartFade", 8, "integer" },
    { "lightEndFade", 16, "integer" },
    { "lampSprite", "objects/FrostHelper/wireLamp" },
})

function wireLamps.sprite(room, entity)
    local color = jautils.getColor(entity.wireColor or "595866")

    local firstNode = entity.nodes[1]

    local start = {entity.x, entity.y}
    local stop = {firstNode.x, firstNode.y}
    local control = {
        (start[1] + stop[1]) / 2,
        (start[2] + stop[2]) / 2 + 24
    }

    local points = drawing.getSimpleCurve(start, stop, control)

    local sprites = { drawableLine.fromPoints(points, color, 1) }

    local lightCount = entity.lightCount
    local lightColors = jautils.getColors(entity.colors or "Red,Yellow,Blue,Green,Orange")

    for i = 1, lightCount, 1 do
        local percent = i / (lightCount + 1)
        local pointX, pointY = drawing.getCurvePoint(start, stop, control, percent)
        local lamp = drawableSprite.fromTexture(entity.lampSprite or "objects/FrostHelper/wireLamp", { x = pointX, y = pointY })
        lamp:setColor(lightColors[i % #lightColors])

        table.insert(sprites, lamp)
    end

    return sprites
end

function wireLamps.nodeSprite() end

function wireLamps.selection(room, entity)
    local main = utils.rectangle(entity.x - 2, entity.y - 2, 5, 5)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x - 2, node.y - 2, 5, 5)
        end
    end

    return main, nodes
end


return wireLamps