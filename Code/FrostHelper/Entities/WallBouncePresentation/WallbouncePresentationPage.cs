using Celeste;
using Microsoft.Xna.Framework;
using System.Collections;

namespace FrostHelper.Entities.WallBouncePresentation {
    public abstract class WallbouncePresentationPage {
        public int Width => Presentation.ScreenWidth;

        public int Height => Presentation.ScreenHeight;

        public abstract IEnumerator Routine();

        public virtual void Added(WallbouncePresentation presentation) {
            Presentation = presentation;
        }

        public virtual void Update() {
        }

        public virtual void Render() {
        }

        protected IEnumerator PressButton() {
            WaitingForInput = true;
            while (!Input.MenuConfirm.Pressed) {
                yield return null;
            }
            WaitingForInput = false;
            Audio.Play("event:/new_content/game/10_farewell/ppt_mouseclick");
            yield break;
        }

        public WallbouncePresentation Presentation;

        public Color ClearColor;

        public Transitions Transition;

        public bool AutoProgress;

        public bool WaitingForInput;

        public enum Transitions {
            ScaleIn,
            FadeIn,
            Rotate3D,
            Blocky,
            Spiral
        }
    }
}
