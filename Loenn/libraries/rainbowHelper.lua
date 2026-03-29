local utils = require("utils")
local frostSettings = require("mods").requireFromPlugin("libraries.settings")
local compat = require("mods").requireFromPlugin("libraries.compat")
---@module 'tracker'
local tracker = require("mods").requireFromPlugin("libraries.tracker")
---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local rainbowHelper = {}

---Implementation of Calc.YoYo from Monocle
---@param value number
---@return number
local function yoyo(value)
    if value <= 0.5 then return value * 2 end

    return 1 - (value - 0.5) * 2
end


local function vanillaRainbow(room, x, y)
    local rx,ry = x + room.x, y + room.y
    local posLength = math.sqrt((rx * rx) + (ry * ry)) % 280 / 280

    return { utils.hsvToRgb(0.4 + yoyo(posLength) * 0.4, 0.4, 0.9) }
end

-- Maddie' Helping Hand integration

local function distance(v1, v2)
    local x = v1.x - v2.x
    local y = v1.y - v2.y

    return math.sqrt(x*x + y*y)
end

local function clamp(value, min, max)
    value = ((value > max) and max or value);
    value = ((value < min) and min or value);
    return value;

end

local function lerp(value1, value2, amount)
	return value1 + (value2 - value1) * amount;
end

-- Performs linear interpolation of two colors
local function lerpColor(a, b, progress)
    local amount = clamp(progress, 0, 1);

    return {
        lerp(a[1], b[1], amount),
        lerp(a[2], b[2], amount),
        lerp(a[3], b[3], amount),
        lerp(a[4] or 1, b[4] or 1, amount)
    }
end

local function getModHue(colors, gradientSize, room, position, loopColors, center, gradientSpeed)
    local len = #colors

    if len == 1 then
        -- edge case: there is 1 color, just return it!
        return colors[1]
    end

    local progress = distance(position, center)

    while progress < 0 do
        progress = progress + gradientSize
    end

    progress = progress % gradientSize / gradientSize

    if not loopColors then
        progress = yoyo(progress);
    end

    if progress == 1 then
        return colors[len - 1];
    end

    local globalProgress = (len - 1) * progress
    local colorIndex = math.floor(globalProgress)
    local progressInIndex = globalProgress - colorIndex
    return lerpColor(colors[colorIndex + 1], colors[colorIndex + 2], progressInIndex)
end

---@param room Room
---@param x number
---@param y number
---@param controller UnknownEntity
---@return NormalizedColorTable
local function getColorFromController(room, x, y, controller)
    return getModHue(
        jautils.getColors(controller.colors),
        controller.gradientSize,
        room, {x = x, y = y}, controller.loopColors, {x=controller.centerX, y=controller.centerY}, controller.gradientSpeed
    )
end

---Gets the rainbow hue at the given location in the room.
---@param room Room
---@param x number
---@param y number
---@param width number?
---@param height number?
---@return NormalizedColorTable
function rainbowHelper.getRainbowHue(room, x, y, width, height)
    if compat.inLonn and frostSettings.rainbowsUseControllers() then
        width, height = width or 16, height or 16
        local selfRect = utils.rectangle(x - width / 2, y - height / 2, width, height)
        local entities = tracker.getAll(room, tracker.rainbowControllerTag)

        for _, entity in ipairs(entities) do
            local name = entity._name
            if name == "MaxHelpingHand/RainbowSpinnerColorController"
            or name == "MaxHelpingHand/FlagRainbowSpinnerColorController" then
                return getColorFromController(room, x, y, entity)
            elseif name == "MaxHelpingHand/RainbowSpinnerColorAreaController"
                or name == "MaxHelpingHand/FlagRainbowSpinnerColorAreaController" then

                if utils.aabbCheck(entity, selfRect) then
                    return getColorFromController(room, x, y, entity)
                end
            end
        end
    end


    return vanillaRainbow(room, x, y)
end

---Applies a rainbow color to all given sprites
---@param room Room
---@param sprites DrawableSprite[]
function rainbowHelper.rainbowifyAll(room, sprites)
    for _,sprite in ipairs(sprites) do
        sprite:setColor(rainbowHelper.getRainbowHue(room, sprite.x, sprite.y))
    end
end

return rainbowHelper