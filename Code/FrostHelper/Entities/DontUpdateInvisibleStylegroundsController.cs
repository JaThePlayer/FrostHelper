namespace FrostHelper;

[CustomEntity("FrostHelper/DontUpdateInvisibleStylegroundsController")]
public class DontUpdateInvisibleStylegroundsController : Entity {
    Type[] AffectedTypes;

    public DontUpdateInvisibleStylegroundsController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        AffectedTypes = API.API.GetTypes(data.Attr("types", ""));
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        var level = scene as Level;
        var backdrops = level!.Foreground.Backdrops;

        // wrap any affected stylegrounds with our own styleground which skips Update when the styleground is invisible
        for (int i = backdrops.Count - 1; i >= 0; i--) {
            Backdrop? backdrop = level.Foreground.Backdrops[i];
            if (AffectedTypes.Contains(backdrop.GetType())) {
                backdrops.RemoveAt(i);
                backdrops.Add(new Wrapper(backdrop));
            }
        }

        RemoveSelf();
    }

    public class Wrapper : Backdrop {
        public Backdrop Inner;

        public Wrapper(Backdrop inner) {
            Inner = inner;

            OnlyIfFlag = inner.OnlyIfFlag;
            OnlyIfNotFlag = inner.OnlyIfNotFlag;
            AlsoIfFlag = inner.AlsoIfFlag;
            Dreaming = inner.Dreaming;
            ExcludeFrom = inner.ExcludeFrom;
            OnlyIn = inner.OnlyIn;
            InstantIn = inner.InstantIn;
            InstantOut = inner.InstantOut;
        }

        public override void Update(Scene scene) {
            base.Update(scene);

            if (Visible) {
                // this will end up calling base.Update(scene) again, oh well
                Inner.Update(scene);
            }
        }

        // Render doesn't get called if !Visible, no extra actions needed
        public override void Render(Scene scene) => Inner.Render(scene);
        public override void BeforeRender(Scene scene) => Inner.BeforeRender(scene);
        public override void Ended(Scene scene) => Inner.Ended(scene);
    }
}
