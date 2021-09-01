module FrostHelperSnowballTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/SnowballTrigger" FrostHelperSnowballTriggerPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, spritePath::String="snowball", speed::Number=200.0, resetTime::Number=0.8, ySineWaveFrequency::Number=0.5, drawOutline::Bool=true, direction::String="Right")

const placements = Ahorn.PlacementDict(
	"Custom Snowball Trigger (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperSnowballTriggerPlacement,
		"rectangle"
	)
)

const directions = String[
	"Right", "Left"
]

Ahorn.editingOptions(entity::FrostHelperSnowballTriggerPlacement) = Dict{String, Any}(
    "direction" => directions
)

end