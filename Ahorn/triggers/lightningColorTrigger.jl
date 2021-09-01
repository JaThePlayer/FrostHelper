module FrostHelperLightning

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/LightningColorTrigger" FHLightningColorTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, color1::String="fcf579", color2::String="8cf7e2", fillColor::String="ffffff", fillColorMultiplier::Number=0.1, persistent::Bool=true)

const placements = Ahorn.PlacementDict(
	"Lightning Color (Frost Helper)" => Ahorn.EntityPlacement(
		FHLightningColorTrigger,
		"rectangle"
	)
)

end