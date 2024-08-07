local celesteEnums = require("consts.celeste_enums")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local barrier = {
    name = "FrostHelper/CustomInvisibleBarrier",
    fillColor = {0.4, 0.4, 0.4, 0.8},
    borderColor = {0.4, 0.4, 0.4, 0.8},
}

jautils.createPlacementsPreserveOrder(barrier, "default", {
    { "soundIndex", 33, "editableDropdown", celesteEnums.tileset_sound_ids },
    { "canClimb", false },
}, true)

return barrier