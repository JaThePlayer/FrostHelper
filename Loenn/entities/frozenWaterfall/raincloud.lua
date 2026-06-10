---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local drawableSpriteStruct = require("structs.drawable_sprite")

local cloud = {
    name = "FrostHelper/Raincloud",
    depth = 0,
}

local rainField = jautils.fields.complex {
    separator = ";",
    innerFields = {
        {
            name = "FrostHelper.fields.rainGenerator.colors",
            default = "161933",
            info = jautils.fields.colorList {
                elementSeparator = ','
            }
        },
        {
            name = "FrostHelper.fields.rainGenerator.opacity",
            default = 1,
            info = jautils.fields.nonNegativeNumber { },
        },
        {
            name = "FrostHelper.fields.rainGenerator.density",
            default = 0.75,
            info = jautils.fields.nonNegativeNumber { },
        },
        {
            name = "FrostHelper.fields.rainGenerator.speedRange",
            default = "200,600",
            info = jautils.fields.range {
                defaultFrom = 200,
                defaultTo = 600
            }
        },
        {
            name = "FrostHelper.fields.rainGenerator.scaleRange",
            default = "4,16",
            info = jautils.fields.range {
                defaultFrom = 4,
                defaultTo = 16
            }
        },
        {
            name = "FrostHelper.fields.rainGenerator.rotationRange",
            default = "-0.05,0.05",
            info = jautils.fields.range {
                defaultFrom = -0.05,
                defaultTo = 0.05
            }
        },
        {
            name = "FrostHelper.fields.rainGenerator.enableFlag",
            default = "",
            info = jautils.fields.sessionExpression { }
        },
        {
            name = "FrostHelper.fields.rainGenerator.collideWith",
            default = "Celeste.Player,Celeste.Solid",
            info = jautils.fields.typeList { }
        },
        {
            name = "FrostHelper.fields.rainGenerator.presimulationTime",
            default = 1,
            info = jautils.fields.nonNegativeNumber { }
        },
        {
            name = "FrostHelper.fields.rainGenerator.windMultiplier",
            default = 0,
            info = jautils.fields.number {
                options = {
                    { "None (0)", 0 },
                    { "Default (0.1)", 0.1 },
                },
                editable = true,
            }
        },
        {
            name = "FrostHelper.fields.rainGenerator.rainbow",
            default = false,
        }
        -- { "flagIfPlayerInside", "" }, -- undecided if I like the current impl
        -- { "generatorLength", -1, "integer" }, -- undecided if this should be public
    }
}

jautils.createPlacementsPreserveOrder(cloud, "default", {
    { "color", "LightSkyBlue", "color" },
    { "rain", "LightSkyBlue;1;0.75;200,600;4,16;-0.05,0.05;;Celeste.Player,Celeste.Solid;1;0;false", rainField },
    { "depth", -9000, jautils.fields.depth { } },
    { "fragile", false },
    { "small", false },
})

local normalScale = 1.0
local smallScale = 29 / 35

local function getTexture(entity)
    local fragile = entity.fragile

    if fragile then
        return "objects/clouds/fragile00"

    else
        return "objects/clouds/cloud00"
    end
end

function cloud.sprite(room, entity)
    local texture = getTexture(entity)
    local sprite = drawableSpriteStruct.fromTexture(texture, entity)
    local small = entity.small
    local scale = small and smallScale or normalScale

    sprite:setScale(scale, 1.0)

    return sprite
end

return cloud