module FrostHelperBadelineChaserBlockActivator

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/BadelineChaserBlockActivator" BadelineChaserBlockActivator(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockWidth, solid::Bool=true)

const placements = Ahorn.PlacementDict(
    "Badeline Chaser Block Activator (Frost Helper)" => Ahorn.EntityPlacement(
        BadelineChaserBlockActivator,
        "rectangle",
    ) 
)

Ahorn.minimumSize(entity::BadelineChaserBlockActivator) = 16, 16
Ahorn.resizable(entity::BadelineChaserBlockActivator) = true, true

function Ahorn.selection(entity::BadelineChaserBlockActivator)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function getTextures(entity::BadelineChaserBlockActivator)
	solid = get(entity, "solid", true)
	if solid == true
		return "objects/FrostHelper/badelineChaserBlock/activator", "objects/FrostHelper/badelineChaserBlock/emblemsolid"
	end
	return "objects/FrostHelper/badelineChaserBlock/activatorfield", "objects/FrostHelper/badelineChaserBlock/emblemfield"
end


function renderSwapBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, midResource::String, frame::String)
    midSprite = Ahorn.getSprite(midResource, "Gameplay")
    
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)

    # Ahorn.drawImage(ctx, midSprite, x + div(width - midSprite.width, 2), y + div(height - midSprite.height, 2))
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::BadelineChaserBlockActivator, room::Maple.Room)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    frame, mid = getTextures(entity)

    renderSwapBlock(ctx, startX, startY, width, height, mid, frame)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::BadelineChaserBlockActivator, room::Maple.Room)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    frame, mid = getTextures(entity)

    renderSwapBlock(ctx, startX, startY, width, height, mid, frame)
end

end
