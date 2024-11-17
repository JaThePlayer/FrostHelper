local jautils = require("mods").requireFromPlugin("libraries.jautils")

local staticBumper = {}
staticBumper.name = "FrostHelper/StaticBumper"
staticBumper.depth = 0
staticBumper.nodeLineRenderType = "line"

jautils.createPlacementsPreserveOrder(staticBumper, "normal", {
    { "respawnTime", 0.6 },
    { "moveTime", 1.81818187 },
    { "sprite", "bumper" },
    { "easing", "CubeInOut", jautils.easings },
    { "hitbox", "C,12,0,0", "FrostHelper.collider" },
    { "wobble", false },
    { "notCoreMode", false },
})

staticBumper.nodeLimits = { 0, 1 }

staticBumper.texture = "objects/Bumper/Idle22"

return staticBumper