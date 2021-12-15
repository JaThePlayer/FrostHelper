--- allows for easy creation of "controller" style entities that only draw a single sprite

local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local controllerEntity = {}

function controllerEntity.createHandler(name, placements, placementsUseJaUtils, spritePath)
    local handler = {
        name = name,
        depth = 8990,
        texture = spritePath,
    }

    if placementsUseJaUtils then
        jautils.createPlacementsPreserveOrder(handler, "normal", placements)
    else
        handler.placements = placements
    end

    return handler
end

return controllerEntity