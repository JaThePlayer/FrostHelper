//#define DEBUG_DISPLAY

using System.Runtime.CompilerServices;

namespace FrostHelper;

/// <summary>
/// Replaces all decals in the scene with just a few DecalContainerRendererers
/// Implements culling and chunking for much better performance in rooms with thousands of decals
/// </summary>
[CustomEntity("FrostHelper/DecalContainer")]
public class DecalContainerMaker : Trigger {
    public readonly int ChunkSize;

    public DecalContainerMaker(EntityData data, Vector2 offset) : base(data, offset) {
        ChunkSize = data.Int("chunkSizeInTiles", 64) * 8;
    }

    public override void Awake(Scene scene) {
        var renderers = new Dictionary<int, DecalContainerRenderer>();
        var bounds = (Scene as Level)!.Bounds;

        var entities = scene.Entities;

        foreach (var item in entities) {
            var d = item.Depth;

            if (item is Decal && !renderers.TryGetValue(d, out _)) {
                var renderer = new DecalContainerRenderer();
                renderer.Depth = d;
                renderers[d] = renderer;
                scene.Add(renderer);

                int chunkSize = ChunkSize;

                for (int x = bounds.Left; x < bounds.Right; x += chunkSize)                     
                    for (int y = bounds.Top; y < bounds.Bottom; y += chunkSize) {
                        var container = new DecalContainer(new Hitbox(chunkSize, chunkSize, x, y));
                        container.Position = new(x, y);
                        renderer.Containers.Add(container);
                    }
            }
        }


        foreach (var ent in entities) {
            if (ent is not Decal item || getParallax(item))
                continue;
            var d = item.Depth;
            var r = renderers[d];
            foreach (var c in r.Containers)                 
                if (c.IsDecalValid(item)) {
                    c.AddDecal(item);
                    break;
                }
        }

        RemoveSelf();
    }

    public static readonly Func<Decal, bool> getParallax =
        typeof(Decal).GetField("parallax", BindingFlags.Instance | BindingFlags.NonPublic)
        .CreateFastGetter<Decal, bool>();

    public static readonly Func<Decal, List<MTexture>> getTextures =
        typeof(Decal).GetField("textures", BindingFlags.Instance | BindingFlags.NonPublic)
        .CreateFastGetter<Decal, List<MTexture>>();
}

public class DecalContainerRenderer : Entity {
    public List<DecalContainer> Containers = new();

    public override void Awake(Scene scene) {
        foreach (var c in Containers)             
            c.Awake(scene);
    }

    public override void Render() {
        var scene = Scene;
        foreach (var c in Containers)             
            c.Render(scene);
    }
}

public class DecalContainer {
    internal class DecalInfo {
        public Decal decal;
        public float HalfWidth, HalfHeight;
    }

    internal List<DecalInfo> Decals = new();
    Hitbox collider;
    internal int maxW, maxH;
    public Vector2 Position;

    public DecalContainer(Hitbox collider) : base() {
        this.collider = collider;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDecalValid(Decal item) {
        return collider.Collide(item.Position);
    }

    public void AddDecal(Decal item) {
        var t = DecalContainerMaker.getTextures(item)[0];
        var w = t.Width * Math.Abs(item.Scale.X);
        var h = t.Height * Math.Abs(item.Scale.Y);

        Decals.Add(new() { decal = item, HalfWidth = w / 2f, HalfHeight = h / 2f });

        maxW = (int)Math.Max(maxW, w);
        maxH = (int)Math.Max(maxH, h);

        item.RemoveSelf();
    }

    public void Awake(Scene scene) {
        collider.Width += maxW;
        collider.Height += maxH;
    }

    private static bool IsInside(Vector2 cam, DecalInfo decal) {
        var item = decal.decal;

        var hw = decal.HalfWidth;
        var hh = decal.HalfHeight;

        var x = item.Position.X;
        var y = item.Position.Y;

        float lenienceX = hw;
        float lenienceY = hh;

        return x + hw >= cam.X - lenienceX
            && x - hw <= cam.X + 320f + lenienceX
            && y + hh >= cam.Y - lenienceY
            && y - hh <= cam.Y + 180f + lenienceY;
    }

    private bool IsInside(Vector2 cam) {
        var x = Position.X;
        var y = Position.Y;

        var w = collider.Width;
        var h = collider.Height;

        var camX = cam.X;
        var camY = cam.Y;

        const float lenience = 64f;

        return x + w >= camX - lenience
            && x <= camX + 320f + lenience
            && y + h >= camY - lenience
            && y <= camY + 180f + lenience;
    }

    public void SetScene(Decal item, Scene scene) {
        setScene(item, scene);
        foreach (var c in item)             
            setEntity(c, item);
    }

    private static readonly Action<Decal, Scene> setScene = typeof(Decal).CreateDelegateFor<Action<Decal, Scene>>("set_Scene");
    private static readonly Action<Component, Entity> setEntity = typeof(Component).CreateDelegateFor<Action<Component, Entity>>("set_Entity");

    public void Render(Scene scene) {
        var cam = FrostModule.GetCurrentLevel().Camera.Position;

        if (!IsInside(cam))
            return;

        foreach (var d in Decals) {
            var item = d.decal;
            if (item.Visible && IsInside(cam, d)) {
                if (item.Scene is null)
                    SetScene(item, scene);

                if (item.Active)
                    item.Update();
                item.Render();
            }
        }

#if DEBUG_DISPLAY
        var x = Position.X;
        var y = Position.Y;

        var w = collider.Width;
        var h = collider.Height;

        Draw.HollowRect(x, y, w, h, Color.BlueViolet);
        Draw.HollowRect(x, y, w - maxW, h - maxH, Color.Red);
#endif
    }
}
