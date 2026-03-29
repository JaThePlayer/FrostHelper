---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

if not jautils.devMode then
    return
end

local forceElytra = {
    name = "FrostHelper/ForceElytraFlightTrigger",
}

jautils.createPlacementsPreserveOrder(forceElytra, "default", {
    { "speed", "0,0", jautils.fields.vector2 {} },
    { "aim", "0,0", jautils.fields.vector2 {} },
})

return forceElytra