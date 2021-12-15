local controllerEntity = require("mods").requireFromPlugin("libraries.controllerEntity")

return controllerEntity.createHandler("FrostHelper/EntityRainbowifyController", {
    name = "normal",
    data = {
        types = "",
    },
}, false, "editor/FrostHelper/EntityRainbowifyController")