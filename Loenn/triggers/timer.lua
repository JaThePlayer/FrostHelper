local jautils = require("mods").requireFromPlugin("libraries.jautils")

local timer = {
    name = "FrostHelper/Timer",
}

local incTimer = {
    name = "FrostHelper/IncrementingTimer",
}

local counterDisplay = {
    name = "FrostHelper/CounterDisplay"
}

local units = {
    "Milliseconds",
    "Seconds",
}

jautils.createPlacementsPreserveOrder(timer, "default", {
    { "timerId", "" },
    { "flag", "" },
    { "time", 1.0 },
    { "icon", "frostHelper/time" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "outputCounter", "", "sessionCounter" },
    { "outputCounterUnit", "Milliseconds", units },
    { "visible", true },
})

jautils.createPlacementsPreserveOrder(incTimer, "default", {
    { "timerId", "" },
    { "stopFlag", "" },
    { "removeFlag", "" },
    { "icon", "frostHelper/time" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "outputCounter", "", "sessionCounter" },
    { "outputCounterUnit", "Milliseconds", units },
    { "visible", true },
})

jautils.createPlacementsPreserveOrder(counterDisplay, "default", {
    { "counter", "", "sessionCounter" },
    { "removeFlag", "" },
    { "visibleFlag", "" },
    { "icon", "" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "showOnRoomLoad", false },
})

return {
    timer,
    incTimer,
    counterDisplay,
}