--- allows for easy creation of "indicator" style entities.
--- This has the placements for a regular Frost Helper indicator

local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controllerEntity = {}

function controllerEntity.createHandler(name, placementAddon)
    local handler = {
        name = name,
    }

    local placements = {
        { "spritePath", "objects/FrostHelper/pufferIndicator" },
        { "color", "ffffff", "color" },
        { "outlineColor", "000000", "color" }
    }

    if placementAddon then
        for _, v in ipairs(placementAddon) do
            table.insert(placements, v)
        end
    end

    jautils.createPlacementsPreserveOrder(handler, "default", placements)

    handler.sprite = function(room, entity)
        return jautils.getOutlinedSpriteFromPath(entity, entity.spritePath, entity.color or "ffffff", entity.outlineColor or "000000")
    end

    return handler
end

return controllerEntity