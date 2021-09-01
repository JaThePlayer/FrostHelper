module BubblerFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/Bubbler" Bubbler(x::Integer, y::Integer, visible::Bool=true, color::String="White")

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::Bubbler) = Dict{String, Any}(
    "color" => colors
)

const placements = Ahorn.PlacementDict(
    "Bubbler (Frost Helper)" => Ahorn.EntityPlacement(
        Bubbler,
        "point",
        Dict{String, Any}(),
        function(entity::Bubbler)
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + 32, Int(entity.data["y"])),
                (Int(entity.data["x"]) + 64, Int(entity.data["y"]))
            ]
        end
    )
)

Ahorn.nodeLimits(entity::Bubbler) = length(get(entity.data, "nodes", [])) == 2 ? (2, 2) : (0, 0)

sprite = "characters/player/bubble.png"

function Ahorn.selection(entity::Bubbler)
    x, y = Ahorn.position(entity)

    if haskey(entity.data, "nodes")
        controllX, controllY = Int.(entity.data["nodes"][1])
        endX, endY = Int.(entity.data["nodes"][2])

        return [
            Ahorn.getSpriteRectangle(sprite, x, y),
            Ahorn.getSpriteRectangle(sprite, controllX, controllY),
            Ahorn.getSpriteRectangle(sprite, endX, endY)
        ]

    else
        return Ahorn.getSpriteRectangle(sprite, x, y)
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::Bubbler)
    px, py = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", Tuple{Int, Int}[])

    for node in nodes
        nx, ny = Int.(node)

        Ahorn.drawArrow(ctx, px, py, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)
        px, py = nx, ny
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Bubbler, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end