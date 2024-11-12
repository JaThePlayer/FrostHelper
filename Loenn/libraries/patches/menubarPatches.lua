local menubar = require("ui.menubar")
local mods = require("mods")
local frostSettings = require("mods").requireFromPlugin("libraries.settings")

local function toggleOnlyShowDependencies()
    mods.getModSettings()["graphics_spinners_createConnections"] = not frostSettings.spinnersConnect()
end

-- Thanks alt-sides helper (which thanks Just Loenny Things)
local mapButton = $(menubar.menubar):find(t -> t[1] == "view")

if not $(mapButton[2]):find(e -> e[1] == "fh_showSpinnerConnectors") then
    table.insert(mapButton[2], {})
    table.insert(mapButton[2], { "fh_showSpinnerConnectors", toggleOnlyShowDependencies, "checkbox", frostSettings.spinnersConnect })
end

local patcher = {}

return patcher