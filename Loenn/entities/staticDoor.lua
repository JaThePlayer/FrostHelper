local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")

local staticDoor = {}

local textures = {"wood", "metal"}
local textureOptions = {}

for _, texture in ipairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

staticDoor.name = "FrostHelper/StaticDoor"
staticDoor.depth = 8998
staticDoor.justification = {0.5, 1.0}

jautils.createPlacementsPreserveOrder(staticDoor, "default", {
    { "type", "wood", "editableDropdown", textureOptions },
    { "openSfx", "" },
    { "closeSfx", "" },
    { "lightOccludeAlpha", 1.0 }
})


function staticDoor.texture(room, entity)
    local variant = entity["type"]

    if variant == "wood" then
        return "objects/door/door00"

    else
        return "objects/door/metaldoor00"
    end
end

return staticDoor