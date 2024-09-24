module LightOfTheFlashTrigger

using ..Ahorn, Maple

@mapdef Trigger "coloredlights/flashlightColorTrigger" FlashlightColorTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, color::String="Transparent", time::Number=-1.0)

const placements = Ahorn.PlacementDict(
    "Player Flashlight Color (Colored Lights)" => Ahorn.EntityPlacement(
        FlashlightColorTrigger,
		"rectangle"
    )
)

function Ahorn.editingOptions(trigger::FlashlightColorTrigger)
    return Dict{String, Any}(
        "color" => colors
    )
end

# Utility functions made by cruor

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

function getColor(color)
    if haskey(Ahorn.XNAColors.colors, color)
        return Ahorn.XNAColors.colors[color]

    else
        try
            return ((Ahorn.argb32ToRGBATuple(parse(Int, replace(color, "#" => ""), base=16))[1:3] ./ 255)..., 1.0)

        catch

        end
    end

    return (1.0, 1.0, 1.0, 1.0)
end

end