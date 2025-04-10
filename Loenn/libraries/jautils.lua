--[[

---- READ THIS ----

jautils (and all other libraries in frost helper) are **NOT** a public api for use in other mods.

If you need something from here, contact me or copy them to your own mod.
]]

local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangleStruct = require("structs.drawable_rectangle")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableLineStruct = require("structs.drawable_line")
local utils = require("utils")
local xnaColors = require("consts.xna_colors")
local mods = require("mods")
local rainbowHelper = mods.requireFromPlugin("libraries.rainbowHelper")
local compat = mods.requireFromPlugin("libraries.compat")
local mapScanHelper = mods.requireFromPlugin("libraries.mapScanHelper")
local easings = mods.requireFromPlugin("libraries.easings")
local fakeTilesHelper = require("helpers.fake_tiles")

local celesteDepths = --require("consts.object_depths")
{
    ["BG Terrain (10000)"] = 10000,
    ["BG Mirrors (9500)"] = 9500,
    ["BG Decals (9000)"] = 9000,
    ["BG Particles (8000)"] = 8000,
    ["Solids Below (5000)"] = 5000,
    ["Below (2000)"] = 2000,
    ["NPCs (1000)"] = 1000,
    ["Theo Crystal (100)"] = 100,
    ["Player (0)"] = 0,
    ["Dust (-50)"] = -50,
    ["Pickups (-100)"] = -100,
    ["Seeker (-200)"] = -200,
    ["Particles (-8000)"] = -8000,
    ["Above (-8500)"] = -8500,
    ["Solids (-9000)"] = -9000,
    ["FG Terrain (-10000)"] = -10000,
    ["FG Decals (-10500)"] = -10500,
    ["Dream Blocks (-11000)"] = -11000,
    ["Crystal Spinners (-11500)"] = -11500,
    ["Player Dream Dashing (-12000)"] = -12000,
    ["Enemy (-12500)"] = -12500,
    ["Fake Walls (-13000)"] = -13000,
    ["FG Particles (-50000)"] = -50000,
    ["Top (-1000000)"] = -1000000,
    ["Formation Sequences (-2000000)"] = -2000000,
}

---@alias color string | table<integer, number>
---@alias sprite table

local jautils = {}

jautils.windingOrders = {
    "Clockwise", "CounterClockwise", "Auto"
}

--[[
    Compatibility
]]
jautils.inLonn = compat.inLonn
jautils.inRysy = compat.inRysy
jautils.inSnowberry = compat.inSnowberry

jautils.easings = "FrostHelper.easing"
jautils.tweenModes = mods.requireFromPlugin("libraries.tweenModes")

---Tries to require a file, returns nil if it doesn't exist
jautils.tryRequire = utils.tryrequire
    and function (lib)
            local success, ret = utils.tryrequire(lib, false)
            if success then
                return ret
            else
                return nil
            end
        end
    or function (lib)
            -- Fallback if utils.tryrequire doesn't exist (snowberry)
            local success, ret = pcall(require, lib)
            if success then
                return ret
            else
                return nil
            end
        end

local atlases = jautils.tryRequire("atlases")

--[[
    UTILS
]]

---Creates a new table with the given size, if possible on the current lua runtime.
---@param narr number number of integer keys
---@param nhash number number of hash keys
---@return table
jautils.newTable = jautils.tryRequire("table.new") or function (narr, nhash)
    return {}
end

---Returns a table which contains all of the elements of all passed tables.
---Argument tables that have a _type field will be added to the returned table directly instead of being looped over
---@param ... table
---@return table
function jautils.union(...)
    local union = {}

    for _, value in ipairs({...}) do
        if not value then
        elseif value._type then -- specific to lonn
            table.insert(union, value)
        else
            jautils.addAll(union, value)
        end
    end

    return union
end

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

local getAllSids = function ()
    return {}
end

-- Snowberry hard-crashes on boot if libraries crash, and the current public build doesn't implement `entities`.
local entities = jautils.tryRequire("entities")
if entities and entities.registeredEntities then
    getAllSids = function ()
        local ret = {}
        local amt = 0
        for k,v in pairs(entities.registeredEntities) do
            table.insert(ret, k)
            amt = amt + 1
        end

        table.sort(ret)

        return ret
    end
end

local easingDict = {}
for _, ease in ipairs(easings) do
    easingDict[ease] = true
end


jautils.counterTimeUnits = {
    "Milliseconds",
    "Seconds",
    "Minutes",
    "Hours"
}

jautils.counterOperations = {
    "Equal",
    "NotEqual",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
}

jautils.counterOperationToMathExpr = {
    ["Equal"] = "==",
    ["NotEqual"] = "!=",
    ["GreaterThan"] = ">",
    ["LessThan"] = "<",
    ["GreaterThanOrEqual"] = ">=",
    ["LessThanOrEqual"] = "<=",
}

jautils.spinnerDirectoryFieldData = {
    baseFolder = "danger",
    pattern = "^(danger/.*)/fg(.-)%d+$",
    captureConverter = function(dir, subdir)
        local animationless = string.match(dir, "(.-)/%d%d$")
        if animationless then
            return animationless .. ">" .. subdir .. "!"
        end

        return dir .. ">" .. subdir
    end,
    displayConverter = function(dir, subdir)
        dir = string.match(dir, "(.-)/%d%d$") or dir

        local humanizedDir = utils.humanizeVariableName(string.match(dir, "^.*/(.*/hot)$") or string.match(dir, "^.*/(.*)$") or dir)
        if subdir and #subdir > 0 then
            return humanizedDir .. " (" .. utils.humanizeVariableName(subdir) .. ")"
        end

        return humanizedDir
    end,
    vanillaSprites = { "danger/crystal/fg_white00", "danger/crystal/fg_red00", "danger/crystal/fg_blue00", "danger/crystal/fg_purple00" },
    langDir = "customSpinner",
}

function jautils.counterConditionToString(counter, operation, target)
    return string.format("%s %s %s", counter, jautils.counterOperationToMathExpr[operation], target)
end

function jautils.typesListFieldInfo() return {
    fieldType = "list",
    elementSeparator = ",",
    elementDefault = "",
    elementOptions = {
        options = getAllSids(),
        searchable = true,
    },
} end

jautils.fieldTypeOverrides = {
    color =  function (data) return {
        fieldType = "color",
        allowXNAColors = true,
        useAlpha = true,
    } end,
    colorOrEmpty =  function (data) return {
        fieldType = "color",
        allowXNAColors = true,
        useAlpha = true,
        allowEmpty = true,
    } end,
    colorList = function (data) return {
        fieldType = "list",
        elementOptions = {
            fieldType = "color",
            allowXNAColors = true,
        },
        elementSeparator = ",",
        elementDefault = "ffffff",
        minimumElements = 1,
    } end,
    list = function (data) return {
        fieldType = "list",
        elementOptions = data and data.elementOptions,
        elementSeparator = data and data.elementSeparator or ",",
        elementDefault = data and data.elementDefault,
    } end,
    typesList = jautils.typesListFieldInfo,
    editableDropdown = function(data)
        return {
            options = data,
            editable = true,
            -- Searchable dropdowns mapping strings to ints crash lonn, so let's not do that
            --searchable = true,
        }
    end,
    dropdown = function(data)
        return {
            options = data,
            editable = false,
            searchable = true,
        }
    end,
    dropdown_int = function(data)
        return {
            fieldType = "integer",
            options = data,
            editable = false,
            searchable = true,
        }
    end,
    depth = function (data)
        return {
            options = celesteDepths,
            editable = true,
            fieldType = "integer",
            --searchable = true,
        }
    end,
    depthOrEmpty = function (data)
        return {
            options = celesteDepths,
            editable = true,
            fieldType = "integer",
            allowEmpty = true,
            --searchable = true,
        }
    end,
    sessionCounter = function (data)
        return {
            options = mapScanHelper.findAllSessionCounters(),
            editable = true,
            searchable = true,
        }
    end,
    sessionSlider = function (data)
        return {}
    end,
    cloudTag = function (data)
        return {
            options = mapScanHelper.findAllCloudTagsInRoom(),
            editable = true,
            searchable = true,
        }
    end,
    ["FrostHelper.stylegroundTag"] = function (data)
        return {
            options = mapScanHelper.findAllStylegroundTagsInMap(),
            editable = false,
            searchable = true,
        }
    end,
    tiletype = function (data)
        return {
            options = fakeTilesHelper.getTilesOptions(data.layer),
            editable = false
        }
    end,
    ["FrostHelper.easing"] = function (data)
        return {
            options = easings,
            editable = true,
            searchable = true,
            validator = function(v)
                if not v then
                    return true
                end

                if easingDict[v] ~= nil then
                    return easingDict[v]
                end

                -- Session Expression validation doesn't exist yet
                if string.find(v, "^expr:") then
                    easingDict[v] = true
                    return true
                end

                -- Frost Helper allows using Lua functions for easings, we're in lua so we can check the syntax
                local code = string.format("return function(p)%s %s end", string.find(v, "return", 1, true) and "" or " return", v)

                local success, errorMsg = loadstring(code)
                if success then
                    easingDict[v] = true
                    return true
                else
                    print(errorMsg)
                    easingDict[v] = false
                end

                return false
            end
        }
    end,
    ["FrostHelper.texturePath"] = function (data)
        if not compat.inSnowberry
           -- Way too laggy for now
           and not (compat.inLonn and (data.baseFolder == "objects" or data.baseFolder == "objects/switchgate")) then
            data.fieldType = "FrostHelper.texturePath"
            return data
        end

        if data.fallback then
            return {
                options = data.fallback,
                editable = true,
            }
        end
    end,
    ["FrostHelper.collider"] = function (data)
        return {
            fieldType = "list",
            elementSeparator = ";",
        }
    end,
    ["FrostHelper.condition"] = function (data)
        -- Fictional field type, for future-proofing
        return nil
    end,
    ["FrostHelper.randomMode"] = function (data)
        return {
            options = {
                "SessionTime",
                "RoomSeed",
                "FullRandom",
                "Custom",
            },
        }
    end,
}

local fieldTypesWhichNeedLiveUpdate = {
    typesList = true,
    sessionCounter = true,
    cloudTag = true,
    tiletype = true,
    ["FrostHelper.stylegroundTag"] = true,
}

local function createFieldInfoFromJaUtilsPlacement(placementData)
    local fieldInformation = {}
    for _,v in ipairs(placementData) do
        local fieldName, defaultValue, fieldType, fieldData = v[1], v[2], v[3], v[4]
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
                    if fieldType["fieldType"] then
                        -- we have a full field definition here, don't do anything about it
                        fieldInformation[fieldName] = fieldType
                    else
                        -- didn't define a type, treat it as a dropdown
                        fieldInformation[fieldName] = jautils.fieldTypeOverrides.dropdown(fieldType)
                    end
                else
                    -- if you just pass a string, treat it as the field type
                    local data = table.shallowcopy(fieldData or {})
                    data.fieldType = fieldType
                    fieldInformation[fieldName] = data
                end
            end
        end

        -- Provide the 'default' option
        if fieldInformation[fieldName] and type(fieldInformation[fieldName]) == "table" then
            fieldInformation[fieldName].default = defaultValue
        elseif not fieldInformation[fieldName] then
            fieldInformation[fieldName] = { default = defaultValue }
        end
    end

    return fieldInformation
end

function jautils.createPlacementsPreserveOrder(handler, placementName, placementData, appendSize)
    handler.placements = {{
        name = placementName,
        data = {}
    }}
    local fieldOrder = { "x", "y" }
    local hasAnyFieldInformation = false
    local needsLiveFieldInfoUpdate = false

    if appendSize then
        table.insert(placementData, 1, { "height", 16 })
        table.insert(placementData, 1, { "width", 16 })
    end

    for _,v in ipairs(placementData) do
        local fieldName, defaultValue, fieldType, fieldData = v[1], v[2], v[3], v[4]

        table.insert(fieldOrder, fieldName)
        handler.placements[1].data[fieldName] = defaultValue

        if fieldType then
            hasAnyFieldInformation = true
            if fieldTypesWhichNeedLiveUpdate[fieldType] then
                needsLiveFieldInfoUpdate = true
            end
        end
    end

    if needsLiveFieldInfoUpdate then
        handler.fieldInformation = function(entity)
            return createFieldInfoFromJaUtilsPlacement(placementData)
        end
    else
        handler.fieldInformation = createFieldInfoFromJaUtilsPlacement(placementData)
    end

    handler.fieldOrder = fieldOrder
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

local emptyTable = { }
if atlases and mods.getModMetadataFromPath and mods.getModNamesFromMetadata then
    local associatedModsFromSpriteCache = {}
    function jautils.associatedModsFromSprite(path)
        local gp = atlases.gameplay
        if not gp then
            return emptyTable
        end

        local cached = associatedModsFromSpriteCache[path]
        if cached then
            return cached
        end
    
        local sprite = gp[path]
        if (not sprite) or sprite.internalFile then
            associatedModsFromSpriteCache[path] = emptyTable
            return emptyTable
        end
    
    
        local filename = sprite.filename
        local modMetadata = mods.getModMetadataFromPath(filename)
        if not modMetadata then
            associatedModsFromSpriteCache[path] = emptyTable
            return emptyTable
        end

        local names = mods.getModNamesFromMetadata(modMetadata)
    
        associatedModsFromSpriteCache[path] = names
    
        return names
    end
else
    function jautils.associatedModsFromSprite(path)
        return emptyTable
    end
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
    color = color and utils.getColor(color) or {0, 0, 0, 1}

    local function get(xOffset,yOffset)
        local texture = drawableSpriteStruct.fromMeta(sprite.meta, sprite)
        texture.x += xOffset
        texture.y += yOffset
        texture.depth = sprite.depth and sprite.depth + 2 or nil -- fix preview depth
        texture.color = color

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
    if color == "00000000" then
        return sprites
    end

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

local spritePathCache = {}

function jautils.getSpritePathOrFallback(entity, customPath, fallback)
    local sprite = nil

    if customPath then
        local cacheKey = customPath
        local cached = spritePathCache[cacheKey]
        if cached then
            return cached, drawableSpriteStruct.fromTexture(cached, entity)
        end

        customPath = string.gsub(customPath, "//", "/")

        sprite = drawableSpriteStruct.fromTexture(customPath, entity)
        if sprite then
            spritePathCache[cacheKey] = customPath
            return customPath, sprite
        end

        -- Celeste will also try appending 00, let's try that
        customPath = customPath .. "00"
        sprite = drawableSpriteStruct.fromTexture(customPath, entity)
        if sprite then
            spritePathCache[cacheKey] = customPath
            return customPath, sprite
        end
    end

    return fallback, drawableSpriteStruct.fromTexture(fallback, entity)
end

function jautils.getCustomSpritePath(entity, spritePropertyName, spritePostfix, fallback)
    local sprite = nil

    local path = entity[spritePropertyName]
    if path then
        local fullPath = path .. (spritePostfix or "")

        return jautils.getSpritePathOrFallback(entity, fullPath, fallback)
    end

    return fallback, drawableSpriteStruct.fromTexture(fallback, entity)
end

local getAtlasSubtexturesCache = {}

function jautils.getAtlasSubtextures(path, fallback)
    if getAtlasSubtexturesCache[path] then
        return getAtlasSubtexturesCache[path]
    end

    local atlas = atlases and atlases.Gameplay
    if not atlas then
        local ret = { jautils.getSpritePathOrFallback({}, path, fallback) }
        getAtlasSubtexturesCache[path] = ret
        return ret
    end

    local paths = {}
    for i = 0, 99 do
        local idx = tostring(i)
        local frameId = path .. idx
        if i < 10 and not atlas[frameId] then
            idx = "0" .. tostring(i)
            frameId = path .. idx
        end

        if not atlas[frameId] then
            local ret = i == 0 and { (jautils.getSpritePathOrFallback({}, path, fallback)) } or paths
            getAtlasSubtexturesCache[path] = ret
            return ret
        end

        table.insert(paths, frameId)
    end

    getAtlasSubtexturesCache[path] = paths
    return paths
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
function jautils.getCustomBlockSprites(entity, spritePropertyName, spritePostfix, fallback, spriteTintPropertyName, ninePatchOptions, color, pos)
    pos = pos or entity
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
        local ninePatch = drawableNinePatch.fromTexture(baseTexture, ninePatchOptions or defaultNinePatchOptions, pos.x, pos.y, entity.width, entity.height)
        if color then
            ninePatch.color = color
        end
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

---Returns a sprite that renders a hollow ellipse
---@param x number
---@param y number
---@param rx number
---@param ry number
---@param color color|nil
---@return sprite
function jautils.getEllipseSprite(x, y, rx, ry, color)
    local segments = 24
    local slice = math.pi * 2 / segments

    local points = jautils.newTable(segments, 0)
    for angle = 0, math.pi * 2, slice do
        table.insert(points, math.cos(angle) * rx + x)
        table.insert(points, math.sin(angle) * ry + y)
    end

    return drawableLineStruct.fromPoints(points, color or jautils.colorWhite)
end

---Returns a sprite that renders a hollow circle
---@param x number
---@param y number
---@param r number
---@param color color|nil
---@return sprite
function jautils.getCircleSprite(x, y, r, color)
    return jautils.getEllipseSprite(x, y, r, r, color)
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

-- checks if two vectors are equal
function jautils.equalVec(ax, ay, bx, by)
    return ax == bx and ay == by
end

function jautils.lengthVec(x, y)
    return math.sqrt(x * x + y * y);
end

-- reimplementation of Calc.Approach(Vector2, Vector2, float)
function jautils.approachVec(fromX, fromY, targetX, targetY, maxMove)
    if (maxMove == 0 or (fromX == targetX and fromY == targetY)) then
        return fromX, fromY
    end

    local diffX, diffY = targetX - fromX, targetY - fromY
    if (jautils.lengthVec(diffX, diffY) < maxMove) then
        return targetX, targetY
    end

    local angleX, angleY = jautils.normalize(diffX, diffY)

    return fromX + (angleX * maxMove), fromY + (angleY * maxMove)
end

-- reimplementation of Calc.Approach(float, float, float)
function jautils.approach(from, to, maxMove)
    if from <= to then
        return math.min(from + maxMove, to)
    end

    return math.max(from - maxMove, to)
end

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

function jautils.getArrowSprites(x1, y1, x2, y2, len, angle, thickness, color)
    color = color or jautils.colorWhite
    local a = math.atan2(y1 - y2, x1 - x2)

    return {
        drawableLineStruct.fromPoints({x1, y1, x2, y2}, color, thickness),
        drawableLineStruct.fromPoints({x2, y2, x2 + len * math.cos(a + angle), y2 + len * math.sin(a + angle)}, color, thickness),
        drawableLineStruct.fromPoints({x2, y2, x2 + len * math.cos(a - angle), y2 + len * math.sin(a - angle)}, color, thickness),
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
    return rainbowHelper.getRainbowHue(room, x, y)
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

    --return {c[1] * alpha, c[2] * alpha, c[3] * alpha, (c[4] or 1) * alpha }
    return {c[1], c[2], c[3], (c[4] or 1) * alpha }
end

--[[
    Strings
]]

---Rounds a number to given amount of digits, and returns it as a string. If digits is not provided, it defaults to 3
---@param number number
---@param digits number|nil
---@return string
function jautils.roundedToString(number, digits)
    if not digits then
        return string.format("%.3f", number)
    end

    return string.format("%." .. tostring(digits) .. "f", number)
end

---Formats a flag for displaying, converting an inverted flag to !flagName
---@param flagName string
---@param inverted boolean
---@return string
function jautils.formatFlag(flagName, inverted)
    if inverted then
        return "!" .. flagName
    else
        return flagName
    end
end

--[[
    Lönn Extended support
]]

local triggers = require("triggers")

---Adds extended text support for the given trigger. Returns the handler itself
---@param triggerHandler table
---@param getter function<table>
---@return table triggerHandler
function jautils.addExtendedText(triggerHandler, getter)
    if not getter then return triggerHandler end

    triggerHandler.triggerText = function (room, trigger)
        local text = triggers.getDrawableDisplayText(trigger)
        local extText = getter(trigger)
        if extText then
            return string.format("%s\n(%s)", text, extText)
        end
        return text
    end

    return triggerHandler
end

--[[
    Rotations
]]

local function indexof(keyvaluetable, toFind)
    for key, val in pairs(keyvaluetable) do
        if val == toFind then
            return key
        end
    end
end

---Returns a handler function to implement entity.rotate(room, entity, direction), where the entity's _name field will get changed according to the rotations table
---@param rotations table { [0] = "upName", [1] = "rightName", [2] = "downName", [3] = "leftName" }
---@return function
function jautils.getNameRotationHandler(rotations)
    return function (room, entity, direction)
        local startIndex = indexof(rotations, entity._name)

        local realIndex = (startIndex + direction) % 4

        entity._name = rotations[realIndex]

        return true
    end
end

---Returns a handler function to implement entity.flip(room, entity, horizontal, vertical), where the entity's _name field will get changed according to the rotations table
---@param rotations table { [0] = "upName", [1] = "rightName", [2] = "downName", [3] = "leftName" }
---@return function
function jautils.getNameFlipHandler(rotations)
    return function (room, entity, horizontal, vertical)
        local startIndex = indexof(rotations, entity._name)

        if vertical then
            if startIndex == 0 or startIndex == 2 then
                entity._name = rotations[(startIndex + 2) % 4]
                return true
            end
        else
            if startIndex == 1 or startIndex == 3 then
                entity._name = rotations[(startIndex + 2) % 4]
                return true
            end
        end

        return false
    end
end

---Creates a legacy handler from the given handler. Legacy handlers have a different SID and no placements.
---This lets mappers still edit existing legacy entities, but not place new ones (without copy-pasting)
---@param handler table
---@param legacySid string
---@return table
function jautils.createLegacyHandler(handler, legacySid)
    local legacy = utils.deepcopy(handler)
    legacy.placements = nil
    legacy.name = legacySid

    return legacy
end

return jautils