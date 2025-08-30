---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local journalTrigger = {}
journalTrigger.name = "FrostHelper/SpeedChallengeJournal"

jautils.createPlacementsPreserveOrder(journalTrigger, "normal", {
    { "challengeNames", "fh_test", "list" },
    { "autoAddSid",  true },
})

return journalTrigger