---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flagSequencer = {
    name = "FrostHelper/SequencerTrigger",
    nodeLimits = { 0, math.huge },
}

jautils.createPlacementsPreserveOrder(flagSequencer, "default", {
    { "sequence", "", "list", {
        elementSeparator = ";",
        elementDefault = "delay:0",
        elementOptions = {
            fieldType = "FrostHelper.polymorphicComplexField",
            separator = ":",
            langPrefix = "FrostHelper.fields.sequence",
            types = {
                jautils.sequencerFields.delay,
                jautils.sequencerFields.flag,
                jautils.sequencerFields.counter,
                jautils.sequencerFields.slider,
                jautils.sequencerFields.activateAt
            }
        },
    }},
    { "terminationCondition", "", "FrostHelper.condition" },
    { "loop", false },
    { "oneUse", true },
}, true)

return flagSequencer