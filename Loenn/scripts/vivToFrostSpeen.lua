local script = {}

script.name = "vivToFrostSpeen"
script.displayName = "Viv Helper To Frost Helper Spinners"

script.parameters = {
    moveWithWind = false,
    dashThrough = false,
    bloomAlpha = 0.0,
    bloomRadius = 0.0,
}

function script.fieldOrder()
    return {
        "moveWithWind", "dashThrough", "bloomAlpha", "bloomRadius"
    }
end

function script.run(room, args)
    for _, entity in ipairs(room.entities) do
        if entity._name == "VivHelper/CustomSpinner" then
            entity._name = "FrostHelper/IceSpinner"
            entity.spritePathSuffix = entity.Subdirectory
            entity.attachToSolid = entity.AttachToSolid
            entity.tint = entity.Color
            entity.directory = entity.Directory
            entity.destroyColor = entity.ShatterColor or "ffffff"
            entity.moveWithWind = args.moveWithWind or false
            entity.dashThrough = args.dashThrough or false
            entity.bloomAlpha = args.bloomAlpha or 0.0
            entity.bloomRadius = args.bloomRadius or 0.0

            entity.AttachToSolid = nil
            entity.BorderColor = nil
            entity.Color = nil
            entity.CustomDebris = nil
            entity.DebrisToScale = nil
            entity.Depth = nil
            entity.Directory = nil
            entity.HitboxType = nil
            entity.ImageScale = nil
            entity.Scale = nil
            entity.ShatterColor = nil
            entity.ShatterFlag = nil
            entity.Subdirectory = nil
            entity.Type = nil
            entity.shatterOnDash = nil
            
            --print(require("utils").serialize(entity))
        end
    end
end

return script