module FHCustomSnowballLeaveModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/StopCustomSnowballTrigger" CustomSnowballLeaveTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
	"Stop Custom Snowball (Frost Helper)" => Ahorn.EntityPlacement(
		CustomSnowballLeaveTrigger,
		"rectangle"
	)
)

end