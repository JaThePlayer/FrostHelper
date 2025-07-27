---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")
local utils = require("utils")
local drawing = require("utils.drawing")
local drawableRectangle = require("structs.drawable_rectangle")

---@class DynamicRainGenerator : Entity
---@field depth? integer
---@field editorPreviewLength? integer

---@type EntityHandler<DynamicRainGenerator>
local rain = {
    name = "FrostHelper/DynamicRainGenerator",
}

local indicatorColor = jautils.getConstColor("0000aaaa")
local fillColor = jautils.getConstColor("0000aaaa")
local borderColor = jautils.getConstColor("0000ff")

jautils.createPlacementsPreserveOrder(rain, "default", {
    { "width", 16 },
    { "colors", "161933", "colorList" },
    { "opacity", 1 },
    { "density", 0.75 },
    { "speedRange", "200,600", "range", {from=200, to=600} },
    { "scaleRange", "4,16", "range", {from=4, to=16} },
    { "rotationRange", "-0.05,0.05", "range", {from=-0.05, to=0.05} },
   -- { "generatorLength", -1, "integer" }, -- undecided if this should be public
    { "depth", 2000, "depth" },
    { "enableFlag", "", "FrostHelper.condition" },
   -- { "flagIfPlayerInside", "" }, -- undecided if I like the current impl
    { "collideWith", "Celeste.Player,Celeste.Solid", "typesList" },
    { "presimulationTime", 1 },
    { "editorPreviewLength", 32 },
    { "rainbow", false },
    { "attachToSolid", false }
})

function rain.depth(room, entity)
    return entity.depth or 0
end

function rain.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, math.max(entity.width or 8, 8), math.max(entity.height or 8, 8))
end

local function getMinMaxRotation(entity)
    local str = tostring(entity.rotationRange or "-0.05,0.05")
    local singular = tonumber(str)
    if singular then
        return singular, singular
    end

    local min, max = str:match("(%-?%d+),(%-?%d+)")
    if not min then
        return 0, 0
    end

    return tonumber(min), tonumber(max)
end

-- drawableLine has 0.2px offsets, we'll just reimplement the main part of drawableLine without said offset
local function getRotatedRectangleSprite(x1, y1, x2, y2, color, thickness)
    local theta = math.atan2(y2 - y1, x2 - x1)
    local magnitude = math.sqrt((x1 - x2)^2 + (y1 - y2)^2)

    local x = x1
    local y = y1
    local width = magnitude

    local sprite = drawableRectangle.fromRectangle("fill", x, y, width, thickness, color):getDrawableSprite()
    sprite.rotation = theta

    return sprite
end

local function addLineAtAngle(sprites, x, y, angle, len)
    local angleVecX, angleVecY = jautils.angleToVector(angle, 1.)
    local offsetX, offsetY = angleVecX * len, angleVecY * len
    local segments = drawing.getDashedLineSegments(math.floor(x + offsetX), math.floor(y + offsetY), math.floor(x), math.floor(y))

    for _, s in ipairs(segments) do
        table.insert(sprites, getRotatedRectangleSprite(s[1], s[2], s[3], s[4], indicatorColor, 1))
    end
end

function rain.sprite(room, entity)
    local rectangle = rain.selection(room, entity)

    local sprites = jautils.getBorderedRectangleSprites(rectangle, fillColor, borderColor)

    local minRot, maxRot = getMinMaxRotation(entity)
    local x, y, w, h = entity.x, entity.y, entity.width, entity.height

    if w and maxRot >= 90 then
        x = x + 1
    elseif h and maxRot >= 270 then
        y = y + 1
    end
    local previewLen = entity.editorPreviewLength or 32
    addLineAtAngle(sprites, x, y, jautils.degreeToRadians(maxRot + 90), previewLen)

    if w then
        addLineAtAngle(sprites, x + w - 1, y, jautils.degreeToRadians(minRot + 90), previewLen)
    else
        addLineAtAngle(sprites, x, y + h - 1, jautils.degreeToRadians(minRot + 90), previewLen)
    end

    return sprites
end

return rain