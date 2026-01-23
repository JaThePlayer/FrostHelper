---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flagSequencer = {
    name = "FrostHelper/SequencerTrigger",
    nodeLimits = { 0, math.huge },
}

jautils.createPlacementsPreserveOrder(flagSequencer, "default", {
    { "sequence", "", jautils.fields.list {
        elementSeparator = ";",
        elementDefault = "delay:0",
        elementOptions = jautils.fields.polymorphicComplex {
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
    { "terminationCondition", "", jautils.fields.sessionExpression{} },
    { "loop", false },
    { "oneUse", true },
}, true)

return flagSequencer