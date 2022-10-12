namespace FrostHelper;

[CustomEntity("FrostHelper/DontUpdateInvisibleStylegroundsController")]
public class DontUpdateInvisibleStylegroundsController : Entity {
    Type[] AffectedTypes;
    bool bg;

    public DontUpdateInvisibleStylegroundsController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        AffectedTypes = API.API.GetTypes(data.Attr("types", ""));

        bg = data.Bool("bg", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        var level = scene as Level;

        // wrap any affected stylegrounds with our own styleground which skips Update when the styleground is invisible
        WrapBackdrops(level!.Foreground.Backdrops);
        if (bg) {
            WrapBackdrops(level.Background.Backdrops);
        }

        RemoveSelf();
    }

    private void WrapBackdrops(List<Backdrop> backdrops) {
        for (int i = backdrops.Count - 1; i >= 0; i--) {
            Backdrop? backdrop = backdrops[i];
            if (AffectedTypes.Contains(backdrop.GetType())) {
                backdrops.RemoveAt(i);
                backdrops.Add(new Wrapper(backdrop));
            }
        }
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

        public override void BeforeRender(Scene scene) {
            if (Visible) {
                Inner.BeforeRender(scene);
            }
        }

        public override void Ended(Scene scene) => Inner.Ended(scene);
    }
}
