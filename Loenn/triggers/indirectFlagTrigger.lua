---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local function cleanupExtText(txt)
    return txt
end

local indirectFlag = {
    name = "FrostHelper/IndirectFlagTrigger",
}

jautils.createPlacementsPreserveOrder(indirectFlag, "default", {
    { "flag", '"death_$($deaths)"', jautils.fields.sessionExpression {} },
    { "value", '1', jautils.fields.sessionExpression {} }
})

jautils.addExtendedText(indirectFlag, function (trigger)
    return cleanupExtText(trigger.flag)
end)

local indirectCounter = {
    name = "FrostHelper/IndirectCounterTrigger",
}

jautils.createPlacementsPreserveOrder(indirectCounter, "default", {
    { "counter", '"death_$($deaths)"', jautils.fields.sessionExpression {} },
    { "value", '1', jautils.fields.sessionExpression {} }
})

jautils.addExtendedText(indirectCounter, function (trigger)
    return cleanupExtText(trigger.counter)
end)

local indirectSlider = {
    name = "FrostHelper/IndirectSliderTrigger",
}

jautils.createPlacementsPreserveOrder(indirectSlider, "default", {
    { "slider", '"time_$($deaths)"', jautils.fields.sessionExpression {} },
    { "value", '$time', jautils.fields.sessionExpression {} }
})

jautils.addExtendedText(indirectSlider, function (trigger)
    return cleanupExtText(trigger.slider)
end)


return {
    indirectFlag,
    indirectCounter,
    indirectSlider,
}