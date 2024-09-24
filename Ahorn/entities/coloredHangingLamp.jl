module HangingLampOfTheColorVariety

using ..Ahorn, Maple

@mapdef Entity "coloredlights/hanginglamp" ColoredHangingLamp(x::Integer, y::Integer, height::Integer=16, color::String="White", alpha::Number=1, startFade::Integer=24, endFade::Integer=48)

Ahorn.editingOptions(entity::ColoredHangingLamp) = Dict{String, Any}(
    "color" => colors
)

function ColoredHangingLampFinalizer(entity::ColoredHangingLamp)
    nx, ny = Int.(entity.data["nodes"][1])
    y = Int(entity.data["y"])

    entity.data["height"] = max(abs(ny - y), 8)
    
    delete!(entity.data, "nodes")
end

const placements = Ahorn.PlacementDict(
    "Colored Hanging Lamp" => Ahorn.EntityPlacement(
        ColoredHangingLamp,
        "line",
        Dict{String, Any}(),
        ColoredHangingLampFinalizer
    )
)

Ahorn.minimumSize(entity::ColoredHangingLamp) = 0, 8
Ahorn.resizable(entity::ColoredHangingLamp) = false, true

function Ahorn.selection(entity::ColoredHangingLamp)
    x, y = Ahorn.position(entity)
    height = get(entity.data, "height", 16)

    return Ahorn.Rectangle(x + 1, y, 7, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ColoredHangingLamp, room::Maple.Room)
    Ahorn.drawImage(ctx, "objects/hanginglamp", 1, 0, 0, 0, 7, 3)

    height = get(entity.data, "height", 16)
    for i in 0:4:height - 11
        Ahorn.drawImage(ctx, "objects/hanginglamp", 1, i + 3, 0, 8, 7, 4)
    end

    Ahorn.drawImage(ctx, "objects/hanginglamp", 1, height - 8, 0, 16, 7, 8)
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