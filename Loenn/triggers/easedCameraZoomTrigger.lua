local jautils = require("mods").requireFromPlugin("libraries.jautils")

local easedCamera = {
    name = "FrostHelper/EasedCameraZoomTrigger",
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
    { "revertOnLeave", false },
    { "focusOnPlayer", true },
    { "revertMode", "RevertToNoZoom", ZoomTriggerRevertModes},
    { "disableInPhotosensitiveMode", true },
})

return easedCamera