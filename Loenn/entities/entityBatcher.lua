local controllerEntity = require("mods").requireFromPlugin("libraries.controllerEntity")

return controllerEntity.createHandler("FrostHelper/EntityBatcher", {
    { "flag", "" },
    { "flagInverted", false },
    { "effect", "" },
    { "depth", -1000000, "integer" },
    { "types", "" },
    { "parameters", "" },
    { "dynamicDepthBatchSplitField", "" },
    { "makeEntitiesInvisible", true },
    { "consumeStylegrounds", false }
}, true, "editor/FrostHelper/RainbowTilesetController")