module FrostHelperChronosTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/ChronosTrigger" FrostHelperChronosTriggerPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, time::Number=1.0)

const placements = Ahorn.PlacementDict(
	"Chronos (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperChronosTriggerPlacement,
		"rectangle"
	)
)

end