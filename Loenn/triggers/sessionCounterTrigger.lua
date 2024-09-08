local jautils = require("mods").requireFromPlugin("libraries.jautils")

local sessionCounterTrigger = {
    name = "FrostHelper/SessionCounterTrigger",
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

    "BitwiseOr",
    "BitwiseAnd",
    "BitwiseXor",
    "BitwiseShiftLeft",
    "BitwiseShiftRight",
}

local counterOperationToMathExpr = {
    ["Set"] = "=",
    ["Increment"] = "+=",
    ["Decrement"] = "-=",
    ["Multiply"] = "*=",
    ["Divide"] = "/=",
    ["Remainder"] = "%=",
    ["Power"] = "^=",

    ["BitwiseOr"] = "|=",
    ["BitwiseAnd"] = "&=",
    ["BitwiseXor"] = "xor=",
    ["BitwiseShiftLeft"] = "<<=",
    ["BitwiseShiftRight"] = ">>=",

    ["Min"] = "min=",
    ["Max"] = "max=",
    ["Distance"] = "dist=",
}

jautils.createPlacementsPreserveOrder(sessionCounterTrigger, "default", {
    { "counter", "", "sessionCounter" },
    { "value", "0", "sessionCounter" },
    { "operation", "Set", counterOperations },
    { "clearOnSpawn", false }
}, true)

jautils.addExtendedText(sessionCounterTrigger, function (trigger)
    return string.format("%s %s %s", trigger.counter, counterOperationToMathExpr[trigger.operation], trigger.value)
end)

return sessionCounterTrigger