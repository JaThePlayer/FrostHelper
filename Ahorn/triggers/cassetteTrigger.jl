module FrostHelperCassetteTempoTrigger

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/CassetteTempoTrigger" FHCassetteTempoTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, Tempo::Number=1.0, ResetOnLeave::Bool=false)


const placements = Ahorn.PlacementDict(
	"Cassette Tempo Trigger (Frost Helper)" => Ahorn.EntityPlacement(
		FHCassetteTempoTrigger,
		"rectangle"
	)
)

end