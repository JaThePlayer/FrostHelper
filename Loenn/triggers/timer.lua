local jautils = require("mods").requireFromPlugin("libraries.jautils")

local timer = {
    name = "FrostHelper/Timer",
}

local incTimer = {
    name = "FrostHelper/IncrementingTimer",
}

jautils.createPlacementsPreserveOrder(timer, "default", {
    { "timerId", "" },
    { "flag", "" },
    { "time", 1.0 },
    { "visible", true },
})

jautils.createPlacementsPreserveOrder(incTimer, "default", {
    { "timerId", "" },
    { "stopFlag", "" },
    { "removeFlag", "" },
    { "visible", true },
})

return {
    timer,
    incTimer,
}