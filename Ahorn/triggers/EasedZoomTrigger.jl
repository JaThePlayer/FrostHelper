module FrostHelperEasedCameraZoomTrigger

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/EasedCameraZoomTrigger" EasedCameraZoomTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, easing::String="Linear", targetZoom::Number=2.0, revertOnLeave::Bool=false, easeDuration::Number=1.0, focusOnPlayer::Bool=false, revertMode::String="RevertToNoZoom", disableInPhotosensitiveMode::Bool=false)

const placements = Ahorn.PlacementDict(
    "Eased Camera Zoom (Frost Helper)" => Ahorn.EntityPlacement(
        EasedCameraZoomTrigger,
        "rectangle"
    )
)

Ahorn.nodeLimits(entity::EasedCameraZoomTrigger) = 0, 1

Ahorn.editingOptions(entity::EasedCameraZoomTrigger) = Dict{String, Any}(
    "easing" => easings,
    "revertMode" => revertModes
)


const easings = String[
    "BackIn",
    "BackInOut",
    "BackOut",
    "BounceIn",
    "BounceInOut",
    "BounceOut",
    "CubeIn",
    "CubeInOut",
    "CubeOut",
    "ElasticIn",
    "ElasticInOut",
    "ElasticOut",
    "ExpoIn",
    "ExpoInOut",
    "ExpoOut",
    "Linear",
    "QuadIn",
    "QuadInOut",
    "QuadOut",
    "SineIn",
    "SineInOut",
    "SineOut"
]

const revertModes = String[
    "RevertToNoZoom",
    "RevertToPreviousZoom",
]

end