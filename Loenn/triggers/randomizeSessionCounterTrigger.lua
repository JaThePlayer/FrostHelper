local jautils = require("mods").requireFromPlugin("libraries.jautils")

local sessionCounterTrigger = {
    name = "FrostHelper/RandomizeSessionCounterTrigger",
}

local seedModes = {
    "SessionTime",
    "RoomSeed",
    "FullRandom",
    "Custom",
}

jautils.createPlacementsPreserveOrder(sessionCounterTrigger, "default", {
    { "counter", "", "sessionCounter" },
    { "min", "0", "sessionCounter" },
    { "max", "0", "sessionCounter" },
    { "seedMode", "SessionTime", seedModes },
    { "seed", 0, "integer" }
}, true)

jautils.addExtendedText(sessionCounterTrigger, function (trigger)
    return string.format("%s\n[%s;%s]", trigger.counter, trigger.min, trigger.max)
end)

return sessionCounterTrigger