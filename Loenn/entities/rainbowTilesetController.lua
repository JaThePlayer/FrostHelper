local controllerEntity = require("mods").requireFromPlugin("libraries.controllerEntity")

return controllerEntity.createHandler("FrostHelper/RainbowTilesetController", {
    name = "normal",
    data = {
        tilesets = "3",
        bg = false,
    },
}, false, "editor/FrostHelper/RainbowTilesetController")