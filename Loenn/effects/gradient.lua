local blendStates = require("mods").requireFromPlugin("libraries.blendStates", "FrostHelper")

return {
    name = "FrostHelper/Gradient",
    defaultData = {
        gradient = "ffffff,000000,50;000000,ffffff,50",
        direction = "Vertical",
        blendMode = "alphablend",
        loopX = false,
        loopY = false,
    },
    fieldInformation = {
        direction = {
            editable = false,
            options = {
                "Vertical", "Horizontal"
            }
        },
        blendMode = {
            editable = false,
            options = blendStates.blendModes
        },
        gradient = {
            fieldType = "list",
            elementSeparator = ";",
            elementDefault = "ffffff,000000,100",
            elementOptions = {
                fieldType = "FrostHelper.complexField",
                separator = ",",
                innerFields = {
                    {
                        name = "FrostHelper.fields.gradient.from",
                        info = {
                            fieldType = "color",
                        }
                    },
                    {
                        name = "FrostHelper.fields.gradient.to",
                        info = {
                            fieldType = "color",
                        }
                    },
                    {
                        name = "FrostHelper.fields.gradient.percent",
                        default = 0,
                        info = {
                            fieldType = "number",
                            minimumValue = 0.00001,
                        }
                    },
                }
            },
        },
    },
}