local script = {}

script.name = "frostSpeenChange"
script.displayName = "Frost Helper Spinners Change"

script.parameters = {
    directory = "",
    tint = "ffffff",
    moveWithWind = false,
    dashThrough = false,
    bloomAlpha = 0.0,
    bloomRadius = 0.0,
    attachToSolid = false,
    spritePathSuffix = "",
}

function script.fieldOrder()
    return {
        "directory", "tint", "moveWithWind", "dashThrough", "bloomAlpha", "bloomRadius", "attachToSolid", "spritePathSuffix"
    }
end

function script.run(room, args)
    for _, entity in ipairs(room.entities) do
        if entity._name == "FrostHelper/IceSpinner" then
            entity.spritePathSuffix = args.spritePathSuffix
            entity.attachToSolid = args.attachToSolid
            entity.tint = args.tint or "ffffff"
            entity.directory = args.directory
            entity.destroyColor = entity.ShatterColor or "ffffff"
            entity.moveWithWind = args.moveWithWind or false
            entity.dashThrough = args.dashThrough or false
            entity.bloomAlpha = args.bloomAlpha or 0.0
            entity.bloomRadius = args.bloomRadius or 0.0
        end
    end
end

return script