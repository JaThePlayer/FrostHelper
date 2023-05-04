local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local frostSettings = require("mods").requireFromPlugin("libraries.settings")
local bloomSprite = require("mods").requireFromPlugin("libraries.bloomSprite")

local fallback = "danger/FrostHelper/icecrystal/fg03"
local fallbackbg = "danger/FrostHelper/icecrystal/bg"
local spinner = {}

spinner.name = "FrostHelper/IceSpinner"
spinner.depth = -8500

jautils.createPlacementsPreserveOrder(spinner, "custom_spinner", {
    { "tint", "ffffff", "color" },
    { "destroyColor", "b0eaff", "color" },
    { "borderColor", "000000", "color" },
    { "directory", "danger/FrostHelper/icecrystal" },
    { "spritePathSuffix", "" },
    { "attachToSolid", false },
    { "moveWithWind", false },
    { "dashThrough", false },
    { "rainbow", false },
    { "collidable", true },
    { "drawOutline", true },
    { "bloomAlpha", 0.0 },
    { "bloomRadius", 0.0 },
    { "debrisCount", 8, "integer" },
    { "attachGroup", -1, "FrostHelper.attachGroup" },
    { "singleFGImage", false }
})

jautils.addPlacement(spinner, "rainbowTexture", {
    {"directory", "danger/crystal" },
	{"spritePathSuffix", "_white" },
})

local function createConnectorsForSpinner(room, entity, baseBGSprite)
    local sprites = {}

    local name = entity._name
    local attachGroup = entity.attachGroup
    local attachToSolid = entity.attachToSolid
    local x, y = entity.x, entity.y

    for _, e2 in ipairs(room.entities) do
        if e2 == entity then break end

        if e2._name == name and e2.attachGroup == attachGroup and e2.attachToSolid == attachToSolid and jautils.distanceSquared(x, y, e2.x, e2.y) < 576 then
            local connector = jautils.copyTexture(baseBGSprite,
                math.floor((x + e2.x) / 2),
                math.floor((y + e2.y) / 2),
                false)
            connector.depth = -8499
            table.insert(sprites, connector)
        end
    end

    return sprites
end

function spinner.sprite(room, entity)
    local pathSuffix = entity.spritePathSuffix or ""

    local sprites = frostSettings.spinnersConnect()
        and createConnectorsForSpinner(room, entity, jautils.getCustomSprite(entity, "directory", "/bg" .. pathSuffix, fallbackbg))
        or {}

    table.insert(sprites, jautils.getCustomSprite(entity, "directory", "/fg" .. pathSuffix .. "03", fallback))

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