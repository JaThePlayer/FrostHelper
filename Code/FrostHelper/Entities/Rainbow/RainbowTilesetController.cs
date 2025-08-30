using FrostHelper.ModIntegration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/RainbowTilesetController")]
[Tracked]
public class RainbowTilesetController : Entity {
    #region Hooks
    private static bool _loadedHooks;

    public static void LoadHooksIfNeeded() {
        if (_loadedHooks)
            return;
        _loadedHooks = true;

        IL.Monocle.TileGrid.RenderAt += TileGrid_RenderAt;
        On.Celeste.Debris.Init_Vector2_char_bool += DebrisOnInit_Vector2_char_bool;
    }

    [OnUnload]
    public static void Unload() {
        if (!_loadedHooks)
            return;
        _loadedHooks = false;

        IL.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt;
        On.Celeste.Debris.Init_Vector2_char_bool -= DebrisOnInit_Vector2_char_bool;
    }

    private static Debris DebrisOnInit_Vector2_char_bool(On.Celeste.Debris.orig_Init_Vector2_char_bool orig, Debris self, Vector2 pos, char tileset, bool playsound) {
        var d = orig(self, pos, tileset, playsound);
        var c = GetController();
        if (c is { }) {
            if (c._allDebris || c._debris.Contains(tileset)) {
                d.PostUpdate += static (entity) => {
                    var debris = (Debris)entity;
                    debris.image.Color =
                        // Todo: if needed, impl Mode.LerpedToGray here, as this is vanilla behavior:
                        //Color.Lerp(ColorHelper.GetHue(self.Scene, self.image.RenderPosition), Color.Gray, self.fadeLerp) * self.alpha;
                        ColorHelper.GetHue(debris.Scene, debris.image.RenderPosition) * debris.alpha;
                };
            }
        }
        
        return d;
    }

    private static byte GetFirstLocalId(ILCursor cursor, string typeName) {
        return (byte) cursor.Body.Variables.First(v => v.VariableType.Name.Contains(typeName)).Index;
    }

    private static void TileGrid_RenderAt(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        var positionId = GetFirstLocalId(cursor, "Vector2");
        var mTextureId = GetFirstLocalId(cursor, "MTexture");

        VariableDefinition controllerId = new VariableDefinition(il.Import(typeof(RainbowTilesetController)));
        il.Body.Variables.Add(controllerId);

        cursor.EmitCall(GetController);
        cursor.Emit(OpCodes.Stloc, controllerId);

        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<SpriteBatch>("Draw"))) {
            cursor.Emit(OpCodes.Ldloc_S, positionId); // pos
            cursor.Emit(OpCodes.Ldloc_S, mTextureId); // mTexture
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldloc, controllerId);
            cursor.EmitCall(GetColor);
        }
    }

    private static RainbowTilesetController? GetController() {
        ColorHelper.SetGetHueScene(Engine.Scene);
        return Engine.Scene.Tracker.SafeGetEntity<RainbowTilesetController>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GetColor(Color col, Vector2 position, MTexture texture, TileGrid self, RainbowTilesetController? controller) {
        if (controller is null)
            return col;
        
        var p = texture.Parent;
        if (ReferenceEquals(p, controller._lastChecked))
            return ColorHelper.GetHue(position) * self.Alpha;

        if (controller._tilesetTextures.Contains(p.AtlasPath)) {
            controller._lastChecked = p;
            return ColorHelper.GetHue(position) * self.Alpha;
        }

        return col;
    }
    #endregion

    struct StringWrap : IEquatable<StringWrap> {
        public StringWrap(string s) {
            String = s;
        }
        
        public string String;
        
        public bool Equals(StringWrap other) {
            return other.String == String;
        }

        public override bool Equals(object? obj)
        {
            return obj is StringWrap other && Equals(other);
        }

        public override int GetHashCode() {
            return string.GetHashCode(String.AsSpan("tilesets/".Length));
        }
        
        public static implicit operator string(StringWrap self) => self.String;
        
        public static implicit operator StringWrap(string self) => new(self);
    }

    private readonly HashSet<StringWrap> _tilesetTextures;
    private MTexture? _lastChecked;
    
    private readonly HashSet<char> _debris;
    private bool _allDebris;

    private static string? AtlasPath(MTexture? x) => x?.AtlasPath ?? x?.Parent?.AtlasPath;
    
    internal RainbowTilesetController(params List<MTexture?> textures) {
        LoadHooksIfNeeded();
        
        _tilesetTextures = textures.SelectNotNull(AtlasPath).Select(x => new StringWrap(x)).ToHashSet();
        _debris = [];
        _allDebris = false;
    }

    public RainbowTilesetController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        LoadHooksIfNeeded();

        bool bg = data.Bool("bg", false);
        var all = data.Attr("tilesets") == "*";
        var autotiler = bg ? GFX.BGAutotiler : GFX.FGAutotiler;
        var includeDebris = data.Bool("includeDebris", false);
        Tag = Tags.Persistent;

        _allDebris = all && includeDebris;
        
        if (!all) {
            var tilesetIDs = FrostModule.GetCharArrayFromCommaSeparatedList(data.Attr("tilesets"));
            
            _debris = includeDebris ? new HashSet<char>(tilesetIDs) : [];

            _tilesetTextures = new(tilesetIDs.Length);
            for (int i = 0; i < tilesetIDs.Length; i++) {
                if (AtlasPath(autotiler.GenerateMap(new VirtualMap<char>(new[,] { { tilesetIDs[i] } }), true).TileGrid.Tiles[0, 0].Parent) is {} path)
                    _tilesetTextures.Add(path);
            }
        } else {
            var autotilerLookupKeys = autotiler.lookup;
            _tilesetTextures = new(autotilerLookupKeys.Count);
            var enumerator = autotilerLookupKeys.GetEnumerator();
            for (int i = 0; i < autotilerLookupKeys.Count; i++) {
                enumerator.MoveNext();
                if (AtlasPath(autotiler.GenerateMap(new VirtualMap<char>(new[,] { { enumerator.Current.Key } }), true).TileGrid.Tiles[0, 0].Parent) is {} path)
                    _tilesetTextures.Add(path);
            }

            _debris = [];
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        var controllers = Scene.Tracker.SafeGetEntities<RainbowTilesetController>();
        if (controllers.Count > 1) {
            var first = controllers.First(c => c.Scene == scene) as RainbowTilesetController;
            if (first != this) {
                first!._tilesetTextures.UnionWith(_tilesetTextures); //first._tilesetTextures.Union(_tilesetTextures).ToList();
                first._debris.UnionWith(_debris);
                first._allDebris |= _allDebris;
                RemoveSelf();
            }
        }
    }

    internal static void RainbowifyTexture(Scene scene, MTexture texture) {
        // Check if its already rainbowified
        if (ControllerHelper<RainbowTilesetController>.FindFirst(scene,
                c => c._tilesetTextures.Contains(texture.AtlasPath)) is { }) {
            return;
        }
        
        var c = ControllerHelper<RainbowTilesetController>.AddToSceneIfNeeded(scene,
            c => true,
            () => new RainbowTilesetController(texture));
        
        if (AtlasPath(texture) is {} path)
            c._tilesetTextures.Add(path);
    }
}
