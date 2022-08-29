namespace FrostHelper;

[CustomEntity("FrostHelper/HotGarbageGenerator")]
public class HotGarbageGenerator : Entity {
    public float TilesPerBlade;
    public Vector2 Offset;

    public HotGarbageGenerator(EntityData data, Vector2 offset) : base(data.Position + offset) {
        TilesPerBlade = data.Float("tilesPerBlade", 46f);
        Offset = new();
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        // intentionally don't seed the randomizer :)
        var rng = new Random();

        var levelData = (scene as Level)!.Session.LevelData;

        const int safeLocationWidth = 16;
        const int safeLocationWidthByTwo = safeLocationWidth / 2;

        var safeLocations = levelData.Spawns.Select(s => new Rectangle((int) s.X - safeLocationWidthByTwo, (int)s.Y - safeLocationWidthByTwo, safeLocationWidth, safeLocationWidth)).ToList();

        var tileBounds = levelData.TileBounds;
        var bounds = (scene as Level)!.Bounds;

        var blades = tileBounds.Width * tileBounds.Height / TilesPerBlade;

        for (int i = 0; i < (int)blades; i++) {
            Vector2 pos, nodePos;
            while (true) {
                FindBladeLocation(rng, safeLocations, bounds, out pos, out nodePos);

                // simulate the trajectory of the blade, making sure it doesn't intersect any safe locations
                var center = nodePos + Offset;
                var length = (pos - center).Length();

                bool valid = true;

                for (float p = 0; valid && p <= 1f; p += 1f / 300f) {
                    var angle = MathHelper.Lerp(3.14159274f, -3.14159274f, p);
                    var loc = center + Calc.AngleToVector(angle, length);

                    
                    foreach (var safe in safeLocations) {
                        if (Collide.CircleToRect(loc, 6f, safe)) {
                            valid = false;
                            break;
                        }
                    }
                }

                if (valid)
                    break;
            }

            scene.Add(new BladeRotateSpinner(new() {
                Nodes = new Vector2[1] { nodePos },
                Position = pos,
                Values = new() {
                    { "clockwise", rng.NextDouble() > 0.5f },
                }
            }, Offset));
        }
    }

    private static void FindBladeLocation(Random rng, List<Rectangle> safeLocations, Rectangle bounds, out Vector2 pos, out Vector2 nodePos) {
        pos = RandomPosition(rng, bounds, safeLocations);
        while (true) {
            const int maxOffset = 5 * 8;
            nodePos = pos + new Vector2() {
                X = rng.Next(0, maxOffset * 2) - maxOffset,
                Y = rng.Next(0, maxOffset * 2) - maxOffset,
            };

            if (IsValidLocation(nodePos, safeLocations))
                break;
        }
    }

    public static bool IsValidLocation(Vector2 pos, List<Rectangle> safePlaces) {
        foreach (var safe in safePlaces) {
            if (safe.Contains((int) pos.X, (int) pos.Y)) {
                return false;
            }
        }

        return true;
    }

    public static Vector2 RandomPosition(Random rng, Rectangle bounds, List<Rectangle> safePlaces) {
        while (true) {
            var pos = new Vector2() {
                X = rng.Next(bounds.Left, bounds.Right),
                Y = rng.Next(bounds.Top, bounds.Bottom),
            };
            
            if (IsValidLocation(pos, safePlaces))
                return pos;
        }
    }


}
