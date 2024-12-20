local jautils = require("mods").requireFromPlugin("libraries.jautils")

local sessionSliderTrigger = {
    name = "FrostHelper/SessionSliderTrigger",
}

local counterOperations = {
    "Set",
    "Increment",
    "Decrement",
    "Multiply",
    "Divide",
    "Remainder",
    "Power",

    "Min",
    "Max",
    "Distance",
}

local counterOperationToMathExpr = {
    ["Set"] = "=",
    ["Increment"] = "+=",
    ["Decrement"] = "-=",
    ["Multiply"] = "*=",
    ["Divide"] = "/=",
    ["Remainder"] = "%=",
    ["Power"] = "^=",

    ["Min"] = "min=",
    ["Max"] = "max=",
    ["Distance"] = "dist=",
}

jautils.createPlacementsPreserveOrder(sessionSliderTrigger, "default", {
    { "slider", "", "sessionSlider" },
    { "value", "0", "FrostHelper.condition" },
    { "operation", "Set", counterOperations },
    { "clearOnSpawn", false },
    { "once", false }
}, true)

jautils.addExtendedText(sessionSliderTrigger, function (trigger)
    return string.format("%s %s %s", trigger.slider, counterOperationToMathExpr[trigger.operation], trigger.value)
end)

return sessionSliderTrigger