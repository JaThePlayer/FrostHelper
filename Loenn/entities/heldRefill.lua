local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local heldRefill = {}
heldRefill.name = "FrostHelper/HeldRefill"
heldRefill.depth = -100
heldRefill.nodeLimits = { 1, 999 }
heldRefill.nodeLineRenderType = "line"

jautils.createPlacementsPreserveOrder(heldRefill, "normal", {
    { "speed", 6 }
})

function heldRefill.sprite(room, entity)
    local sprites = {drawableSpriteStruct.fromTexture("objects/refill/idle00", entity)}

    for i = 1, #entity.nodes - 1 do
        local points = {entity.nodes[i].x, entity.nodes[i].y, entity.nodes[i+1].x, entity.nodes[i+1].y}
        local line = drawableLine.fromPoints(points, entity.lineColor, 1)
        line.depth = -100
        for _, sprite in ipairs(line:getDrawableSprite()) do
            table.insert(sprites, sprite)
        end
    end

    return sprites
end

function heldRefill.selection(room, entity)
    local main = utils.rectangle(entity.x - 4, entity.y - 4, 8, 8)

    if entity.nodes then
        local nodeSelections = {}
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x - 4, node.y - 4, 8, 8))
        end
        return main, nodeSelections
    end

    return main, { }
end

return heldRefill