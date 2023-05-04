local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")
local drawableLineStruct = require("structs.drawable_line")
local drawableFunction = require("structs.drawable_function")
local utils = require("utils")
local drawing = jautils.inLonn and require("utils.drawing") or nil

local helper = {}

local function point(position, color)
    return jautils.getFilledRectangleSprite(utils.rectangle(position.x - 1, position.y - 1, 3, 3), color)
end

local function drawFilledPolygon(pt, fillColor)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(fillColor)
        local ok, triangles = pcall(love.math.triangulate, pt)
        if not ok then return end
        for _, triangle in ipairs(triangles) do
            love.graphics.polygon("fill", triangle)
        end
    end)
end

function helper.getSpriteFunc(nodeColor, lineColor, fillColor, mainNodeColor)
    nodeColor = jautils.getColor(nodeColor or "ffffff")
    lineColor = jautils.getColor(lineColor or "fcf579")
    fillColor = jautils.getColor(fillColor or "fcf57919")

    mainNodeColor = mainNodeColor or nodeColor

    return function(room, entity)
        if entity.nodes then
            local points = { entity.x, entity.y }
            local nodeSprites = { point(entity, mainNodeColor)}
            for _, value in ipairs(entity.nodes) do
                table.insert(points, value.x)
                table.insert(points, value.y)

                table.insert(nodeSprites, point(value, nodeColor))
            end

            local filled = entity.fill ~= false

            if filled then
                table.insert(points, entity.x)
                table.insert(points, entity.y)
            end

            return jautils.union(
                (filled and jautils.inLonn) and drawableFunction.fromFunction(drawFilledPolygon, points, fillColor),
                drawableLineStruct.fromPoints(points, lineColor, 1),
                nodeSprites
            )
        end
        return jautils.getPixelSprite(entity.x, entity.y, mainNodeColor)
    end
end

-- make sure nodes aren't drawn because it looks stupid
function helper.nodeSprite() end

function helper.selection(room, entity)
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

-- will be added to lonn soon:tm:
--[[
function helper.nodeAdded(room, entity, node)
    -- place node at mouse position
    local mx, my = love.mouse.getPosition()
    local nodeX, nodeY = viewportHandler.getRoomCoordinates(room, mx, my)

    local nodes = entity.nodes

    if node == 0 then
        table.insert(nodes, 1, {x = nodeX, y = nodeY})

    else
        table.insert(nodes, node + 1, {x = nodeX, y = nodeY})
    end

    return true
end
]]

return helper