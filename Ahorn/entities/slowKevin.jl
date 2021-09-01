module SlowKevin

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/SlowCrushBlock" SlowCrushBlock(x::Integer, y::Integer, directory::String = "objects/FrostHelper/slowcrushblock/", chillout::Bool = false, crushSpeed::Number = 120.0, returnSpeed::Number = 60.0, returnAcceleration::Number = 160.0, crushAcceleration::Number = 250.0)

const placements = Ahorn.PlacementDict(
    "Slow Kevin (Both, Frost Helper)" => Ahorn.EntityPlacement(
        SlowCrushBlock,
        "rectangle"
    ),
    "Slow Kevin (Vertical, Frost Helper)" => Ahorn.EntityPlacement(
        SlowCrushBlock,
        "rectangle",
        Dict{String, Any}(
            "axes" => "vertical"
        )
    ),
    "Slow Kevin (Horizontal, Frost Helper)" => Ahorn.EntityPlacement(
        SlowCrushBlock,
        "rectangle",
        Dict{String, Any}(
            "axes" => "horizontal"
        )
    ),
)

frameImage = Dict{String, String}(
    "none" => "objects/FrostHelper/slowcrushblock/block00",
    "horizontal" => "objects/FrostHelper/slowcrushblock/block01",
    "vertical" => "objects/FrostHelper/slowcrushblock/block02",
    "both" => "objects/FrostHelper/slowcrushblock/block03"
)

smallFace = "objects/FrostHelper/slowcrushblock/idle_face"
giantFace = "objects/FrostHelper/slowcrushblock/giant_block00"

kevinColor = (98, 34, 43) ./ 255

Ahorn.editingOptions(entity::SlowCrushBlock) = Dict{String, Any}(
    "axes" => Maple.kevin_axes
)

Ahorn.minimumSize(entity::SlowCrushBlock) = 24, 24
Ahorn.resizable(entity::SlowCrushBlock) = true, true

Ahorn.selection(entity::SlowCrushBlock) = Ahorn.getEntityRectangle(entity)

# Todo - Use randomness to decide on Kevin border
function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SlowCrushBlock, room::Maple.Room)
    axes = lowercase(get(entity.data, "axes", "both"))
    chillout = get(entity.data, "chillout", false)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    giant = height >= 48 && width >= 48 && chillout
    face = giant ? giantFace : smallFace
    frame = frameImage[lowercase(axes)]
    faceSprite = Ahorn.getSprite(face, "Gameplay")

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, kevinColor)
    Ahorn.drawImage(ctx, faceSprite, div(width - faceSprite.width, 2), div(height - faceSprite.height, 2))

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, 0, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, height - 8, 8, 24, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, 0, (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, width - 8, (i - 1) * 8, 24, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, 0, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, 0, height - 8, 0, 24, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, height - 8, 24, 24, 8, 8)
end

end