namespace FrostHelper;

[CustomEntity("FrostHelper/MirrorSurfaceRectangle")]
public class MirrorSurfaceRectangle : Entity {
    public MirrorSurfaceRectangle(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var rect = new Rectangle((int) Position.X, (int)Position.Y, data.Width, data.Height);

        //808000 is a neutral color
        int r = 0x80 - data.Int("offsetX", 0);
        int g = 0x80 - data.Int("offsetY", 0);
        var color = new Color(r, g, 0);

        Add(new MirrorSurface() { 
            OnRender = () => Draw.Rect(rect, color),
        });
    }

    /* Does some funky stuff, TODO: make this a seperate thing
    public override void Awake(Scene scene) {
        base.Awake(scene);

        foreach (var item in scene.Entities) {
            item.Add(new MirrorReflection());
        }
    }*/
}
