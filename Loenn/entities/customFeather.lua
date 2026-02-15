---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local fallback = "objects/flyFeather/idle00"

---@type EntityHandler<UnknownEntity>
local feather = {}

feather.name = "FrostHelper/CustomFeather"
feather.depth = 0

local aimInvertions = {
    "None",
    "InvertX",
    "InvertY",
    "InvertBoth",
}

jautils.createPlacementsPreserveOrder(feather, "normal", {
    { "flyColor", "ffd65c", "color" },
    { "spriteColor", "ffffff", "color" },
    { "flyTime", 2.0 },
    { "respawnTime", 3.0 },
    { "maxSpeed", 190.0 },
    { "lowSpeed", 140.0 },
    { "neutralSpeed", 91.0 },
    { "spritePath", "objects/flyFeather/" },
    { "hitbox", "R,20,20,-10,-10", "FrostHelper.collider" },
    { "invertAim", "None", aimInvertions },
    { "version", 1, jautils.fields.integer{}, nil, { hidden = true } },
    { "shielded", false },
    { "singleUse", false },
    { "refillStamina", true },
    { "refillDashes", true },
})

function feather.sprite(room, entity)
    return jautils.union(
        jautils.getCustomSprite(entity, "spritePath", "idle00", fallback, "spriteColor"),
        entity.shielded and jautils.getCircleSprite(entity.x, entity.y, 12) or nil
    )
end

function feather.selection(room, entity)
    return utils.rectangle(entity.x - 12, entity.y - 12, 24, 24)
end

return feather