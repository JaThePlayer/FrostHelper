local utils = require("utils")

return {
    name = "FrostHelper/DecalContainer",
    depth = -math.huge,
    placements = {
        name = "default",
        data = {
            chunkSizeInTiles = 64
        }
    },
    fieldInformation = {
        chunkSizeInTiles = {
            fieldType = "integer",
            minimumValue = 1
        }
    },
    rectangle = function (room, entity, viewport)
        return utils.rectangle(entity.x - 4, entity.y - 4, 8, 8)
    end,
    borderColor = { 1, 1, 1 },
    fillColor = { 1, 1, 1, 0.3}
}