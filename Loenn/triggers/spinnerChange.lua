local jautils = require("mods").requireFromPlugin("libraries.jautils")

local spinnerChange = {
    name = "FrostHelper/ChangeSpinnersTrigger",
    nodeLimits = { 0, 1 },
}

local spinnerShatter = {
    name = "FrostHelper/ShatterSpinnersTrigger",
}

local tristate = {
    "LeaveUnchanged",
    "True",
    "False"
}

local animationBehaviors = {
    "LeaveUnchanged",
    "Reset"
}

local collisionModes = {
    "LeaveUnchanged",
    "Kill",
    "PassThrough",
    "Shatter",
    "ShatterGroup",
}
local collisionModesForHoldables = {
    "LeaveUnchanged",
    -- "Kill", - No way to consistently kill holdables
    "PassThrough",
    "Shatter",
    "ShatterGroup",
}

jautils.createPlacementsPreserveOrder(spinnerChange, "default", {
    { "filter", "", "FrostHelper.condition" },
    { "cacheFilter", "", "FrostHelper.condition" },
    { "newDirectory", "", "FrostHelper.texturePath", jautils.spinnerDirectoryFieldData },
    { "animationBehavior", "LeaveUnchanged", animationBehaviors },
    { "newCollidable", "LeaveUnchanged", tristate },
    { "newRainbow", "LeaveUnchanged", tristate },
    { "newTint", "", "colorOrEmpty" },
    { "newBorderColor", "", "colorOrEmpty" },
    { "newDashThrough", "LeaveUnchanged", collisionModes },
    { "newOnHoldable", "LeaveUnchanged", collisionModesForHoldables },
    { "newDepth", "", "depthOrEmpty" },

    { "nextTriggerDelay", 0.0 },
    { "oncePerSpinner", false }
}, true)

jautils.createPlacementsPreserveOrder(spinnerShatter, "default", {
    { "filter", "", "FrostHelper.condition" },
    { "cacheFilter", "", "FrostHelper.condition" },
}, true)


return {
    spinnerChange,
    spinnerShatter
}