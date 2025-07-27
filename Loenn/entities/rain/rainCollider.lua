---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")
local utils = require("utils")

---@class RainCollider : Entity

---@type EntityHandler<RainCollider>
local rainCollider = {
    name = "FrostHelper/RainCollider",
}

local indicatorColor = jautils.getConstColor("0000aaaa")
local fillColor = jautils.getConstColor("0000aaaa")
local borderColor = jautils.getConstColor("74C5C5")

jautils.createPlacementsPreserveOrder(rainCollider, "default", {
    { "width", 8 },
    { "height", 8 },
    { "makeSplashes", true },
    { "passThroughChance", 0 },
})

function rainCollider.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end

function rainCollider.sprite(room, entity)
    local rectangle = rainCollider.selection(room, entity)
    return jautils.getBorderedRectangleSprites(rectangle, fillColor, borderColor)
end

return rainCollider