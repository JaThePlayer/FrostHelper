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

jautils.createPlacementsPreserveOrder(timer, "default", {
    { "timerId", "" },
    { "flag", "" },
    { "time", 1.0 },
    { "icon", "frostHelper/time" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "outputCounter", "", "sessionCounter" },
    { "outputCounterUnit", "Milliseconds", jautils.counterTimeUnits },
    { "visible", true },
}, true)

jautils.createPlacementsPreserveOrder(incTimer, "default", {
    { "timerId", "" },
    { "stopFlag", "", "FrostHelper.condition" },
    { "removeFlag", "", "FrostHelper.condition"  },
    { "icon", "frostHelper/time" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "outputCounter", "", "sessionCounter" },
    { "outputCounterUnit", "Milliseconds", jautils.counterTimeUnits },
    { "visible", true },
    { "savePb", false },
}, true)

jautils.createPlacementsPreserveOrder(counterDisplay, "default", {
    { "counter", "", "sessionCounter" },
    { "removeFlag", "" },
    { "visibleFlag", "" },
    { "icon", "" },
    { "iconColor", "ffffff", "color" },
    { "textColor", "ffffff", "color" },
    { "showOnRoomLoad", false },
}, true)

return {
    timer,
    incTimer,
    counterDisplay,
}