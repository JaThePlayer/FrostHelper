namespace FrostHelper;

[CustomEntity("FrostHelper/EntityMover")]
class EntityMover : Entity {
    List<Type> Types;
    bool isBlacklist;

    Vector2 endNode;

    // For Tween
    Ease.Easer easer;
    float duration;

    float pauseTime;
    bool mustCollide;

    float pauseTimer = 0f;

    Tween tween;
    List<Tuple<Entity, Vector2>> entities = new List<Tuple<Entity, Vector2>>();

    bool relativeMode = true;
    Vector2 distance;

    string onEndSFX;


    public EntityMover(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);

        Types = FrostModule.GetTypes(data.Attr("types", "")).ToList();
        isBlacklist = data.Bool("blacklist");

        pauseTime = data.Float("pauseTimeLength", 0f);
        pauseTimer = data.Float("startPauseTimeLength", 0f);
        relativeMode = data.Bool("relativeMovementMode", false);
        onEndSFX = data.Attr("onEndSFX", "");
        if (isBlacklist) {
            // Some basic types we don't want to move D:
            foreach (Type type in new List<Type>() { typeof(Player), typeof(SolidTiles), typeof(BackgroundTiles), typeof(SpeedrunTimerDisplay), typeof(StrawberriesCounter) })
                Types.Add(type);
        }

        endNode = data.FirstNodeNullable(offset).GetValueOrDefault();
        easer = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
        duration = data.Float("moveDuration", 1f);
        mustCollide = data.Bool("mustCollide", true);
        distance = new Vector2(endNode.X - Position.X, endNode.Y - Position.Y);
    }

    bool moveBack;

    public override void Awake(Scene scene) {
        base.Awake(scene);
        foreach (Entity entity in scene.Entities) {
            if ((!mustCollide || Collider.Collide(entity.Position)) && (Types.Contains(entity.GetType()) != isBlacklist)) {
                entities.Add(new Tuple<Entity, Vector2>(entity, entity.Position));
            }
        }
        var t = Tween.Create(Tween.TweenMode.Looping, easer, duration, true);
        t.OnUpdate = (Tween tw) => {
            foreach (var item in entities) {
                if (item == null) {
                    continue;
                }
                Vector2 start = item.Item2;
                Vector2 end = relativeMode ? item.Item2 + distance : endNode;
                if (moveBack) {
                    if (item.Item1 is Solid solid) {
                        try {
                            solid.MoveTo(Vector2.Lerp(end, start, tw.Eased));
                        } catch { }
                    } else {
                        item.Item1.Position = Vector2.Lerp(end, start, tw.Eased);
                    }
                } else {
                    if (item.Item1 is Solid solid) {
                        try {
                            solid.MoveTo(Vector2.Lerp(start, end, tw.Eased));
                        } catch { }
                    } else {
                        item.Item1.Position = Vector2.Lerp(start, end, tw.Eased);
                    }
                }

            }
        };

        t.OnComplete = delegate (Tween tw) {
            moveBack = !moveBack;
            pauseTimer = pauseTime;
            if (onEndSFX != "") {
                Audio.Play(onEndSFX);
            }
        };
        Add(tween = t);
    }

    public override void Update() {
        if (pauseTimer > 0f) {
            pauseTimer -= Engine.DeltaTime;
        } else {
            tween.Update();
        }
    }
}
