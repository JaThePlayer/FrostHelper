local jautils = require("mods").requireFromPlugin("libraries.jautils")
local enums = require("consts.celeste_enums")

local boss = {
    name = "FrostHelper/LuaBoss",
    depth = 0,
    nodeLineRenderType = "line",
    texture = "characters/badelineBoss/charge00",
    nodeLimits = {0, -1},
}

jautils.createPlacementsPreserveOrder(boss, "default", {
    { "filename", "Assets/FrostHelper/LuaBoss/example" },
	{ "sprite", "badeline_boss" },
    { "color", "ffffff", "color" },
	{ "lockCamera", true },
    { "cameraLockY", true },
    { "startHit", false },
})

return boss
