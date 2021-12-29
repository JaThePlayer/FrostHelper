local script = {}

script.name = "frostSpeenChange"
script.displayName = "Frost Helper Spinners Change"

script.parameters = {
    directory = "",         directoryEdit = false,
    tint = "ffffff",        tintEdit = false,
    borderColor = "000000", borderColorEdit = false,
    destroyColor = "b0eaff",destroyColorEdit = false,
    moveWithWind = false,   moveWithWindEdit = false,
    dashThrough = false,    dashThroughEdit = false,
    bloomAlpha = 0.0,       bloomAlphaEdit = false,
    bloomRadius = 0.0,      bloomRadiusEdit = false,
    attachToSolid = false,  attachToSolidEdit = false,
    spritePathSuffix = "",  spritePathSuffixEdit = false,
    debrisCount = 8,        debrisCountEdit = false,
    drawOutline = true,     drawOutlineEdit = false,
    collidable = true,      collidableEdit = false,
    rainbow = false,        rainbowEdit = false,
    attachGroup = -1,       attachGroupEdit = false,
}

script.fieldOrder = {
    "directory", "directoryEdit",
    "spritePathSuffix","spritePathSuffixEdit",
    "tint", "tintEdit",
    "borderColor", "borderColorEdit",
    "destroyColor", "destroyColorEdit",
    "moveWithWind", "moveWithWindEdit",
    "dashThrough", "dashThroughEdit",
    "bloomAlpha", "bloomAlphaEdit",
    "bloomRadius", "bloomRadiusEdit",
    "attachToSolid", "attachToSolidEdit",
    "debrisCount", "debrisCountEdit",
    "rainbow", "rainbowEdit",
    "collidable", "collidableEdit",
    "drawOutline", "drawOutlineEdit",
    "attachGroup", "attachGroupEdit",
}

script.fieldInformation = {
    tint = {
        fieldType = "color",
        allowXNAColors = true,
    },
    borderColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    destroyColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    attachGroup = {
        fieldType = "FrostHelper.attachGroup",
    },
}

local function change(entity, args, value)
    if (args[value .. "Edit"]) then
        entity[value] = args[value]
    end
end

function script.run(room, args)
    for _, entity in ipairs(room.entities) do
        if entity._name == "FrostHelper/IceSpinner" then
            change(entity, args, "directory")
            change(entity, args, "tint")
            change(entity, args, "moveWithWind")
            change(entity, args, "dashThrough")
            change(entity, args, "bloomAlpha")
            change(entity, args, "bloomRadius")
            change(entity, args, "attachToSolid")
            change(entity, args, "spritePathSuffix")
            change(entity, args, "debrisCount")
            change(entity, args, "drawOutline")
            change(entity, args, "collidable")
            change(entity, args, "drawOutline")
            change(entity, args, "attachGroup")
            change(entity, args, "borderColor")
            change(entity, args, "destroyColor")
        end
    end
end

return script