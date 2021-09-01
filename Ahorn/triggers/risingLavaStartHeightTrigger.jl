module FrostHelperCustomRisingLavaStartHeightTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/CustomRisingLavaStartHeightTrigger" FrostHelperCustomRisingLavaStartHeightTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

Ahorn.nodeLimits(entity::FrostHelperCustomRisingLavaStartHeightTrigger) = 1, 1

const placements = Ahorn.PlacementDict(
	"Custom Rising Lava Start Height (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperCustomRisingLavaStartHeightTrigger,
		"rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]), Int(entity.data["y"]) + 16)]
        end
	)
)

end