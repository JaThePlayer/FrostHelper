local frostHelperSnowballTriggerPlacement = {}
frostHelperSnowballTriggerPlacement.name = "FrostHelper/SnowballTrigger"
frostHelperSnowballTriggerPlacement.placements = {
    name = "normal",
    data = {
        width = 16,
        height = 16,
        spritePath = "snowball",
        speed = 200.0,
        resetTime = 0.8,
        ySineWaveFrequency = 0.5,
        drawOutline = true,
        direction = "Right",
        replaceExisting = true,
    }
}

return frostHelperSnowballTriggerPlacement