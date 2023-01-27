local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controller = {
    name = "FrostHelper/CustomSpinnerController",
    texture = "editor/FrostHelper/SpinnerController",
}

local validOutlineShaders = { "", "FrostHelper/spinnerSolidBorder" }

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "cycles", false },
    { "outlineShader", "", validOutlineShaders },
})

return controller