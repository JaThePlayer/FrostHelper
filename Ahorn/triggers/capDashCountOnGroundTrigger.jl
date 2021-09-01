module FrostHelperCapDashOnGroundTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/CapDashOnGroundTrigger" FrostHelperCapDashOnGroundTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
	"Cap Dash Count On Ground (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperCapDashOnGroundTrigger,
		"rectangle"
	)
)

end