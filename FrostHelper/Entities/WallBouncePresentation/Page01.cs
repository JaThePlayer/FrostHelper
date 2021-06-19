using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper.Entities.WallBouncePresentation
{
    public class Page01 : WallbouncePresentationPage
    {
		public Page01()
		{
			Transition = Transitions.ScaleIn;
			ClearColor = Calc.HexToColor("9fc5e8");
		}

		public override void Added(WallbouncePresentation presentation)
		{
			base.Added(presentation);
		}

		public override IEnumerator Routine()
		{
			Audio.SetAltMusic("event:/new_content/music/lvl10/intermission_powerpoint");
			yield return 1f;
			title = new AreaCompleteTitle(new Vector2(Width / 2f, Height / 2f - 100f), Presentation.GetCleanDialog("PAGE1_TITLE"), 2f, true);
			yield return 1f;
			while (subtitleEase < 1f)
			{
				subtitleEase = Calc.Approach(subtitleEase, 1f, Engine.DeltaTime);
				yield return null;
			}
			yield return 0.1f;
			yield break;
		}

		public override void Update()
		{
			title?.Update();
		}

		public override void Render()
		{
			title?.Render();

			if (subtitleEase > 0f)
			{
				Vector2 position = new Vector2(Width / 2f, Height / 2f + 80f);
				float x = 1f + Ease.BigBackIn(1f - subtitleEase) * 2f;
				float y = 0.25f + Ease.BigBackIn(subtitleEase) * 0.75f;
				ActiveFont.Draw(Presentation.GetCleanDialog("PAGE1_SUBTITLE"), position, new Vector2(0.5f, 0.5f), new Vector2(x, y), Color.Black * 0.8f);
			}
		}

		private AreaCompleteTitle title;

		private float subtitleEase;
	}
}
