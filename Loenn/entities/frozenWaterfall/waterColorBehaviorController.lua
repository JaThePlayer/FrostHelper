local utils = require("utils")
---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local controller = {
    name = "FrostHelper/DynamicWaterBehaviorController",
    texture = "editor/FrostHelper/SpinnerController",
}

local behaviorsFieldInfo = {
    elementSeparator = ";",
    elementDefault = "0000ff~setDashes:true=0",
    elementOptions = {
        fieldType = "FrostHelper.complexField",
        separator = "~",
        innerFields = {
            {
                name = "FrostHelper.fields.DynamicWaterBehavior.color",
                info = {
                    fieldType = "color",
                    allowXNAColors = true,
                }
            },
            {
                name = "FrostHelper.fields.DynamicWaterBehavior.behavior",
                info = {
                    fieldType = "FrostHelper.polymorphicComplexField",
                    separator = ":",
                    langPrefix = "FrostHelper.fields.sequence",
                    types = {
                        jautils.sequencerFields.flag,
                        jautils.sequencerFields.counter,
                        jautils.sequencerFields.slider,
                        jautils.sequencerFields.setDashes,
                        jautils.sequencerFields.kill,
                        jautils.sequencerFields.blockDashRecovery,
                        jautils.sequencerFields.blockDash,
                    }
                }
            }
        }
    },
}

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "behaviors", "Blue~setDashes:true=0;Red~setDashes:true=1;Pink~setDashes:true=2;Black~kill:", "list", behaviorsFieldInfo },
    --{ "rainBehaviors", "Blue~blockDash:0.18;Blue~blockDashRecovery:0.18;Red~setDashes:false=1;Pink~setDashes:false=2;Black~kill:", "list", behaviorsFieldInfo }
})

return controller