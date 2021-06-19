using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace FrostHelper.Entities.WallBouncePresentation
{
    class Page04 : WallbouncePresentationPage
    {
		public Page04()
		{
			Transition = Transitions.FadeIn;
			ClearColor = Calc.HexToColor("f4cccc");
		}

		public override void Added(WallbouncePresentation presentation)
		{
			base.Added(presentation);
			List<MTexture> textures = Presentation.Gfx.GetAtlasSubtextures(presentation.GraphicsKeyPrefix + "playback/platforms");
			tutorial = new WallbouncePlayback(Presentation.GetTutorialPath("wallbounce"), new Vector2(-88f, 20f));
			tutorial.OnRender = delegate ()
			{
				textures[(int)(time % textures.Count)].DrawCentered(Vector2.Zero);
			};
			tutorial.Playback.Visible = true;
		}

		public override IEnumerator Routine()
		{
			yield return 0.5f;
			list = FancyText.Parse(Presentation.GetDialog("PAGE4_LIST"), Width, 32, 1f, new Color?(Color.Black * 0.7f), null);
			float delay = 0f;
			while (listIndex < list.Nodes.Count)
			{
				if (list.Nodes[listIndex] is FancyText.NewLine)
				{
					yield return PressButton();
				}
				else
				{
					delay += 0.008f;
					if (delay >= 0.016f)
					{
						delay -= 0.016f;
						yield return 0.016f;
					}
				}
				listIndex++;
			}
			yield break;
		}

		public override void Update()
		{
			time += Engine.DeltaTime * 4f;
			tutorial.Update();
		}

		public override void Render()
		{
			ActiveFont.DrawOutline(Presentation.GetCleanDialog("PAGE4_TITLE"), new Vector2(128f, 100f), Vector2.Zero, Vector2.One * 1.5f, Color.White, 2f, Color.Black);
			tutorial.Render(new Vector2(Width / 2f, Height / 2f - 100f));
			if (list != null)
			{
				list.Draw(new Vector2(160f, Height - 400), new Vector2(0f, 0f), Vector2.One, 1f, 0, listIndex);
			}
		}

		private WallbouncePlayback tutorial;

		private FancyText.Text list;

		private int listIndex;

		private float time;
	}
}
