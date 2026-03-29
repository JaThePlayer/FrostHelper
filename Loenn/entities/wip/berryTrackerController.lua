---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

if not jautils.devMode then
    return
end

local controller = {
    name = "FrostHelper/BerryTrackerController",
    texture = "editor/FrostHelper/SpinnerController",
}

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "berryCounter", "" },
    { "levelsets", "", jautils.fields.list {
        elementSeparator = ","
    }}
})

return controller