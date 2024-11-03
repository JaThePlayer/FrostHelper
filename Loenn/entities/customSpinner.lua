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
    { "directory", "danger/crystal" },
    { "spritePathSuffix", "_white" },
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

function spinner.sprite(room, entity)
    local pathSuffix = entity.spritePathSuffix or ""
    local color = utils.getColor(entity.tint or "ffffff")

    local d = entity.directory or ""
    if not pathCache[d] then
        pathCache[d] = {}
    end

    local cache = pathCache[d][pathSuffix]
    if not cache then
        cache = {
            jautils.getCustomSpritePath(entity, "directory", "/fg" .. pathSuffix .. "03") or jautils.getCustomSpritePath(entity, "directory", "/fg" .. pathSuffix .. "00") or fallback,
            jautils.getCustomSpritePath(entity, "directory", "/bg" .. pathSuffix, fallback)
        }

        pathCache[d][pathSuffix] = cache
    end

    local sprites
    if frostSettings.spinnersConnect() then
        local baseSprite = drawableSpriteStruct.fromTexture(cache[2], entity)
        baseSprite:setColor(color)
        sprites = createConnectorsForSpinner(room, entity, baseSprite)
    else
        sprites = {}
    end

    local fgSprite = drawableSpriteStruct.fromTexture(cache[1], entity)
    fgSprite:setColor(color)
    table.insert(sprites, fgSprite)

    --local sprites = frostSettings.spinnersConnect()
    --    and createConnectorsForSpinner(room, entity, jautils.getCustomSprite(entity, "directory", "/bg" .. pathSuffix, fallbackbg))
    --    or {}

    --table.insert(sprites, jautils.getCustomSprite(entity, "directory", "/fg" .. pathSuffix .. "03", fallback))

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