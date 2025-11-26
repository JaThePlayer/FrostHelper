--- allows for easy creation of "controller" style entities that only draw a single sprite

---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controllerEntity = {}

---@param name string
---@param placements PlacementInfo|PlacementInfo[]|JaUtilsPlacementData
---@param placementsUseJaUtils boolean
---@param spritePath string
---@return EntityHandler<UnknownEntity>
function controllerEntity.createHandler(name, placements, placementsUseJaUtils, spritePath)
    ---@type EntityHandler<UnknownEntity>
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