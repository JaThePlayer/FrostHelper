namespace FrostHelper.Entities;

[CustomEntity($"FrostHelper/AltTileLayer = {nameof(Generate)}")]
[TrackedAs(typeof(SolidTiles))]
internal sealed class AltTileLayer : Entity {
    private enum Layers {
        Bg,
        Fg
    }

    private enum CollidableMode {
        Default,
        Yes,
        No
    }

    public static Entity Generate(Level level, LevelData levelData, Vector2 offset, EntityData data) {
        var layer = data.Enum("layer", Layers.Fg);
        var collidable = data.Enum("collidable", CollidableMode.Default) switch {
            CollidableMode.Yes => true,
            CollidableMode.No => false,
            _ => layer is Layers.Fg
        };
        
        var tileString = data.Attr("tileData", "");
        var tw = data.Width / 8;
        var th = data.Height / 8;
        
        if (string.IsNullOrWhiteSpace(tileString))
            return new Entity(data.Position + offset);
        
        var tileMap = new VirtualMap<char>(tw, th, '0');
        var lines = tileString.Split('\n');
        for (int y = 0; y < lines.Length; y++) {
            var line = lines[y];
            for (int x = 0; x < line.Length; x++) {
                var c = line[x];

                tileMap[x, y] = c;
            }
        }
        
        Entity entity = collidable 
            ? layer == Layers.Fg ? new SolidTiles(data.Position + offset, tileMap) : new Solid(data.Position + offset, data.Width, data.Height, true) 
            : new Entity(data.Position + offset);

        entity.Visible = true;
        entity.Active = true;
        entity.Depth = data.Int("depth", Depths.FGTerrain);
        entity.Collidable = collidable;
        Color color = data.GetColor("color", "ffffff");

        if (collidable) {
            // LightingRenderer hardcodes level.SolidsData for light calculations, we need to mutate it for proper lighting.
            var lvlSolids = level.SolidsData;
            Rectangle tileBounds = level.Session.MapData.TileBounds;
            
            for (int x = levelData.TileBounds.Left; x < levelData.TileBounds.Right; x++)
            {
                var locX = x - levelData.TileBounds.Left;
                for (int y = levelData.TileBounds.Top; y < levelData.TileBounds.Bottom; y++)
                {
                    var locY = y - levelData.TileBounds.Top;
                    var newTile = tileMap[locX, locY];
                    if (newTile != '0')
                        lvlSolids[x - tileBounds.Left, y - tileBounds.Top] = newTile;
                }
            }
        }

        TileGrid grid;
        AnimatedTiles animatedTiles;
        
        if (entity is SolidTiles s) {
            grid = s.Tiles;
            animatedTiles = s.AnimatedTiles;
        } else {
            var autotiler = layer switch {
                Layers.Bg => GFX.BGAutotiler,
                Layers.Fg => GFX.FGAutotiler,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
            };
        
            Autotiler.Generated map = autotiler.GenerateMap(tileMap, new Autotiler.Behaviour {
                EdgesExtend = true,
                EdgesIgnoreOutOfLevel = false,
                PaddingIgnoreOutOfLevel = false,
            });
            grid = map.TileGrid;
            animatedTiles = map.SpriteOverlay;

            entity.Add(grid);
            entity.Add(map.SpriteOverlay);
        
            if (collidable)
                AddCollider(entity, tileMap);
        }
        
        grid.Color = color;
        animatedTiles.Color = color;

        return entity;
    }
    
    private static void AddCollider(Entity entity, VirtualMap<char> tileMap) {
        var grid = new Grid(tileMap.Columns, tileMap.Rows, 8f, 8f);
        // copied from SolidTiles ctor
        entity.Collider = grid;
        for (int i = 0; i < tileMap.Columns; i += 50) {
            for (int j = 0; j < tileMap.Rows; j += 50) {
                if (!tileMap.AnyInSegmentAtTile(i, j))
                    continue;
                
                int k = i;
                int num = Math.Min(k + 50, tileMap.Columns);
                while (k < num) {
                    int l = j;
                    int num2 = Math.Min(l + 50, tileMap.Rows);
                    while (l < num2) {
                        if (tileMap[k, l] != '0') {
                            grid[k, l] = true;
                        }
                        l++;
                    }
                    k++;
                }
            }
        }
    }
}