namespace FrostHelper.Entities.WallBouncePresentation;

class Page02 : WallbouncePresentationPage {
    public Page02() {
        title = new List<TitleText>();
        Transition = Transitions.Rotate3D;
        ClearColor = Calc.HexToColor("fff2cc");
    }

    public override void Added(WallbouncePresentation presentation) {
        base.Added(presentation);
    }

    public override IEnumerator Routine() {
        string[] text = Presentation.GetCleanDialog("PAGE2_TITLE").Split(new char[]
        {
            '|'
        });
        Vector2 pos = new Vector2(128f, 128f);
        int num;
        for (int i = 0; i < text.Length; i = num + 1) {
            TitleText item = new TitleText(pos, text[i]);
            title.Add(item);
            yield return item.Stamp();
            pos.X += item.Width + ActiveFont.Measure(' ').X * 1.5f;
            num = i;
        }
        yield return PressButton();
        list = FancyText.Parse(Presentation.GetDialog("PAGE2_LIST"), Width, 32, 1f, new Color?(Color.Black * 0.7f), null);
        float delay = 0f;
        while (listIndex < list.Nodes.Count) {
            if (list.Nodes[listIndex] is FancyText.NewLine) {
                yield return PressButton();
            } else {
                delay += 0.008f;
                if (delay >= 0.016f) {
                    delay -= 0.016f;
                    yield return 0.016f;
                }
            }
            listIndex++;
        }
        yield return PressButton();
        Audio.Play("event:/new_content/game/10_farewell/ppt_impossible");
        while (impossibleEase < 1f) {
            impossibleEase = Calc.Approach(impossibleEase, 1f, Engine.DeltaTime);
            yield return null;
        }
        yield break;
    }

    public override void Update() {
    }

    public override void Render() {
        foreach (TitleText titleText in title) {
            titleText.Render();
        }
        if (list != null) {
            list.Draw(new Vector2(160f, 260f), new Vector2(0f, 0f), Vector2.One, 1f, 0, listIndex);
        }
        if (impossibleEase > 0f) {
            MTexture mtexture = Presentation.Gfx["Guy Clip Art"];
            float num = 0.75f;
            mtexture.Draw(new Vector2(Width - mtexture.Width * num, Height - 640f * impossibleEase), Vector2.Zero, Color.White, num);
            Matrix transformationMatrix = Matrix.CreateRotationZ(-0.5f + Ease.CubeIn(1f - impossibleEase) * 8f) * Matrix.CreateTranslation(Width - 500, Height - 600, 0f);
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, transformationMatrix);
            ActiveFont.Draw(Presentation.GetCleanDialog("PAGE2_IMPOSSIBLE"), Vector2.Zero, new Vector2(0.5f, 0.5f), Vector2.One * (2f + (1f - impossibleEase) * 0.5f), Color.Black * impossibleEase);
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin();
        }
    }

    private List<TitleText> title;

    private FancyText.Text list;

    private int listIndex;

    private float impossibleEase;

    private class TitleText {
        public TitleText(Vector2 pos, string text) {
            Position = pos;
            Text = text;
            Width = ActiveFont.Measure(text).X * 1.5f;
        }

        public IEnumerator Stamp() {
            while (ease < 1f) {
                ease = Calc.Approach(ease, 1f, Engine.DeltaTime * 4f);
                yield return null;
            }
            yield return 0.2f;
            yield break;
        }

        public void Render() {
            if (ease <= 0f) {
                return;
            }
            Vector2 scale = Vector2.One * (1f + (1f - Ease.CubeOut(ease))) * 1.5f;
            ActiveFont.DrawOutline(Text, Position + new Vector2(Width / 2f, ActiveFont.LineHeight * 0.5f * 1.5f), new Vector2(0.5f, 0.5f), scale, Color.White, 2f, Color.Black);
        }

        public const float Scale = 1.5f;

        public string Text;

        public Vector2 Position;

        public float Width;

        private float ease;
    }
}
