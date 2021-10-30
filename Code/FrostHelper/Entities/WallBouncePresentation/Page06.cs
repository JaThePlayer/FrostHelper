using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace FrostHelper.Entities.WallBouncePresentation {
    class Page06 : WallbouncePresentationPage {
        public Page06() {
            Transition = Transitions.Rotate3D;
            ClearColor = Calc.HexToColor("d9d2e9");
        }

        public override IEnumerator Routine() {
            yield return 1f;
            Audio.Play("event:/new_content/game/10_farewell/ppt_happy_wavedashing");
            title = new AreaCompleteTitle(new Vector2(Width / 2f, 150f), Presentation.GetCleanDialog("PAGE6_TITLE"), 2f, true);
            yield return 1.5f;
            yield break;
        }

        public override void Update() {
            if (title != null) {
                title.Update();
            }
        }

        public override void Render() {
            Presentation.Gfx["Bird Clip Art"].DrawCentered(new Vector2(Width, Height) / 2f, Color.White, 1.5f);
            if (title != null) {
                title.Render();
            }
        }

        private AreaCompleteTitle title;
    }
}

