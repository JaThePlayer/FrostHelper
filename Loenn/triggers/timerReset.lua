local jautils = require("mods").requireFromPlugin("libraries.jautils")

local timerChange = {
    name = "FrostHelper/TimerReset",
}

jautils.createPlacementsPreserveOrder(timerChange, "default", {
    { "timerId", "" },
    { "oneUse", false }
})

return timerChange