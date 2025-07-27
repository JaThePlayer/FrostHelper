---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

---@class CassetteTempoTrigger : Trigger
---@field Tempo number
---@field ResetOnLeave boolean

---@type TriggerHandler<CassetteTempoTrigger>
local fHCassetteTempoTrigger = {}
fHCassetteTempoTrigger.name = "FrostHelper/CassetteTempoTrigger"
fHCassetteTempoTrigger.placements = {
    name = "normal",
    data = {
        Tempo = 1.0,
        ResetOnLeave = false,
    }
}

jautils.addExtendedText(fHCassetteTempoTrigger, function (trigger)
    return jautils.roundedToString(trigger.Tempo)
end)

return fHCassetteTempoTrigger