module FrostHelperSpeedBerryCollectTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/SpeedBerryCollectTrigger" FrostHelperSpeedBerryCollectTriggerPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
	"Speed Berry Collect Trigger (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperSpeedBerryCollectTriggerPlacement,
		"rectangle"
	)
)

end