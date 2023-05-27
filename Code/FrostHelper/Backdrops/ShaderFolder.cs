using FrostHelper.ModIntegration;

namespace FrostHelper.Backdrops;

internal class ShaderFolder : Backdrop {
    private Effect Effect;
    public List<Backdrop> Inner { get; private set; }

    public ShaderFolder(MapData map, BinaryPacker.Element element) {
        Inner = map.CreateBackdrops(element);

        Effect = ShaderHelperIntegration.GetEffect(element.Attr("shader", ""));
    }

    public override void Update(Scene scene) {
        base.Update(scene);

        if (Visible) {
            foreach (var item in Inner) {
                item.Update(scene);
            }
        }
    }

    public override void BeforeRender(Scene scene) {
        base.BeforeRender(scene);

        if (Visible) {
            foreach (var item in Inner) {
                item.BeforeRender(scene);
            }
        }
    }

    public override void Ended(Scene scene) {
        base.Ended(scene);

        foreach (var item in Inner) {
            item.Ended(scene);
        }
    }

    public override void Render(Scene scene) {
        base.Render(scene);

        ShaderWrapperBackdrop.RenderWithShader(Renderer, scene, Effect, Inner, fakeVisibility: false);
    }
}
