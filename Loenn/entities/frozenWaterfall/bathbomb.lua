local drawableLine = require("structs.drawable_line")
local drawing = require("utils.drawing")

---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

---@type EntityHandler<UnknownEntity>
local controller = {
    name = "FrostHelper/BathBomb",
    depth = 100,
}

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "directory", "danger/" },
    { "color", "LightSkyBlue", "color" },
    { "bubble", false },
})

controller.sprite = function (room, entity)
    local mainSprite = jautils.getCustomSprite(entity, "directory", "snowball00", "danger/snowball00", "color")

    if entity.bubble then
        local x, y = entity.x or 0, entity.y or 0
        local points = drawing.getSimpleCurve({x - 11, y - 1}, {x + 11, y - 1}, {x - 0, y - 6})
        local lineSprites = drawableLine.fromPoints(points):getDrawableSprite()

        table.insert(lineSprites, 1, mainSprite)

        return lineSprites

    else
        return mainSprite
    end
end

return controller