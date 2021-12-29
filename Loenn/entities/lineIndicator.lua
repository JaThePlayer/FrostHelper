local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableLineStruct = require("structs.drawable_line")
local utils = require("utils")

local lineIndicator = {
    name = "FrostHelper/LineIndicator",
    nodeLimits = { 1, 999 },
}

jautils.createPlacementsPreserveOrder(lineIndicator, "default", {
    { "color", "ffffff", "color" },
})

local function point(position, color)
    return  jautils.getFilledRectangleSprite(utils.rectangle(position.x - 1, position.y - 1, 3, 3), color)
end

function lineIndicator.sprite(room, entity)
    if entity.nodes then
        local points = { entity.x, entity.y }
        local nodeSprites = { point(entity, "ff0000")}
        for _, value in ipairs(entity.nodes) do
            table.insert(points, value.x)
            table.insert(points, value.y)

            table.insert(nodeSprites, point(value, entity.color or jautils.colorWhite))
        end

        return jautils.addAll(
            drawableLineStruct.fromPoints(points, jautils.getColor(entity.color or jautils.colorWhite), 1):getDrawableSprite(),
            nodeSprites
        )
    end
    return jautils.getPixelSprite(entity.x, entity.y, jautils.getColor(entity.color or jautils.colorWhite))
end

-- make sure nodes aren't drawn because it looks stupid
function lineIndicator.nodeSprite() end

function lineIndicator.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, 4, 4)

    if entity.nodes then
        local nodeSelections = {}
        for _, node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x, node.y, 4, 4))
        end
        return main, nodeSelections
    end

    return main, { }
end

return lineIndicator