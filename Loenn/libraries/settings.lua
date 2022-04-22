local frostSettings = {}
local mods = require("mods")

function frostSettings.get(settingName, default)
    local settings = mods.getModSettings()
    if not settingName then
        return settings
    end

    local value = settings[settingName]
    if value == nil then
        value = default
        settings[settingName] = default
    end

    return value
end

function frostSettings.spinnersConnect()
    return frostSettings.get("graphics_spinners_createConnections", true)
end

function frostSettings.spinnerBorder()
    return frostSettings.get("graphics_spinners_createOutlines", true)
end

function frostSettings.rainbowsUseControllers()
    return frostSettings.get("graphics_rainbows_useMaxHelpingHandControllers", true)
end

function frostSettings.fancyDreamBlocks()
    return frostSettings.get("graphics_dreamBlocks_fancy", true)
end

function frostSettings.useDebugRC()
    return frostSettings.get("debugRC", false)
end

-- setup default values immediately
frostSettings.spinnersConnect()
frostSettings.spinnerBorder()
frostSettings.rainbowsUseControllers()
frostSettings.useDebugRC()
frostSettings.fancyDreamBlocks()

return frostSettings