module FrostHelperAnxietyTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/AnxietyTrigger" FrostHelperAnxietyTriggerPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, multiplyer::Number=1.0)

const placements = Ahorn.PlacementDict(
	"Anxiety (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperAnxietyTriggerPlacement,
		"rectangle"
	)
)

end