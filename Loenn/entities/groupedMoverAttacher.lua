local jautils = require("mods").requireFromPlugin("libraries.jautils")

local entityGroupAttacher = {
    name = "FrostHelper/GroupedMoverAttacher",
    depth = math.huge,
    fillColor = { 1, 1, 1, 0.2 },
    borderColor = { 1, 1, 1, 0.5 },
}

jautils.createPlacementsPreserveOrder(entityGroupAttacher, "default", {
    { "width", 8 },
    { "height", 8 },
    { "types", "", "typesList" },
    { "isBlacklist", false },
    { "attachGroup", 0, "FrostHelper.attachGroup" },
    { "specialHandling", true },
    { "canBeLeader", true },
})

return entityGroupAttacher

