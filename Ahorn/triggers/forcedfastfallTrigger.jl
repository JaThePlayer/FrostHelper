module FrostHelperFastfallTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/ForcedFastfall" FrostHelperFastfalllTriggerPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
	"Forced Fastfall (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperFastfalllTriggerPlacement,
		"rectangle"
	)
)

end