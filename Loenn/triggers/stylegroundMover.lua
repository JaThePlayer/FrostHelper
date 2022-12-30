local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")

local afterDeathBehaviours = {
    "Reset", -- resets the stylegrounds to the position before any movement.
    "Stay", -- makes the styleground stay at the location it was before death
    "SnapToEnd", -- makes the styleground snap to the end position after death.
}

local mover = {
    name = "FrostHelper/StylegroundMoveTrigger",
}

jautils.createPlacementsPreserveOrder(mover, "Styleground Move", {
    { "tag", "", "FrostHelper.stylegroundTag" },
    { "duration", 1.0 },
    { "moveByX", 0.0 },
    { "moveByY", 0.0 },
    { "easing", "CubeInOut", jautils.easings },
    { "afterDeath", "Reset", afterDeathBehaviours },
    { "once", false },
}, true)

jautils.addExtendedText(mover, function(trigger)
    return trigger.tag
end)

return mover