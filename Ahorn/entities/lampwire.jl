module FHLampWire

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/WireLamps" WireLamps(x::Integer, y::Integer, above::Bool=false, colors::String="Red,Yellow,Blue,Green,Orange", wireColor::String="595866",
                                                 lightCount::Integer=3, lightAlpha::Number=1.0, lightStartFade::Integer=8, lightEndFade::Integer=16)

const placements = Ahorn.PlacementDict(
    "Wire Lamps (Frost Helper)" => Ahorn.EntityPlacement(
        WireLamps,
        "line"
    )
)

Ahorn.nodeLimits(entity::WireLamps) = 1, 1

function Ahorn.selection(entity::WireLamps)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.Rectangle(x - 4, y - 4, 8, 8)]

    for node in nodes
        nx, ny = node

        push!(res, Ahorn.Rectangle(nx - 4, ny - 4, 8, 8))
    end

    return res
end

wireColorHex = "595866"
wireColor = (89, 88, 102, 1) ./ (255, 255, 255, 1)
wireColorSelected = (Ahorn.colors.selection_selected_fc)

function renderWire(ctx::Ahorn.Cairo.CairoContext, entity::WireLamps, color::Ahorn.colorTupleType=wireColor)
    x, y = Ahorn.position(entity)

    start = (x, y)
    stop = get(entity.data, "nodes", [start])[1]
    control = (start .+ stop) ./ 2 .+ (0, 24)

    curve = Ahorn.SimpleCurve(start, stop, control)
    Ahorn.drawSimpleCurve(ctx, curve, color, thickness=1)
end

Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::WireLamps) = renderWire(ctx, entity, wireColorSelected)

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::WireLamps, room::Maple.Room)
    # Make sure Alpha is 1
    rawColor = Ahorn.argb32ToRGBATuple(parse(Int, wireColorHex, base=16))[1:3] ./ 255
    color = (rawColor..., 1.0)

    renderWire(ctx, entity, color)
end

end