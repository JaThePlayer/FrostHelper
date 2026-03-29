local utils = require("utils")
local mods = require("mods")
---@module 'jautils'
local jautils = mods.requireFromPlugin("libraries.jautils")
---@module 'tracker'
local tracker = mods.requireFromPlugin("libraries.tracker")
---@module 'rainbowHelper'
local rainbowHelper = mods.requireFromPlugin("libraries.rainbowHelper")

local frostSettings = mods.requireFromPlugin("libraries.settings")
local bloomSprite = mods.requireFromPlugin("libraries.bloomSprite")
local drawableSpriteStruct = require("structs.drawable_sprite")

local fallback = mods.internalModContent .. "/missing_image"

---@type EntityHandler<UnknownEntity>
local spinner = {
    name = "FrostHelper/IceSpinner"
}
---@type EntityHandler<UnknownEntity>
local triggerSpinner = {
    name = "FrostHelper/TriggerSpinner"
}

local collisionModes = {
    "Kill",
    "PassThrough",
    "Shatter",
    "ShatterGroup",
}
local collisionModesForHoldables = {
    -- "Kill", - No way to consistently kill holdables
    "PassThrough",
    "Shatter",
    "ShatterGroup",
}

local collisionModesForHoldables_Unactivated = {
    -- "Kill", - No way to consistently kill holdables
    "PassThrough",
    "Activate",
    "Shatter",
    "ShatterGroup",
}

function spinner.depth(room, entity)
    return entity.depth or -8500
end
triggerSpinner.depth = spinner.depth

jautils.createPlacementsPreserveOrder(spinner, "custom_spinner", {
    { "directory", "danger/crystal>_white", jautils.fields.spinnerDirectory { } },
    { "tint", "ffffff", "color" },
    { "borderColor", "000000", "color" },
    { "destroyColor", "b0eaff", "color" },
    { "bloomAlpha", 0.0 },
    { "bloomRadius", 0.0 },
    { "debrisCount", 8, jautils.fields.nonNegativeInteger { } },
    { "attachGroup", -1, "FrostHelper.attachGroup" },
    { "hitbox", "C,6,0,0;R,16,4,-8,-3", "FrostHelper.collider"},
    { "scale", 1 },
    { "imageScale", 1 },
    { "depth", -8500, "depth" },
    { "onHoldable", "PassThrough", collisionModesForHoldables },
    { "onPlayer", "Kill", collisionModes },
    { "dashThrough", "Kill", collisionModes }, -- Dash Through needs to be last, because it used to be a bool, and it will break the layout if its not last and uses the old bool value.
    { "attachToSolid", false },
    { "rainbow", false },
    { "collidable", true },
    { "drawOutline", true },
    { "singleFGImage", false }
})

jautils.createPlacementsPreserveOrder(triggerSpinner, "default", {
    { "directory", "danger/FrostHelper/triggerSpinner>_off!", jautils.fields.spinnerDirectory { } },
    { "onDirectory", "danger/FrostHelper/triggerSpinner>_on!", jautils.fields.spinnerDirectory { } },
    { "delay", 0.3 },
    { "hitbox", "C,6,0,0;R,16,4,-8,-3", "FrostHelper.collider"},
    { "tint", "ffffff", "color" },
    { "borderColor", "000000", "color" },
    { "bloomAlpha", 0.0 },
    { "bloomRadius", 0.0 },
    { "destroyColor", "b0eaff", "color" },
    { "debrisCount", 8, "integer" },
    { "attachGroup", -1, "FrostHelper.attachGroup" },
    { "scale", 1 },
    { "imageScale", 1 },
    { "depth", -8500, "depth" },
    { "onHoldable", "PassThrough", collisionModesForHoldables },
    { "unactivatedOnHoldable", "PassThrough", collisionModesForHoldables_Unactivated },
    { "onPlayer", "Kill", collisionModes },
    { "dashThrough", "Kill", collisionModes }, -- Dash Through needs to be last, because it used to be a bool, and it will break the layout if its not last and uses the old bool value.
    { "attachToSolid", false },
    { "rainbow", false },
    { "singleFGImage", false }
})

function spinner.ignoredFields(entity)
    if string.find(entity.directory or "", ">") then
        return { "_name", "_id", "originX", "originY", "spritePathSuffix" }
    end

    return { "_name", "_id", "originX", "originY" }
end

local pathCache = {}

local function getSpriteCache(entity)
    local d = entity.directory or ""
    local spritePathSuffix = entity.spritePathSuffix or ""
    local dirCache = pathCache[d]
    if not dirCache then
        pathCache[d] = {}
        dirCache = pathCache[d]
    end

    local cache = pathCache[spritePathSuffix]
    if cache then
        return cache
    end

    local unanimated = string.match(d, "^(.-)!$")
    d = unanimated or d

    local subDirIdx = string.find(d, ">")
    local subdir = spritePathSuffix
    if subDirIdx then
        subdir = string.sub(d, subDirIdx + 1)
        d = string.sub(d, 1, subDirIdx - 1)
    end

    if unanimated then
        d = d .. "/00"
    end

    cache = {
        jautils.getAtlasSubtextures(d .. "/fg" .. subdir, fallback),
        jautils.getAtlasSubtextures(d .. "/bg" .. subdir, fallback),
        nil, -- width
        nil, -- associatedMods
    }

    cache[3] = drawableSpriteStruct.fromTexture(cache[1][1]).meta.realWidth

    pathCache[spritePathSuffix] = cache
    return cache
end

local function createConnectorsForSpinner(room, entity, baseBGSprite, cache)
    local sprites = {}

    local name = entity._name
    local attachGroup = entity.attachGroup
    local attachToSolid = entity.attachToSolid or false
    local x, y = entity.x, entity.y
    local imageScale = entity.imageScale or 1
    local id = entity._id

    local s = cache[3]

    for _, e2 in ipairs(tracker.getAll(room, name)) do
        if (e2._id or -1) <= id then goto continue end

        if e2.attachGroup == attachGroup and e2.attachToSolid == attachToSolid then
            local e2x, e2y = e2.x, e2.y
            local scaleSum = (imageScale+(e2.imageScale or 1))
            if jautils.distanceSquared(x, y, e2x, e2y) < s*getSpriteCache(e2)[3]*scaleSum*scaleSum/4 then
                local connector = jautils.copyTexture(baseBGSprite,
                    math.floor((x + e2x) / 2),
                    math.floor((y + e2y) / 2),
                false)
                connector:setScale(imageScale, imageScale)

                connector.depth = -8499
                table.insert(sprites, connector)
            end
        end

        ::continue::
    end

    return sprites
end

function spinner.associatedMods(entity)
    local cache = getSpriteCache(entity)
    if cache[4] then
        return cache[4]
    end

    local fgSprite = cache[2][1]
    if not fgSprite then
        cache[4] = { "FrostHelper" }
        return cache[4]
    end

    local associated = jautils.associatedModsFromSprite(fgSprite)
    if associated[1] == "FrostHelper" then
        cache[4] = associated
        return cache[4]
    end

    cache[4] = { "FrostHelper", unpack(associated) }
    return cache[4]
end
triggerSpinner.associatedMods = spinner.associatedMods

function spinner.sprite(room, entity)
    local color = utils.getColor(entity.tint or "ffffff") or "ffffff"
    local cache = getSpriteCache(entity)
    local imageScale = entity.imageScale or 1

    utils.setSimpleCoordinateSeed(entity.x, entity.y)

    local fgSprite = drawableSpriteStruct.fromTexture(cache[1][math.random(#cache[1])], entity)
    fgSprite:setColor(color)
    fgSprite:setScale(imageScale,imageScale)

    local sprites
    if frostSettings.spinnersConnect() then
        local baseSprite = drawableSpriteStruct.fromTexture(cache[2][math.random(#cache[2])], entity)
        baseSprite:setColor(color)
        sprites = createConnectorsForSpinner(room, entity, baseSprite, cache)
    else
        sprites = {}
    end

    table.insert(sprites, fgSprite)

    if entity.rainbow then
        rainbowHelper.rainbowifyAll(room, sprites)
    end

    local drawBorder = frostSettings.spinnerBorder() and (entity.drawOutline ~= false)
    if drawBorder then
        sprites = jautils.getBordersForAll(sprites, entity.borderColor)
    end

    if frostSettings.spinnerBloom() then
        local bloomAlpha = entity.bloomAlpha or 0
        if bloomAlpha > 0 then
            table.insert(sprites, bloomSprite.getSprite(entity, entity.bloomAlpha, entity.bloomRadius or 1))
        end
    end

    return sprites
end
triggerSpinner.sprite = spinner.sprite

function spinner.selection(room, entity)
    local scale = entity.scale or 1
    return utils.rectangle(entity.x - 8 * scale, entity.y - 8 * scale, 16 * scale, 16 * scale)
end
triggerSpinner.selection = spinner.selection

spinner.placements[1].ext_group = "FrostHelper/IceSpinner"
triggerSpinner.placements[1].ext_group = "FrostHelper/IceSpinner"

return {
    spinner,
    triggerSpinner
}