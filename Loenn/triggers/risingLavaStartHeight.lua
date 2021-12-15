local trigger = {}
trigger.name = "FrostHelper/CustomRisingLavaStartHeightTrigger"
trigger.placements = {
    name = "normal",
    data = {
        width = 16,
        height = 16,
    }
}

trigger.nodeLineRenderType = "line"
trigger.nodeLimits = { 1, 1 }

return trigger