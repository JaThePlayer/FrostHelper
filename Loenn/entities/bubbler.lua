local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local bubbler = {
    name = "FrostHelper/Bubbler",
    depth = 0,
    texture = "objects/FrostHelper/bubble00",
    nodeLimits = {2, 2},
    nodeLineRenderType = "line",
    --offset = { 0, -10 },
    justification = { .5, .5 },
    nodeOffset = { 0, -10 },
}

jautils.createPlacementsPreserveOrder(bubbler, "normal", {
    { "color", "ffffff", "color"},
    { "hitbox", "R,14,14,0,0", "FrostHelper.collider" },
    { "visible", true },
    { "showReturnIndicator", true },
})


function bubbler.sprite(room, entity)
    local mainSprite = drawableSpriteStruct.fromTexture("objects/FrostHelper/bubble00", entity)

    return mainSprite
end

function bubbler.nodeSprite(room, entity, node, nodeIndex)
    local nodeSprite = drawableSpriteStruct.fromTexture("objects/FrostHelper/bubble00", node)
    nodeSprite:addPosition(0, -10)

    return nodeSprite
end

function bubbler.selection(room, entity)
    local main = utils.rectangle(entity.x - 8, entity.y - 8, 17, 17)

    if entity.nodes then
        local nodeSelections = {}
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x - 8, node.y - 8 - 10, 17, 17))
        end
        return main, nodeSelections
    end

    return main, { }
end

return bubbler