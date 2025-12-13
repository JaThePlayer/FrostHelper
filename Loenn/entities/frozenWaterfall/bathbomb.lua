local utils = require("utils")
---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controller = {
    name = "FrostHelper/BathBomb",
    texture = "danger/snowball00",
}


jautils.createPlacementsPreserveOrder(controller, "default", {
    { "color", "LightSkyBlue", "color" },
    { "bubble", false },
})

return controller