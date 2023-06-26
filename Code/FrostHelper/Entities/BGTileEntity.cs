namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/BGTileEntity")]
internal sealed class BGTileEntity : Entity {
    private VirtualMap<char> TileMap;
    private TileGrid TileGrid;

    private readonly float Alpha;
    private readonly Color Color;
    private readonly int AttachGroup;

    public BGTileEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Depth = data.Int("depth", Depths.BGTerrain);
        Alpha = data.Float("alpha", 1f);
        Color = data.GetColor("color", "ffffff");
        AttachGroup = data.Int("attachGroup", -1);

        var tileString = data.Attr("tileData", "");
        var tw = data.Width / 8;
        var th = data.Height / 8;

        TileMap = new(tw, th, '0');

        if (string.IsNullOrWhiteSpace(tileString))
            return;

        var lines = tileString.Split('\n');
        for (int y = 0; y < lines.Length; y++) {
            var line = lines[y];
            for (int x = 0; x < line.Length; x++) {
                var c = line[x];

                TileMap[x, y] = c;
            }
        }

        TileGrid = GFX.BGAutotiler.GenerateMap(TileMap, new Autotiler.Behaviour() {
            EdgesExtend = false,
            EdgesIgnoreOutOfLevel = false,
            PaddingIgnoreOutOfLevel = false,
        }).TileGrid;

        
        TileGrid.Alpha = Alpha;
        TileGrid.Color = Color;

        Add(TileGrid);

        AddCollider();

        StaticMover mover = AttachGroup == -1 ? new StaticMover() : new GroupedStaticMover(AttachGroup, data.Bool("canBeLeader", false));
        mover.SolidChecker = (solid) => solid.CollideCheck(this);
        mover.OnShake = mover.OnMove = (offset) => {
            Position += offset;
        };

        Add(mover);
    }

    private void AddCollider() {
        var data = TileMap;
        var grid = new Grid(data.Columns, data.Rows, 8f, 8f);
        // copied from SolidTiles ctor
        Collider = grid;
        for (int i = 0; i < data.Columns; i += 50) {
            for (int j = 0; j < data.Rows; j += 50) {
                if (data.AnyInSegmentAtTile(i, j)) {
                    int k = i;
                    int num = Math.Min(k + 50, data.Columns);
                    while (k < num) {
                        int l = j;
                        int num2 = Math.Min(l + 50, data.Rows);
                        while (l < num2) {
                            if (data[k, l] != '0') {
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

    public override void Awake(Scene scene) {
        base.Awake(scene);

        TileGrid.ClipCamera = (scene as Level)?.Camera;
    }
}
