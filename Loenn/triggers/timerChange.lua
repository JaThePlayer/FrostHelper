local jautils = require("mods").requireFromPlugin("libraries.jautils")

local timerChange = {
    name = "FrostHelper/TimerChange",
}

local operations = {
    "Add",
    "Set"
}

jautils.createPlacementsPreserveOrder(timerChange, "default", {
    { "timerId", "" },
    { "timeChange", "0", "FrostHelper.condition" },
    { "operation", "Add", operations },
    { "oneUse", true }
})

return timerChange