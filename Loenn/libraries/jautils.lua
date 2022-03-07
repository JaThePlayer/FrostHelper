local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangleStruct = require("structs.drawable_rectangle")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableLineStruct = require("structs.drawable_line")
local utils = require("utils")
local xnaColors = require("consts.xna_colors")

---@alias color string | table<integer, number>
---@alias sprite table

local jautils = {}

jautils.easings = require("mods").requireFromPlugin("libraries.easings")

--[[
    UTILS
]]

function jautils.addAll(addTo, toAddTable, insertLoc)
    if insertLoc then
        for _, value in ipairs(toAddTable) do
            table.insert(addTo, insertLoc, value)
        end
    else
        for _, value in ipairs(toAddTable) do
            table.insert(addTo, value)
        end
    end


    return addTo
end

--[[
    PLACEMENTS
]]

jautils.fieldTypeOverrides = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    },
    editableDropdown = function(data)
        return {
            options = data,
            editable = true
        }
    end,
    dropdown = function(data)
        return {
            options = data,
            editable = false
        }
    end,
}

function jautils.createPlacementsPreserveOrder(handler, placementName, placementData, appendSize)
    handler.placements = {{
        name = placementName,
        data = {}
    }}
    local fieldOrder = { "x", "y" }
    local fieldInformation = {}
    local hasAnyFieldInformation = false

    if appendSize then
        table.insert(placementData, 1, { "height", 16 })
        table.insert(placementData, 1, { "width", 16 })
    end

    for _,v in ipairs(placementData) do
        local fieldName, defaultValue, fieldType, fieldData = v[1], v[2], v[3], v[4]

        table.insert(fieldOrder, fieldName)
        handler.placements[1].data[fieldName] = defaultValue
        if fieldType then
            local override = jautils.fieldTypeOverrides[fieldType]
            if override then
                -- use an override from jaUtils if available
                -- used by color to automatically support XNA color names
                local typ = type(override)
                if typ == "function" then
                    fieldInformation[fieldName] = override(fieldData)
                else
                    fieldInformation[fieldName] = override
                end
            else
                -- otherwise just use it normally
                local typ = type(fieldType)
                if typ == "table" then
                    if fieldType[fieldType] then
                        -- we have a full field definition here, don't do anything about it
                        fieldInformation[fieldName] = fieldType
                    else
                        -- didn't define a type, treat it as a dropdown
                        fieldInformation[fieldName] = jautils.fieldTypeOverrides.dropdown(fieldType)
                    end
                else
                    fieldInformation[fieldName] = { fieldType = fieldType }
                end
            end

            hasAnyFieldInformation = true
        end
    end

    handler.fieldOrder = fieldOrder
    if hasAnyFieldInformation then
        handler.fieldInformation = fieldInformation
    end
end

function jautils.addPlacement(handler, placementName, ...)
    local newPlacement = {
        name = placementName,
        data = {}
    }

    -- copy defaults from main placement
    for k,v in pairs(handler.placements[1].data) do
        newPlacement.data[k] = v
    end

    -- apply new parameters
    for _,v in ipairs(...) do
        newPlacement.data[v[1]] = v[2]
    end

    table.insert(handler.placements, newPlacement)
end


--[[
    SPRITES
]]
---@type color
jautils.colorWhite = utils.getColor("ffffff")
jautils.colorBlack = {0, 0, 0, 1}
jautils.radian = math.pi / 180

function jautils.getOutlinedSpriteFromPath(data, spritePath, color, outlineColor, scaleX)
    local sprite = drawableSpriteStruct.fromTexture(spritePath, data)
    sprite:setColor(color or jautils.colorWhite)
    sprite.scaleX = scaleX or 1

    local sprites = jautils.getBorder(sprite, outlineColor or jautils.colorBlack)

    table.insert(sprites, sprite)

    return sprites
end

function jautils.getBorder(sprite, color)
    local function get(xOffset,yOffset)
        local texture = drawableSpriteStruct.fromMeta(sprite.meta, sprite)
        texture.x += xOffset
        texture.y += yOffset
        texture.depth = 2 -- fix preview depth
        texture.color = color and utils.getColor(color) or {0, 0, 0, 1}

        return texture
    end

    return {
        get(0, -1),
        get(0, 1),
        get(-1, 0),
        get(1, 0)
    }
end

function jautils.getBordersForAll(sprites, color)
    -- manually iterate since we're changing the table
    local newSprites = {}

    local spriteLen = #sprites
    for i = 1, spriteLen, 1 do
        jautils.addAll(newSprites, jautils.getBorder(sprites[i], color))
    end

    return jautils.addAll(newSprites, sprites)
end

function jautils.copyTexture(baseTexture, x, y, relative)
    local texture = drawableSpriteStruct.fromMeta(baseTexture.meta, baseTexture)
    texture.x = relative and baseTexture.x + x or x
    texture.y = relative and baseTexture.y + y or y

    return texture
end

function jautils.getCustomSpritePath(entity, spritePropertyName, spritePostfix, fallback)
    local sprite = nil

    local path = entity[spritePropertyName]
    if path then
        local fullPath = path .. (spritePostfix or "")
        sprite = drawableSpriteStruct.fromTexture(fullPath, entity)
        if sprite then
            return fullPath, sprite
        end

        -- Celeste will also try appending 00, let's try that
        sprite = drawableSpriteStruct.fromTexture(fullPath .. "00", entity)
        if sprite then
            return fullPath .. "00", sprite
        end
    end

    return fallback, drawableSpriteStruct.fromTexture(fallback, entity)
end

---Returns the custom sprite for an entity, retrieved from gfx.game[entity[spritePropertyName]], tinted with entity.tint, or gfx.game[fallback]
---@param entity table
---@param spritePropertyName string
---@param spritePostfix string
---@param fallback string
---@param spriteTintPropertyName string
---@return sprite?
function jautils.getCustomSprite(entity, spritePropertyName, spritePostfix, fallback, spriteTintPropertyName)
    local spritePath, sprite = jautils.getCustomSpritePath(entity, spritePropertyName, spritePostfix, fallback)

    if sprite and entity[spriteTintPropertyName or "tint"] then
        sprite:setColor(entity[spriteTintPropertyName or "tint"])
    end

    return sprite
end

---Returns a list of sprites needed to render a block using a 24x24 texture, split and tiled into 8x8 chunks
---@param entity table
---@param blockSpritePath string
---@return table<integer, sprite>
function jautils.getBlockSprites(entity, blockSpritePath)
    local sprites = {}

    for x = 0, entity.width - 1, 8 do
        for y = 0, entity.height - 1, 8 do
            local blockSprite = drawableSpriteStruct.fromTexture(blockSpritePath, entity)
            blockSprite:setPosition(entity.x + x, entity.y + y)
            blockSprite:setJustification(0.0, 0.0)
            blockSprite:useRelativeQuad((x == 0 and 0 or (x == entity.width - 8 and 16 or 8)) + blockSprite.meta.offsetX, (y == 0 and 0 or (y == entity.height - 8 and 16 or 8)) + blockSprite.meta.offsetY, 8, 8)

            table.insert(sprites, blockSprite)
        end
    end

    return sprites
end

local defaultNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

---Returns a list of sprites needed to render a block using a 24x24 texture, split and tiled into 8x8 chunks. The sprite is retrieved using jautils.getCustomSprite
---@param entity table
---@param spritePropertyName string
---@param spritePostfix string
---@param fallback string
---@return table<integer, sprite>
function jautils.getCustomBlockSprites(entity, spritePropertyName, spritePostfix, fallback, spriteTintPropertyName, ninePatchOptions)

    if ninePatchOptions == "old" then
        local sprites = {}
        local baseTexture = jautils.getCustomSprite(entity, spritePropertyName, spritePostfix, fallback, spriteTintPropertyName)
        baseTexture:setJustification(0.0, 0.0)

        local textureWidth, textureHeight = baseTexture.meta.realWidth , baseTexture.meta.realHeight
        local rightCornerPositionX, rightCornerPositionY = textureWidth - 8, textureHeight - 8

        for x = 0, entity.width - 1, 8 do
            for y = 0, entity.height - 1, 8 do
                local blockSprite = jautils.copyTexture(baseTexture, (x == 0 and -baseTexture.meta.offsetX or x) + baseTexture.meta.offsetX, (y == 0 and -baseTexture.meta.offsetY or y) + baseTexture.meta.offsetY)
                --local blockSprite = jautils.getCustomSprite(entity, spritePropertyName, spritePostfix, fallback, spriteTintPropertyName)
                if blockSprite then
                    --blockSprite:addPosition((x == 0 and -blockSprite.meta.offsetX or x) + blockSprite.meta.offsetX, (y == 0 and -blockSprite.meta.offsetY or y) + blockSprite.meta.offsetY)
                    --blockSprite:setJustification(0.0, 0.0)
                    blockSprite:useRelativeQuad(
                        (x == 0 and -blockSprite.meta.offsetX or (x == entity.width - 8 and rightCornerPositionX or 8)) + blockSprite.meta.offsetX,
                        (y == 0 and -blockSprite.meta.offsetY or (y == entity.height - 8 and rightCornerPositionY or 8)) + blockSprite.meta.offsetY,
                        8, 8
                    )

                    table.insert(sprites, blockSprite)
                end
            end
        end
        return sprites
    else
        local baseTexture = jautils.getCustomSpritePath(entity, spritePropertyName, spritePostfix, fallback)
        local ninePatch = drawableNinePatch.fromTexture(baseTexture, ninePatchOptions or defaultNinePatchOptions, entity.x, entity.y, entity.width, entity.height)
        return ninePatch:getDrawableSprite()
    end
end

---Returns a 1x1 drawableSprite located at x,y
---@param x number
---@param y number
---@param color color
---@return sprite sprite
function jautils.getPixelSprite(x, y, color)
    return drawableSpriteStruct.fromInternalTexture(drawableRectangleStruct.tintingPixelTexture, { x = x, y = y, jx = 0, jy = 0, color = color or jautils.colorWhite })
end

---Returns a sprite for a rectangle, optionally tinted with fillColor
---@param rectangle table
---@param fillColor color
---@return sprite sprites
function jautils.getFilledRectangleSprite(rectangle, fillColor)
    return drawableRectangleStruct.fromRectangle("fill", rectangle.x, rectangle.y, rectangle.width, rectangle.height, fillColor or jautils.colorWhite):getDrawableSprite()
end

---Returns 4 sprites for a hollow rectangle, optionally tinted with fillColor
---@param rectangle table
---@param fillColor color
---@return sprite[] sprites
function jautils.getHollowRectangleSprites(rectangle, fillColor)
    return drawableRectangleStruct.fromRectangle("line", rectangle.x, rectangle.y, rectangle.width, rectangle.height, fillColor or jautils.colorWhite):getDrawableSprite()
end

function jautils.getBorderedRectangleSprites(rectangle, fillColor, lineColor)
    return drawableRectangleStruct.fromRectangle("bordered", rectangle.x, rectangle.y, rectangle.width, rectangle.height, fillColor or jautils.colorWhite, lineColor or jautils.colorWhite):getDrawableSprite()
end

function jautils.getLineSprite(x1, y1, x2, y2, color, thickness)
    return drawableLineStruct.fromPoints({x1, y1, x2, y2}, color or jautils.colorWhite, thickness or 1)
end

---Calls jautils.getColor on all elements of the comma-seperated list and returns a table of results.
---@param list string
---@return color[] colors
function jautils.getColors(list)
    local colors = {}
    local split = string.split(list, ",")()
    for _, value in ipairs(split) do
        table.insert(colors, jautils.getColor(value))
    end

    return colors
end

--[[
    MATH
]]

function jautils.map(val, min, max, newMin, newMax)
    return (val - min) / (max - min) * (newMax - newMin) + newMin
end

---Implementation of Calc.YoYo from Monocle
---@param value number
---@return number
function jautils.yoyo(value)
    if value <= 0.5 then return value * 2 end

    return 1 - (value - 0.5) * 2
end

---Returns the sign of a number (1 or -1)
---@param num number
---@return number sign
function jautils.sign(num)
    return num > 0 and 1 or -1
end

function jautils.normalize(x,y)
    local magnitude = math.sqrt(x*x + y*y)

    return x / magnitude, y / magnitude
end

function jautils.perpendicularVector(vector)
    return { -vector[2], vector[1] }
end

function jautils.perpendicular(x, y)
    return -y, x
end

function jautils.square(num)
    return num * num
end

function jautils.distanceSquared(x1, y1, x2, y2)
    local diffX, diffY = x2 - x1, y2 - y1
    return diffX * diffX + diffY * diffY
end

function jautils.distance(x1, y1, x2, y2)
    return math.sqrt(jautils.distanceSquared(x1, y1, x2, y2))
end

function jautils.midpoint(x1, y1, x2, y2)
    return (x1 + x2) / 2, (y1 + y2) / 2
end

function jautils.degreeToRadians(degree)
    return degree * jautils.radian
end

function jautils.angleToVector(angleRadians, length)
	return math.cos(angleRadians) * length, math.sin(angleRadians) * length
end

function jautils.drawArrow(x1, y1, x2, y2, len, angle)
	love.graphics.line(x1, y1, x2, y2)
	local a = math.atan2(y1 - y2, x1 - x2)
	love.graphics.line(x2, y2, x2 + len * math.cos(a + angle), y2 + len * math.sin(a + angle))
	love.graphics.line(x2, y2, x2 + len * math.cos(a - angle), y2 + len * math.sin(a - angle))
end

function jautils.getArrowSprites(x1, y1, x2, y2, len, angle, thickness)
    local a = math.atan2(y1 - y2, x1 - x2)

    return {
        drawableLineStruct.fromPoints({x1, y1, x2, y2}, jautils.colorWhite, thickness),
        drawableLineStruct.fromPoints({x2, y2, x2 + len * math.cos(a + angle), y2 + len * math.sin(a + angle)}, jautils.colorWhite, thickness),
        drawableLineStruct.fromPoints({x2, y2, x2 + len * math.cos(a - angle), y2 + len * math.sin(a - angle)}, jautils.colorWhite, thickness),
    }
end

--[[
    COLORS
]]
function jautils.rainbowifyAll(room, sprites)
    for _,sprite in ipairs(sprites) do
        sprite:setColor(jautils.getRainbowHue(room, sprite.x, sprite.y))
    end
end

function jautils.getRainbowHue(room, x, y)
    local rx,ry = x + room.x, y + room.y
    local posLength = math.sqrt((rx * rx) + (ry * ry)) % 280 / 280

    return { utils.hsvToRgb(0.4 + jautils.yoyo(posLength) * 0.4, 0.4, 0.9) }
end

--TODO: pretty sure lonn supports rgba at this point, making this useless
function jautils.parseHexColor(color)
    if #color == 6 then
        local success, r, g, b = utils.parseHexColor(color)
        return success, r, g, b, 1
    elseif #color == 8 then
        color := match("^#?([0-9a-fA-F]+)$")

        if color then
            local number = tonumber(color, 16)
            local r, g, b, a = math.floor(number / 256^3) % 256, math.floor(number / 256^2) % 256, math.floor(number / 256) % 256, math.floor(number) % 256

            return true, r / 255, g / 255, b / 255, a / 255
        end
    end

    return false, 0, 0, 0
end

--TODO: pretty sure lonn supports rgba at this point, making this useless
function jautils.getColor(color)
    local colorType = type(color)

    if colorType == "string" then
        -- Check XNA colors, otherwise parse as hex color
        if xnaColors[color] then
            return xnaColors[color]

        else
            local success, r, g, b, a = jautils.parseHexColor(color)

            if success then
                return {r, g, b, a}
            end
            print("Invalid hex color " .. color)
            return success
        end

    elseif colorType == "table" and (#color == 3 or #color == 4) then
        return color
    end
end

function jautils.multColor(color, alpha)
    local c = jautils.getColor(color)

    return {c[1] * alpha, c[2] * alpha, c[3] * alpha, (c[4] or 1) * alpha }
end

return jautils