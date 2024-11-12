local utils = require("utils")
local mods = require("mods")
local jautils = mods.requireFromPlugin("libraries.jautils")
local frostSettings = mods.requireFromPlugin("libraries.settings")
local bloomSprite = mods.requireFromPlugin("libraries.bloomSprite")
local drawableSpriteStruct = require("structs.drawable_sprite")

local fallbackbg = "danger/FrostHelper/icecrystal/bg"
local fallback = mods.internalModContent .. "/missing_image"

local spinner = {}

spinner.name = "FrostHelper/IceSpinner"
spinner.depth = -8500

jautils.createPlacementsPreserveOrder(spinner, "custom_spinner", {
    { "directory", "danger/crystal>_white", "FrostHelper.texturePath", {
        baseFolder = "danger",
        pattern = "^(danger/.*)/fg(.-)%d+$",
        captureConverter = function(dir, subdir)
            return dir .. ">" .. subdir
        end,
        displayConverter = function(dir, subdir)
            local humanizedDir = utils.humanizeVariableName(string.match(dir, "^.*/(.*/hot)$") or string.match(dir, "^.*/(.*)$") or dir)
            if subdir and #subdir > 0 then
                return humanizedDir .. " (" .. utils.humanizeVariableName(subdir) .. ")"
            end

            return humanizedDir
        end,
        vanillaSprites = { "danger/crystal/fg_white00", "danger/crystal/fg_red00", "danger/crystal/fg_blue00", "danger/crystal/fg_purple00" },
        langDir = "customSpinner",
    }},
    { "spritePathSuffix", "" },
    { "tint", "ffffff", "color" },
    { "borderColor", "000000", "color" },
    { "destroyColor", "b0eaff", "color" },
    { "bloomAlpha", 0.0 },
    { "bloomRadius", 0.0 },
    { "debrisCount", 8, "integer" },
    { "attachGroup", -1, "FrostHelper.attachGroup" },
    { "attachToSolid", false },
    { "dashThrough", false },
    { "rainbow", false },
    { "collidable", true },
    { "drawOutline", true },
    { "singleFGImage", false }
})

function spinner.ignoredFields(entity)
    if string.find(entity.directory or "", ">") then
        return { "_name", "_id", "originX", "originY", "spritePathSuffix" }
    end

    return { "_name", "_id", "originX", "originY" }
end

local function createConnectorsForSpinner(room, entity, baseBGSprite)
    local sprites = {}

    local name = entity._name
    local attachGroup = entity.attachGroup
    local attachToSolid = entity.attachToSolid or false
    local x, y = entity.x, entity.y

    for _, e2 in ipairs(room.entities) do
        if e2 == entity then break end

        if e2._name == name and e2.attachGroup == attachGroup and e2.attachToSolid == attachToSolid then
            local e2x, e2y = e2.x, e2.y

            if jautils.distanceSquared(x, y, e2x, e2y) < 576 then
                local connector = jautils.copyTexture(baseBGSprite,
                    math.floor((x + e2x) / 2),
                    math.floor((y + e2y) / 2),
                false)

                connector.depth = -8499
                table.insert(sprites, connector)
            end
        end
    end

    return sprites
end

local pathCache = {}

local function getSpriteCache(entity)
    local d = entity.directory or ""
    local subdir = nil
    local cacheKey = nil

    local subDirIdx = string.find(d, ">")
    if subDirIdx then
        cacheKey = d
        subdir = string.sub(d, subDirIdx + 1)
        d = string.sub(d, 1, subDirIdx - 1)
    else
        subdir = entity.spritePathSuffix or ""
        cacheKey = d .. ">" .. subdir
    end

    local cache = pathCache[cacheKey]
    if not pathCache[cacheKey] then
        cache = {
            jautils.getAtlasSubtextures(d .. "/fg" .. subdir, fallback),
            jautils.getAtlasSubtextures(d .. "/bg" .. subdir, fallback)
        }

        pathCache[cacheKey] = cache
    end

    return cache
end

function spinner.associatedMods(entity)
    local cache = getSpriteCache(entity)

    return { "FrostHelper", unpack(jautils.associatedModsFromSprite(cache[2][1])) }
end

function spinner.sprite(room, entity)
    local color = utils.getColor(entity.tint or "ffffff")
    local cache = getSpriteCache(entity)

    utils.setSimpleCoordinateSeed(entity.x, entity.y)

    local sprites
    if frostSettings.spinnersConnect() then
        local baseSprite = drawableSpriteStruct.fromTexture(cache[2][math.random(#cache[2])], entity)
        baseSprite:setColor(color)
        sprites = createConnectorsForSpinner(room, entity, baseSprite)
    else
        sprites = {}
    end

    local fgSprite = drawableSpriteStruct.fromTexture(cache[1][math.random(#cache[1])], entity)
    fgSprite:setColor(color)
    table.insert(sprites, fgSprite)

    if entity.rainbow then
        jautils.rainbowifyAll(room, sprites)
    end

    local drawBorder = frostSettings.spinnerBorder() and (entity.drawOutline ~= false)
    if drawBorder then
        sprites = jautils.getBordersForAll(sprites, entity.borderColor)
    end

    if frostSettings.spinnerBloom() and entity.bloomAlpha and entity.bloomAlpha > 0 then
        table.insert(sprites, bloomSprite.getSprite(entity, entity.bloomAlpha, entity.bloomRadius or 1))
    end

    return sprites
end

function spinner.selection(room, entity)
    return utils.rectangle(entity.x - 8, entity.y - 8, 16, 16)
end

return spinner