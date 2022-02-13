local jautils = require("mods").requireFromPlugin("libraries.jautils")

local defaultDirectory = "theAbyssJumpStar"

local jumpStar = {
    name = "FrostHelper/JumpStar",
    depth = 100,
}

local jumpStarModes = {
    "Jump",
    "Dash",
}

jautils.createPlacementsPreserveOrder(jumpStar, "default", {
    { "directory", defaultDirectory },
    { "mode", "Jump", jumpStarModes },
    { "strength", 1, "integer" },
})

jautils.addPlacement(jumpStar, "dash", {
    { "mode", "Dash" }
})

function jumpStar.texture(room, entity)
    return string.format("%s/%s/%sstar00", entity.directory or defaultDirectory, entity.mode, tostring(entity.strength or 0))
end

return jumpStar