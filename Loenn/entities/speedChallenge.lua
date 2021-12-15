local utils = require("utils")
local speedRingChallenge = {}
speedRingChallenge.name = "FrostHelper/SpeedRingChallenge"
speedRingChallenge.nodeLineRenderType = "line"
speedRingChallenge.placements =
{
    name = "normal",
    data = {
        width = 16,
        height = 16,
        timeLimit = 1.0,
        name = "fh_test",
    }
}

local ellipseColor = { 1, 1, 1, 1 }

function speedRingChallenge.nodeLimits(room, entity)
    return 1, 255
end

function speedRingChallenge.draw(room, entity)
    local pr, pg, pb, pa = love.graphics.getColor()

    love.graphics.setColor(ellipseColor)
    love.graphics.ellipse("line", entity.x + entity.width / 2, entity.y + entity.height / 2, entity.width / 2, entity.height / 2)
    for _,v in ipairs(entity.nodes) do
        love.graphics.ellipse("line", v.x + entity.width / 2, v.y + entity.height / 2, entity.width / 2, entity.height / 2)
    end
    love.graphics.setColor(pr, pg, pb, pa)
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