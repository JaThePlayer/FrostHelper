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
    { "attachGroup", -1, "FrostHelper.attachGroup" }
})

jautils.addPlacement(spinner, "rainbowTexture", {
    {"directory", "danger/crystal" },
	{"spritePathSuffix", "_white" },
})

local function createConnectorsForSpinner(room, entity, baseBGSprite)
    local sprites = {}

    for i = 1, #room.entities, 1 do
        local e2 = room.entities[i]

        if e2 == entity then break end

        if e2._name == entity._name and e2.attachGroup == entity.attachGroup and e2.attachToSolid == entity.attachToSolid and jautils.distanceSquared(entity.x, entity.y, e2.x, e2.y) < 576 then
            local connector = jautils.copyTexture(baseBGSprite,
                math.floor((entity.x + e2.x) / 2),
                math.floor((entity.y + e2.y) / 2),
                false)
            connector.depth = -8499 --  1---8499
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

    local drawBorder = frostSettings.spinnerBorder() and (entity.drawOutline == nil and true or entity.drawOutline)
    if drawBorder then
        sprites = jautils.getBordersForAll(sprites, entity.borderColor)
    end

    if entity.bloomAlpha and entity.bloomAlpha > 0 and frostSettings.spinnerBloom() then
        table.insert(sprites, bloomSprite.getSprite(entity, entity.bloomAlpha, entity.bloomRadius or 1))
    end

    return sprites
end

function spinner.selection(room, entity)
    return utils.rectangle(entity.x - 8, entity.y - 8, 16, 16)
end

return spinner