using Celeste.Mod.Helpers;
using FrostHelper.Colliders;

namespace FrostHelper;

[CustomEntity("FrostHelper/ArbitraryShapeLightning")]
[Tracked]
public class ArbitraryShapeLightning : Entity {
    public CustomLightningRenderer.ArbitraryFill? Vertices;
    public CustomLightningRenderer.Edge[] Edges;
    public bool Fill;
    
    #region Hooks

    private static bool _hooksLoaded;
    
    [HookPreload]
    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded) return;
        _hooksLoaded = true;

        On.Celeste.Lightning.SetBreakValue += LightningOnSetBreakValue;
        On.Celeste.Lightning.SetPulseValue += LightningOnSetPulseValue;
        FrostModule.RegisterILHook(EasierILHook.HookCoroutine("Celeste.Lightning", "RemoveRoutine", RemoveRoutinePatch));
        On.Celeste.Level.LoadLevel += LevelOnLoadLevel;
    }
    
    [OnUnload]
    internal static void UnloadHooksIfNeeded() {
        if (!_hooksLoaded) return;
        
        On.Celeste.Lightning.SetBreakValue -= LightningOnSetBreakValue;
        On.Celeste.Level.LoadLevel -= LevelOnLoadLevel;
        On.Celeste.Lightning.SetPulseValue -= LightningOnSetPulseValue;
    }

    private static void LevelOnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader) {
        foreach (CustomLightningRenderer r in self.Tracker.SafeGetEntities<CustomLightningRenderer>()) {
            r.Reset();
        }
        
        orig(self, playerintro, isfromloader);
    }

    private static void RemoveRoutinePatch(ILContext il) {
        var cursor = new ILCursor(il);
        
        // <method begin>
        // + MakeArbitraryShapeLightningUncollidableFromBreakerBox()
        cursor.EmitDelegate(MakeArbitraryShapeLightningUncollidableFromBreakerBox);

        // level.Shake();
        // + ShatterArbitraryShapeLightningFromBreakerBox();
        if (!cursor.TryGotoNextBestFit(MoveType.After, i => i.MatchCallOrCallvirt("Celeste.Level", "Shake"))) {
            Logger.Log(LogLevel.Error, "FrostHelper.ArbitraryShapeLightning", "Failed to find IL in Celeste.Lightning.RemoveRoutine, not applying hook at #1.");
            return;
        }
        cursor.EmitDelegate(ShatterArbitraryShapeLightningFromBreakerBox);
        
        // Lightning.SetBreakValue(level, 0.0f);
        // + RemoveArbitraryShapeLightningFromBreakerBox();
        if (!cursor.TryGotoNextBestFit(MoveType.After, i => i.MatchLdcR4(0f), i => i.MatchCallOrCallvirt("Celeste.Lightning", "SetBreakValue"))) {
            Logger.Log(LogLevel.Error, "FrostHelper.ArbitraryShapeLightning", "Failed to find IL in Celeste.Lightning.RemoveRoutine, not applying hook at #2.");
            return;
        }
        
        cursor.EmitDelegate(RemoveArbitraryShapeLightningFromBreakerBox);
    }

    private static void ShatterArbitraryShapeLightningFromBreakerBox() {
        var level = FrostModule.GetCurrentLevel();
        foreach (ArbitraryShapeLightning e in level.Tracker.SafeGetEntities<ArbitraryShapeLightning>()) {
            if (e.AffectedByBreakerBoxes)
                e.Shatter();
        }
    }

    private static void MakeArbitraryShapeLightningUncollidableFromBreakerBox() {
        var level = FrostModule.GetCurrentLevel();
        foreach (ArbitraryShapeLightning e in level.Tracker.SafeGetEntities<ArbitraryShapeLightning>()) {
            if (e.AffectedByBreakerBoxes)
                e.Collidable = false;
        }
        
        foreach (CustomLightningRenderer r in level.Tracker.SafeGetEntities<CustomLightningRenderer>()) {
            if (r.AffectedByLightningBoxes)
                r.UpdateSeeds = false;
        }
    }
    
    private void Shatter()
    {
        if (Scene == null)
            return;

        foreach (var edge in Edges)
        {
            var p = edge.A;
            while (p != edge.B) {
                p = Calc.Approach(p, edge.B, 4f);
                SceneAs<Level>().ParticlesFG.Emit(Lightning.P_Shatter, 1, Position + p, Vector2.One * 3f);
            }
        }
    }
    
    private static void RemoveArbitraryShapeLightningFromBreakerBox() {
        var level = FrostModule.GetCurrentLevel();
        level.Remove(level.Tracker.SafeGetEntities<ArbitraryShapeLightning>()
            .Cast<ArbitraryShapeLightning>()
            .Where(x=> x.AffectedByBreakerBoxes)
            .ToList());
    }

    private static void LightningOnSetBreakValue(On.Celeste.Lightning.orig_SetBreakValue orig, Level level, float t) {
        orig(level, t);
        
        foreach (CustomLightningRenderer r in level.Tracker.SafeGetEntities<CustomLightningRenderer>()) {
            if (r.AffectedByLightningBoxes)
                r.Fade = t * 0.6f;
        }
    }
    
    private static void LightningOnSetPulseValue(On.Celeste.Lightning.orig_SetPulseValue orig, Level level, float t) {
        orig(level, t);
        
        foreach (CustomLightningRenderer r in level.Tracker.SafeGetEntities<CustomLightningRenderer>()) {
            if (r.AffectedByLightningBoxes)
                r.Fade = t * 0.2f;
        }
    }
    #endregion

    private CustomLightningRenderer? _renderer;
    
    private readonly CustomLightningRenderer.Config _config;

    private bool AffectedByBreakerBoxes => _config.AffectedByBreakerBoxes;

    public ArbitraryShapeLightning(EntityData data, Vector2 offset) : base(data.Position + offset) {
        LoadHooksIfNeeded();
        
        var nodes = data.NodesOffset(offset);

        Fill = data.Bool("fill", true);
        Depth = data.Int("depth", -1000100);

        _config = new(
            data.Bool("affectedByLightningBoxes"), 
            data.ParseArray("edgeBolts", ';', CustomLightningRenderer.DefaultBolts.Backing).ToArray(),
            Depth, 
            data.GetColor("fillColor", "18110919") // ColorHelper.ColorToHex(ColorHelper.GetColor("f7b262") * 0.1f)
        );

        if (Fill) {
            Vertices = new(this, ArbitraryShapeEntityHelper.GetFillFromNodes(data, -data.Position));
        }

        Edges = new CustomLightningRenderer.Edge[nodes.Length + (Fill ? 1 : 0)];
        for (int i = 1; i < nodes.Length; i++) {
            Edges[i] = new CustomLightningRenderer.Edge(this, nodes[i - 1] - Position, nodes[i] - Position);
        }
        Edges[0] = new CustomLightningRenderer.Edge(this, Vector2.Zero, nodes[0] - Position);
        if (Fill)
            Edges[^1] = new CustomLightningRenderer.Edge(this, nodes[^1] - Position, Vector2.Zero);

        Collider = new ShapeHitbox(data.GetNodesWithOffsetWithPositionPrepended(offset)) { Fill = Fill };
        Add(new PlayerCollider(OnPlayer, Collider));
    }

    public void OnPlayer(Player player) {
        if (!player.Dead)
            player.Die(Vector2.UnitX);
    }

    private CustomLightningRenderer GetOrAddRenderer(Scene scene) => _renderer ??= ControllerHelper<CustomLightningRenderer>.AddToSceneIfNeeded(scene,
        r => r.Cfg == _config,
        () => new CustomLightningRenderer(_config));

    public override void Added(Scene scene) {
        base.Added(scene);
        var renderer = GetOrAddRenderer(scene);

        foreach (var item in Edges)
            renderer.Add(item);
        
        if (Fill)
            renderer.Add(Vertices!);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        
        var renderer = GetOrAddRenderer(scene);
        foreach (var item in Edges)
            renderer.Remove(item);
        if (Vertices is not null)
            renderer.Remove(Vertices);
    }
    /*
    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        if (Vertices is null)
            return;
        
        Vector3 vert1, vert2;
        for (int i = 0; i < Vertices.Length - 1; i++) {
            vert1 = Vertices[i];
            vert2 = Vertices[i + 1];
            Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert2.X, vert2.Y), Color.Pink);
        }
        vert1 = Vertices[0];
        vert2 = Vertices[Vertices.Length - 1];
        Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert2.X, vert2.Y), Color.Pink);
    }
    */
}
