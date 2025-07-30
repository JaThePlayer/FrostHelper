local controllerEntity = require("mods").requireFromPlugin("libraries.controllerEntity")
local fakeTilesHelper = require("helpers.fake_tiles")

local controller = controllerEntity.createHandler("FrostHelper/RainbowTilesetController", {
    name = "normal",
    data = {
        tilesets = "3",
        bg = false,
        includeDebris = true,
    },
}, false, "editor/FrostHelper/RainbowTilesetController")

controller.fieldInformation = function(entity) return {
    tilesets = {
        fieldType = "list",
        elementOptions = {
            options = fakeTilesHelper.getTilesOptions(entity.bg and "tilesBg" or "tilesFg"),
            editable = false
        },
        elementSeparator = ",",
        elementDefault = "3",
        minimumElements = 1,
    },
} end

return controller