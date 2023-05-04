local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local heldRefill = {}
heldRefill.name = "FrostHelper/HeldRefill"
heldRefill.depth = -100
heldRefill.nodeLimits = { 1, math.huge }
heldRefill.nodeLineRenderType = "line"

jautils.createPlacementsPreserveOrder(heldRefill, "normal", {
    { "speed", 6 }
})

function heldRefill.nodeSprite() end

function heldRefill.sprite(room, entity)
    local sprites = {drawableSpriteStruct.fromTexture("objects/refill/idle00", entity)}

    local points = { entity.x, entity.y }
    for _, value in ipairs(entity.nodes) do
        table.insert(points, value.x)
        table.insert(points, value.y)
    end

    return jautils.union(
        sprites,
        drawableLine.fromPoints(points, entity.lineColor, 1)
    )
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