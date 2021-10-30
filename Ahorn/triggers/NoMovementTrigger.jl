module FrostHelperNoMovementTrigger

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/NoMovementTrigger" NoMovementTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
	"No Movement (Frost Helper)" => Ahorn.EntityPlacement(
		NoMovementTrigger,
		"rectangle"
	)
)

end