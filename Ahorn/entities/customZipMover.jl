module customZipMover

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomZipMover" CustomZipMover(x::Integer, y::Integer, percentage::Integer=100, lineColor::String="663931", lineLightColor::String="9b6157", directory::String="objects/zipmover", isCore::Bool=false, coldLineColor::String="006bb3", coldLineLightColor::String="0099ff", tint::String="ffffff", showLine::Bool=true, fillMiddle::Bool=true, bloomAlpha::Number=1.0, bloomRadius::Number=6.0, rainbow::Bool=false)

const placements = Ahorn.PlacementDict(
	"Custom Zip Mover (Slow, Frost Helper)" => Ahorn.EntityPlacement(
        CustomZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
			entity.data["percentage"] = 50
			entity.data["lineColor"] = "006bb3"
			entity.data["lineLightColor"] = "0099ff"
			entity.data["directory"] = "objects/FrostHelper/customZipMover/redcog/cold"
        end
    ),
	"Custom Zip Mover (Fast, Frost Helper)" => Ahorn.EntityPlacement(
        CustomZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
			entity.data["percentage"] = 200
			entity.data["lineColor"] = "e62e00"
			entity.data["lineLightColor"] = "ff5c33"
			entity.data["directory"] = "objects/FrostHelper/customZipMover/redcog"
        end
    ),
	"Custom Zip Mover (Core, Frost Helper)" => Ahorn.EntityPlacement(
        CustomZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
			entity.data["percentage"] = 200
			entity.data["lineColor"] = "e62e00"
			entity.data["lineLightColor"] = "ff5c33"
			entity.data["directory"] = "objects/FrostHelper/customZipMover/redcog"
			entity.data["isCore"] = true
			entity.data["coldLineColor"] = "006bb3"
			entity.data["coldLineLightColor"] = "0099ff"
        end
    ),
	"Custom Zip Mover (Custom, Frost Helper)" => Ahorn.EntityPlacement(
        CustomZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
			entity.data["percentage"] = 100
        end
    )	
)

Ahorn.nodeLimits(entity::CustomZipMover) = 1, 1

Ahorn.minimumSize(entity::CustomZipMover) = 16, 16
Ahorn.resizable(entity::CustomZipMover) = true, true

function Ahorn.selection(entity::CustomZipMover)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10)]
end

ropeColor = (102, 57, 49) ./ 255
frame = "objects/zipmover/block"
light = "objects/zipmover/light01"
#customCog = String(entity.data["directory"]) + "/cog"

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomZipMover, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

	#lightSprite = Ahorn.getSprite(get(entity.data, "directory", "objects/zipmover")+"/light01", "Gameplay")
	lightSprite = Ahorn.getSprite(light, "Gameplay")
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    cx, cy = x + width / 2, y + height / 2
    cnx, cny = nx + width / 2, ny + height / 2

    length = sqrt((x - nx)^2 + (y - ny)^2)
    theta = atan(cny - cy, cnx - cx)

    Ahorn.Cairo.save(ctx)

    Ahorn.translate(ctx, cx, cy)
    Ahorn.rotate(ctx, theta)
	ropeColor = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "lineColor", "663931"), base=16))[1:3] ./ 255
	realRopeColor = (ropeColor..., 1.0)
    Ahorn.setSourceColor(ctx, realRopeColor)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    # Offset for rounding errors
    Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
    Ahorn.line_to(ctx, length, 4 + (theta <= 0))

    Ahorn.move_to(ctx, 0, -4 - (theta > 0))
    Ahorn.line_to(ctx, length, -4 - (theta > 0))

    Ahorn.stroke(ctx)

    Ahorn.Cairo.restore(ctx)

    # Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 1.0))
	rawTint = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "tint", "ffffff"), base=16))[1:3] ./ 255
	realTint = (rawTint..., 1.0)
	color = get(entity.data, "color", "Custom")
    if color == "Red"
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (230.0, 46.0, 0.0, 1.0))
        Ahorn.drawSprite(ctx, "objects/FrostHelper/customZipMover/redcog/cog.png", cnx, cny)
	elseif color == "Core"
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (230.0, 46.0, 0.0, 1.0))
        Ahorn.drawSprite(ctx, "objects/FrostHelper/customZipMover/redcog/cog.png", cnx, cny)
    elseif color == "Blue"
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 107.0, 179.0, 1.0))
        Ahorn.drawSprite(ctx, "objects/FrostHelper/customZipMover/redcog/cold/cog.png", cnx, cny)
	elseif color == "Black"
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 0.3))
		Ahorn.drawSprite(ctx, "objects/FrostHelper/customZipMover/blackcog/cog.png", cnx, cny)
	elseif color == "Custom"
		texture = get(entity.data, "directory", "objects/zipmover")
		realTexture = "$texture/cog.png"
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 1.0))
		Ahorn.drawSprite(ctx, realTexture, cnx, cny, tint = realTint)
    else
		Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 1.0))
        Ahorn.drawSprite(ctx, "objects/zipmover/cog.png", cnx, cny)
    end
		
    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8, tint = realTint)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8, tint = realTint)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8, tint = realTint)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8, tint = realTint)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8, tint = realTint)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8, tint = realTint)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8, tint = realTint)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8, tint = realTint)

    Ahorn.drawImage(ctx, lightSprite, x + floor(Int, (width - lightSprite.width) / 2), y, tint = realTint)
end

end