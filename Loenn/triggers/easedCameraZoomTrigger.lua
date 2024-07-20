local jautils = require("mods").requireFromPlugin("libraries.jautils")

local easedCamera = {
    name = "FrostHelper/EasedCameraZoomTrigger",
    category = "camera",
}

local ZoomTriggerRevertModes = {
    "RevertToNoZoom",
    "RevertToPreviousZoom",
}

jautils.createPlacementsPreserveOrder(easedCamera, "default", {
    { "width", 8}, {"height", 8},
    { "easing", "Linear", jautils.easings },
    { "easeDuration", 1 },
    { "targetZoom", 2 },
    { "revertMode", "RevertToNoZoom", ZoomTriggerRevertModes},
    { "revertOnLeave", false },
    { "focusOnPlayer", true },
    { "disableInPhotosensitiveMode", true },
})

return easedCamera