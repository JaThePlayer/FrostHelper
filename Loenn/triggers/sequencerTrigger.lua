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
                {
                    name = "delay",
                    default = "0",
                    defaultValue = "delay:0",
                    info = {
                    }
                },
                {
                    name = "flag",
                    default = "",
                    defaultValue = "flag:myFlag=1",
                    info = {
                        fieldType = "FrostHelper.complexField",
                        separator = "=",
                        innerFields = {
                            {
                                name = "FrostHelper.fields.sequence.flag.name",
                                default = "myFlag",
                                info = {
                                }
                            },
                            {
                                name = "FrostHelper.fields.sequence.flag.value",
                                default = "1",
                                info = {
                                }
                            }
                        }
                    }
                },
                {
                    name = "counter",
                    default = "",
                    defaultValue = "counter:myCounter=0",
                    info = {
                        fieldType = "FrostHelper.complexField",
                        separator = "=",
                        innerFields = {
                            {
                                name = "FrostHelper.fields.sequence.counter.name",
                                default = "myCounter",
                                info = {
                                }
                            },
                            {
                                name = "FrostHelper.fields.sequence.counter.value",
                                default = "0",
                                info = {
                                }
                            }
                        }
                    }
                },
                {
                    name = "slider",
                    default = "",
                    defaultValue = "slider:mySlider=0",
                    info = {
                        fieldType = "FrostHelper.complexField",
                        separator = "=",
                        innerFields = {
                            {
                                name = "FrostHelper.fields.sequence.slider.name",
                                default = "mySlider",
                                info = {
                                }
                            },
                            {
                                name = "FrostHelper.fields.sequence.slider.value",
                                default = "0",
                                info = {
                                }
                            }
                        }
                    }
                },
                {
                    name = "activateAt",
                    default = "0",
                    defaultValue = "activateAt:0",
                    info = {
                    }
                }
            }
        },
    }},
    { "terminationCondition", "", "FrostHelper.condition" },
    { "loop", false },
    { "oneUse", true },
}, true)

return flagSequencer