local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local noDashArea = {}

local particleColor = jautils.getColor("7f7f7f")
local fillColor = jautils.getColor("400000")
local lineColor = jautils.getColor("ff0000")

noDashArea.name = "FrostHelper/NoDashArea"
noDashArea.depth = -11000
noDashArea.nodeLineRenderType = "line"
noDashArea.placements = {
    name = "no_dash_area",
    data = {
        width = 16,
        height = 16,
        fastMoving = false,
    }
}

function noDashArea.sprite(room, entity)
    local rectangle = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    local sprites = jautils.getBorderedRectangleSprites(rectangle, fillColor, lineColor)

    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    for _ = 1, (entity.width * entity.height) / 16, 1 do
        table.insert(sprites, jautils.getPixelSprite(entity.x + math.random(2, entity.width - 2), entity.y + math.random(2, entity.height - 2), particleColor))
    end

    return sprites
end

function noDashArea.nodeLimits(room, entity)
    return 0, 1
end

function noDashArea.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, entity.width, entity.height)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x, node.y, entity.width, entity.height)
        end
    end

    return main, nodes
end

return noDashArea