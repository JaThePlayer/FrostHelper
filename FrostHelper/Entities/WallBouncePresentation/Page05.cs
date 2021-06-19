using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace FrostHelper.Entities.WallBouncePresentation
{
    class Page05 : WallbouncePresentationPage
    {
		public Page05()
		{
			displays = new List<Display>();
			Transition = Transitions.Spiral;
			ClearColor = Calc.HexToColor("fff2cc");
		}

		public override void Added(WallbouncePresentation presentation)
		{
			base.Added(presentation);
			displays.Add(new Display(new Vector2(Width * 0.28f, Height - 600), Presentation.GetDialog("PAGE5_INFO1"), Presentation.GetTutorialPath("too_soon"), new Vector2(-55f, 20f)));
			displays.Add(new Display(new Vector2(Width * 0.72f, Height - 600), Presentation.GetDialog("PAGE5_INFO2"), Presentation.GetTutorialPath("too_late"), new Vector2(-55f, 20f)));
		}

		public override IEnumerator Routine()
		{
			yield return 0.5f;
			yield break;
		}

		public override void Update()
		{
			foreach (Display display in displays)
			{
				display.Update();
			}
		}

		public override void Render()
		{
			ActiveFont.DrawOutline(Presentation.GetCleanDialog("PAGE5_TITLE"), new Vector2(128f, 100f), Vector2.Zero, Vector2.One * 1.5f, Color.White, 2f, Color.Black);
			foreach (Display display in displays)
			{
				display.Render();
			}
		}

		private List<Display> displays;

		private class Display
		{
			public Display(Vector2 position, string text, string tutorial, Vector2 tutorialOffset)
			{
				Position = position;
				Info = FancyText.Parse(text, 896, 8, 1f, new Color?(Color.Black * 0.6f), null);
				Tutorial = new WallbouncePlayback(tutorial, tutorialOffset);
				Tutorial.OnRender = delegate ()
				{
					Draw.Line(-64f, 20f, 64f, 20f, Color.Black);
					Draw.Line(-64f, 20f, -64f, -60f, Color.Black);
				};
				routine = new Coroutine(Routine(), true);
			}

			private IEnumerator Routine()
			{
				PlayerPlayback playback = Tutorial.Playback;
				int step = 0;
				while (true)
				{
					int frameIndex = playback.FrameIndex;
					if (step % 2 == 0)
					{
						Tutorial.Update();
					}
					if (frameIndex != playback.FrameIndex && playback.FrameIndex == playback.FrameCount - 1)
					{
						while (time < 3f)
						{
							yield return null;
						}
						yield return 0.1f;
						while (xEase < 1f)
						{
							xEase = Calc.Approach(xEase, 1f, Engine.DeltaTime * 4f);
							yield return null;
						}
						xEase = 1f;
						yield return 0.5f;
						xEase = 0f;
						time = 0f;
					}
					int num = step;
					step = num + 1;
					yield return null;
				}
				yield break;
			}

			public void Update()
			{
				time += Engine.DeltaTime;
				routine.Update();
			}

			public void Render()
			{
				Tutorial.Render(Position, 4f);
				Info.DrawJustifyPerLine(Position + Vector2.UnitY * 200f, new Vector2(0.5f, 0f), Vector2.One * 0.8f, 1f, 0, int.MaxValue);
				if (xEase > 0f)
				{
					Vector2 vector = Calc.AngleToVector((1f - xEase) * 0.1f + 0.7853982f, 1f);
					Vector2 value = vector.Perpendicular();
					float num = 0.5f + (1f - xEase) * 0.5f;
					float thickness = 64f * num;
					float scaleFactor = 300f * num;
					Vector2 position = Position;
					Draw.Line(position - vector * scaleFactor, position + vector * scaleFactor, Color.Red, thickness);
					Draw.Line(position - value * scaleFactor, position + value * scaleFactor, Color.Red, thickness);
				}
			}

			public Vector2 Position;

			public FancyText.Text Info;

			public WallbouncePlayback Tutorial;

			private Coroutine routine;

			private float xEase;

			private float time;
		}
	}
}

