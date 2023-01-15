namespace FrostHelper;

[CustomEntity("FrostHelper/CustomInvisibleBarrier")]
public sealed class CustomInvisibleBarrier : Solid {
    public CustomInvisibleBarrier(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
        Visible = false;

        if (!data.Bool("canClimb", false))
            Add(new ClimbBlocker(true));

        SurfaceSoundIndex = data.Int("soundIndex", 33);

        AllowStaticMovers = false;
    }
}
