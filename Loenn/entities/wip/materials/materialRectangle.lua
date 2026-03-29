---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

if not jautils.devMode then
    return
end

local matRectangle = {
    name = "FrostHelper/MaterialRectangle",
    fillColor = {0.4, 0.4, 0.4, 0.8},
    borderColor = {0.4, 0.4, 0.4, 0.8},
}

jautils.createPlacementsPreserveOrder(matRectangle, "default", {
    { "name", "" },
    { "depth", 0, jautils.fields.depth {} },
    { "tint", "ffffff", jautils.fields.color {} },
    --{ "soundIndex", 33, "editableDropdown", celesteEnums.tileset_sound_ids },
    --{ "canClimb", false },
}, true)

return matRectangle