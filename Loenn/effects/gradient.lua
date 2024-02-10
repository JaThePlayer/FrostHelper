local blendStates = require("mods").requireFromPlugin("libraries.blendStates", "FrostHelper")

return {
    name = "FrostHelper/Gradient",
    defaultData = {
        gradient = "ffffff,000000,90;000000,ffffff,90",
        direction = "Vertical",
        blendMode = "alphablend"
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
        }
    },
}