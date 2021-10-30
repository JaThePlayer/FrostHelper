module FrostHelperWASDMovementTrigger

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/WASDMovementTrigger" WASDMovementTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, hitboxWidth::Integer=2, speed::Number=80.0, texture::String="util/pixel")

const placements = Ahorn.PlacementDict(
	"WASD Movement (Frost Helper)" => Ahorn.EntityPlacement(
		WASDMovementTrigger,
		"rectangle"
	)
)

end