module FrostHelperFlagIfVisibleModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/FlagIfVisibleTrigger" FrostHelperFlagIfVisiblePlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, flag::String="")

const placements = Ahorn.PlacementDict(
	"Flag If Visible (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperFlagIfVisiblePlacement,
		"rectangle"
	)
)

end