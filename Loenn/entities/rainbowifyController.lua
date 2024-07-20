local controllerEntity = require("mods").requireFromPlugin("libraries.controllerEntity")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controller = controllerEntity.createHandler("FrostHelper/EntityRainbowifyController", {
    name = "normal",
    data = {
        types = "",
    },
}, false, "editor/FrostHelper/EntityRainbowifyController")

jautils.createPlacementsPreserveOrder(controller, "normal", {
    { "types", "", "typesList" },
})

return controller