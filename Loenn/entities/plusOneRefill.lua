local jautils = require("mods").requireFromPlugin("libraries.jautils")

local plusOneRefill = {}
plusOneRefill.name = "FrostHelper/PlusOneRefill"
plusOneRefill.depth = -100

jautils.createPlacementsPreserveOrder(plusOneRefill, "normal", {
    { "directory", "objects/FrostHelper/plusOneRefill"},
    { "particleColor", "ffffff", "color" },
    { "dashCount", 1, "integer" },
    { "respawnTime", 2.5 },
    { "recoverStamina", true },
    { "oneUse", false },
})

function plusOneRefill.sprite(room, entity)
    return jautils.getCustomSprite(entity, "directory", "/idle00", "objects/FrostHelper/heldRefill/idle00")
end

return plusOneRefill