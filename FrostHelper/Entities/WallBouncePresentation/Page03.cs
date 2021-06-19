using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace FrostHelper.Entities.WallBouncePresentation
{
    class Page03 : WallbouncePresentationPage
    {
		public Page03()
		{
			Transition = Transitions.Blocky;
			ClearColor = Calc.HexToColor("d9ead3");
			
			titleDisplayed = "";
		}

		public override void Added(WallbouncePresentation presentation)
		{
			base.Added(presentation);
			clipArt = presentation.Gfx["moveset"];

			title = Presentation.GetCleanDialog("PAGE3_TITLE");
		}

		public override IEnumerator Routine()
		{
			while (titleDisplayed.Length < title.Length)
			{
				titleDisplayed += title[titleDisplayed.Length].ToString();
				yield return 0.05f;
			}
			yield return PressButton();
			Audio.Play("event:/new_content/game/10_farewell/ppt_wavedash_whoosh");
			while (clipArtEase < 1f)
			{
				clipArtEase = Calc.Approach(clipArtEase, 1f, Engine.DeltaTime);
				yield return null;
			}
			yield return 0.25f;
			infoText = FancyText.Parse(Presentation.GetDialog("PAGE3_INFO"), Width - 240, 32, 1f, new Color?(Color.Black * 0.7f), null);
			yield return PressButton();
			Audio.Play("event:/new_content/game/10_farewell/ppt_its_easy");
			easyText = new AreaCompleteTitle(new Vector2(Width / 2f, Height - 150), Presentation.GetCleanDialog("PAGE3_EASY"), 2f, true);
			yield return 1f;
			yield break;
		}

		public override void Update()
		{
			if (easyText != null)
			{
				easyText.Update();
			}
		}

		public override void Render()
		{
			ActiveFont.DrawOutline(titleDisplayed, new Vector2(128f, 100f), Vector2.Zero, Vector2.One * 1.5f, Color.White, 2f, Color.Black);
			if (clipArtEase > 0f)
			{
				Vector2 scale = Vector2.One * (1f + (1f - clipArtEase) * 3f) * 0.8f;
				float rotation = (1f - clipArtEase) * 8f;
				Color color = Color.White * clipArtEase;
				clipArt.DrawCentered(new Vector2(Width / 2f, Height / 2f - 90f), color, scale, rotation);
			}
			if (infoText != null)
			{
				infoText.Draw(new Vector2(Width / 2f, Height - 350), new Vector2(0.5f, 0f), Vector2.One, 1f, 0, int.MaxValue);
			}
			if (easyText != null)
			{
				easyText.Render();
			}
		}

		private string title;

		private string titleDisplayed;

		private MTexture clipArt;

		private float clipArtEase;

		private FancyText.Text infoText;

		private AreaCompleteTitle easyText;
	}
}
