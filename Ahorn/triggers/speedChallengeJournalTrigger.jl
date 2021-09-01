module FrostHelperSpeedJournalTrigger

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/SpeedChallengeJournal" JournalTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    challengeNames::String="sc2020_beg_rainbowBerryRush", autoAddSid::Bool=true)

const placements = Ahorn.PlacementDict(
    "Speed Challenge Journal (Frost Helper)" => Ahorn.EntityPlacement(
        JournalTrigger,
        "rectangle",
    ),
)


function Ahorn.nodeLimits(trigger::JournalTrigger)
    return 0, 0
end

end