local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")
local drawableLineStruct = require("structs.drawable_line")

local arbitraryLight = {
    name = "FrostHelper/ArbitraryLight",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 6,
    nodeVisibility = "never",
}

jautils.createPlacementsPreserveOrder(arbitraryLight, "default", {
    { "color", "ffffff", "color" },
    { "alpha", 1.0 },
    { "startFade", 16, "integer" },
    { "endFade", 64, "integer" },
    { "radius", 24, "integer" },
    { "bloomAlpha", 0 },
    { "connectFirstAndLastNode", false },
})

local function point(position, color)
    return jautils.getFilledRectangleSprite(utils.rectangle(position.x - 1, position.y - 1, 3, 3), color)
end

arbitraryLight.sprite = function (room, entity)
    local nodeColor = jautils.getColor("ffffff")
    local lineColor = jautils.getColor("ffffff")
    local detailLineColor = jautils.getColor("aaaaaaaa")
    local mainNodeColor = jautils.getColor("ff0000")
    local fillColor = jautils.getColor("ffffff19")

    local nodes = entity.nodes
    if nodes and #nodes > 0 then
        local fill = arbitraryShapeEntity.isPolygonSupported and {} or nil
        local nodeSprites = { point(entity, mainNodeColor) }
        local detailedLineNodes = {}
        local x = entity.x
        local y = entity.y
        local pos = {x=x, y=y}

        local function tri(fill, a, b, c)
            if fill then
                table.insert(fill, a.x)
                table.insert(fill, a.y)
                table.insert(fill, b.x)
                table.insert(fill, b.y)
                table.insert(fill, c.x)
                table.insert(fill, c.y)
            end
        end

        for i = 1, #nodes - 1, 1 do
            local nodeA = nodes[i]
            local nodeB = nodes[i + 1]

            tri(fill, pos, nodeA, nodeB)
            table.insert(detailedLineNodes, drawableLineStruct.fromPoints({ x, y, nodeA.x, nodeA.y }, detailLineColor, 0.5))
            table.insert(detailedLineNodes, drawableLineStruct.fromPoints({ nodeA.x, nodeA.y, nodeB.x, nodeB.y }, detailLineColor, 0.5))
        end
        table.insert(detailedLineNodes, drawableLineStruct.fromPoints({ x, y, nodes[#nodes].x, nodes[#nodes].y }, detailLineColor, 0.5))

        for _, value in ipairs(nodes) do
            table.insert(nodeSprites, point(value, nodeColor))
        end


        if entity.connectFirstAndLastNode == true then
            tri(fill, pos, nodes[1], nodes[#nodes])
            table.insert(detailedLineNodes, drawableLineStruct.fromPoints({ nodes[1].x, nodes[1].y, nodes[#nodes].x, nodes[#nodes].y }, detailLineColor, 0.5))
        end

        return jautils.union(
            fill and arbitraryShapeEntity.createPolygonSpritePreTriangulated(fill, fillColor) or {},
            detailedLineNodes,
            nodeSprites
        )
    end
    return jautils.getPixelSprite(entity.x, entity.y, mainNodeColor)
end

arbitraryLight.nodeSprite = arbitraryShapeEntity.nodeSprite
arbitraryLight.selection = arbitraryShapeEntity.selection

return arbitraryLight