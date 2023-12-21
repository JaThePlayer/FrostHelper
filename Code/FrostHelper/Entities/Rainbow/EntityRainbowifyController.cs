namespace FrostHelper;

[CustomEntity("FrostHelper/EntityRainbowifyController")]
[Tracked]
public class EntityRainbowifyController : Entity {
    #region Hooks
    private static ILContext.Manipulator levelRenderManipulator;

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        levelRenderManipulator = AllowColorChange((object self) => {
            var controller = (self as Level)!.Tracker?.SafeGetEntity<EntityRainbowifyController>();
            return controller != null && controller.all;
        }, (object self) => {
            return Vector2.Zero;
        });

        IL.Celeste.Level.Render += levelRenderManipulator;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.Level.Render -= levelRenderManipulator;
        levelRenderManipulator = null!;
    }

    private static ILContext.Manipulator AllowColorChange(Func<object, bool> condition, Func<object, Vector2> positionGetter) {
        return (ILContext il) => {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
#pragma warning disable CL0001 // dont pass lambas - we need to capture some state here
                cursor.EmitDelegate((object self) => {
                    if (condition(self)) {
                        return ColorHelper.GetHue(Engine.Scene, positionGetter(self));
                    } else {
                        return Color.White;
                    }

                });
#pragma warning restore CL0001
                return;
            }
        };
    }
    #endregion

    private bool all;

    private Type[] Types;

    private List<Backdrop> affectedBackdrops;

    public EntityRainbowifyController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        LoadIfNeeded();

        string types = data.Attr("types");
        if (types == "all") {
            all = true;
        } else {
            Types = FrostModule.GetTypes(types);
        }

    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        if (Types is null)
            return;

        foreach (var entity in scene.Entities) {
            if (Types.Contains(entity.GetType()))
                entity.Add(new Rainbowifier());
        }

        affectedBackdrops = new List<Backdrop>();
        foreach (var backdrop in (scene as Level)!.Background.Backdrops) {
            if (Types.Contains(backdrop.GetType()))
                affectedBackdrops.Add(backdrop);
        }
    }

    public override void Render() {
        base.Render();
        if (affectedBackdrops is null)
            return;

        foreach (var backdrop in affectedBackdrops) {
            backdrop.Color = ColorHelper.GetHue(Scene, backdrop.Position);
        }
    }
}
