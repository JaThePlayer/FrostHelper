using Celeste.Mod.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/TasWatchtower")]
internal sealed class TasWatchtower : Lookout {
    private readonly string _path;
    
    #region Hooks
    private static bool _hooksLoaded;
    
    private static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
       // On.Celeste.Lookout.Interact += LookoutOnInteract;
       IL.Celeste.Lookout.Interact += LookoutOnInteract;
       Everest.Events.Level.OnLoadLevel += LevelOnOnLoadLevel;
    }

    [OnUnload]
    private static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
       // On.Celeste.Lookout.Interact -= LookoutOnInteract;
       IL.Celeste.Lookout.Interact -= LookoutOnInteract;
       Everest.Events.Level.OnLoadLevel -= LevelOnOnLoadLevel;
    }
    
    private static void LookoutOnInteract(ILContext il) {
        var cursor = new ILCursor(il);
        
        // Coroutine coroutine = new Coroutine(LookRoutine(player));
        /*
        IL_0048: ldarg.0
        IL_0049: ldarg.1
        IL_004a: callvirt instance class [mscorlib]System.Collections.IEnumerator Celeste.Lookout::LookRoutine(class Celeste.Player)
                 + ChangeInteract(orig, ldarg.0, ldarg.1)
        IL_004f: ldc.i4.1
        IL_0050: newobj instance void Monocle.Coroutine::.ctor(class [mscorlib]System.Collections.IEnumerator, bool)
        IL_0055: stloc.0
        */

        //if (!cursor.TryGotoNextBestFitLogged(MoveType.Before, x => x.MatchNewobj<Coroutine>()))
        //    return;
        if (!cursor.TryGotoNextBestFitLogged(MoveType.After, x => x.MatchCallvirt<Lookout>(nameof(LookRoutine))))
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitCall(ChangeInteract);
    }

    private static IEnumerator ChangeInteract(IEnumerator orig, Lookout self, Player player) {
        if (self is not TasWatchtower tasWatchtower)
            return orig;

        return tasWatchtower.LookRoutine(player);
    }
    #endregion
    
    public TasWatchtower(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();
        
        _path = data.Attr("path");
    }

    private new IEnumerator LookRoutine(Player player) {
        Level level = SceneAs<Level>();
        SandwichLava sandwichLava = Scene.Entities.FindFirst<SandwichLava>();
        sandwichLava?.Waiting = true;
        if (player.Holding != null)
        {
            player.Drop();
        }
        player.StateMachine.State = 11;
        yield return player.DummyWalkToExact((int)X, walkBackwards: false, 1f, cancelOnFall: true);
        if (Math.Abs(X - player.X) > 4f || player.Dead || !player.OnGround())
		{
			if (!player.Dead)
			{
				player.StateMachine.State = 0;
			}
			yield break;
		}
		Audio.Play("event:/game/general/lookout_use", Position);
		if (player.Facing == Facings.Right)
			sprite.Play(animPrefix + "lookRight");
		else
			sprite.Play(animPrefix + "lookLeft");
        
		var playerSprite = player.Sprite;
		var hair = player.Hair;
		bool visible = false;
		hair.Visible = false;
		playerSprite.Visible = visible;
		yield return 0.2f;
        
		Scene.Add(hud = new Hud());
		hud.TrackMode = nodes != null;
		hud.OnlyY = onlyY;
		nodePercent = 0f;
		node = 0;
		Audio.Play("event:/ui/game/lookout_on");
		while ((hud.Easer = Calc.Approach(hud.Easer, 1f, Engine.DeltaTime * 3f)) < 1f)
		{
			level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
			yield return null;
		}
		float accel = 800f;
		float maxspd = 240f;
		Vector2 cam = level.Camera.Position;
		Vector2 speed = Vector2.Zero;
		Vector2 lastDir = Vector2.Zero;
		Vector2 camStart = level.Camera.Position;
		Vector2 camStartCenter = camStart + new Vector2(160f, 90f);
		while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting)
		{

            if (Input.Dash.Pressed) {
                Input.Dash.ConsumePress();
                var session = level.Session;
                var loader = new LevelLoader(session, session.RespawnPoint);
                Engine.Scene = loader;
                PlayOnLoad = _path;
            }
            
			Vector2 value = Input.Aim.Value;
			if (onlyY)
			{
				value.X = 0f;
			}
			if (Math.Sign(value.X) != Math.Sign(lastDir.X) || Math.Sign(value.Y) != Math.Sign(lastDir.Y))
			{
				Audio.Play("event:/game/general/lookout_move", Position);
			}
			lastDir = value;
			if (sprite.CurrentAnimationID != "lookLeft" && sprite.CurrentAnimationID != "lookRight") {
                var anim = animPrefix + (value.X, value.Y) switch {
                    (0f, 0f) => "looking",
                    (0f, > 0f) => "lookingDown",
                    (0f, _) => "lookingUp",
                    (> 0f, 0f) => "lookingRight",
                    (> 0f, > 0f) => "lookingDownRight",
                    (> 0f, _) => "lookingUpRight",
                    (< 0f, 0f) => "lookingLeft",
                    (< 0f, > 0f) => "lookingDownLeft",
                    (< 0f, _) => "lookingUpLeft",
                    _ => "looking"
                };
                sprite.Play(anim);
            }
			if (nodes == null)
			{
				speed += accel * value * Engine.DeltaTime;
				if (value.X == 0f)
				{
					speed.X = Calc.Approach(speed.X, 0f, accel * 2f * Engine.DeltaTime);
				}
				if (value.Y == 0f)
				{
					speed.Y = Calc.Approach(speed.Y, 0f, accel * 2f * Engine.DeltaTime);
				}
				if (speed.Length() > maxspd)
				{
					speed = speed.SafeNormalize(maxspd);
				}
				Vector2 vector = cam;
				var blockers = Scene.Tracker.GetEntities<LookoutBlocker>();
				cam.X += speed.X * Engine.DeltaTime;
				if (cam.X < (float)level.Bounds.Left || cam.X + 320f > (float)level.Bounds.Right)
				{
					speed.X = 0f;
				}
				cam.X = Calc.Clamp(cam.X, level.Bounds.Left, level.Bounds.Right - 320);
				foreach (Entity item in blockers)
				{
					if (cam.X + 320f > item.Left && cam.Y + 180f > item.Top && cam.X < item.Right && cam.Y < item.Bottom)
					{
						cam.X = vector.X;
						speed.X = 0f;
					}
				}
				cam.Y += speed.Y * Engine.DeltaTime;
				if (cam.Y < (float)level.Bounds.Top || cam.Y + 180f > (float)level.Bounds.Bottom)
				{
					speed.Y = 0f;
				}
				cam.Y = Calc.Clamp(cam.Y, level.Bounds.Top, level.Bounds.Bottom - 180);
				foreach (Entity item2 in blockers)
				{
					if (cam.X + 320f > item2.Left && cam.Y + 180f > item2.Top && cam.X < item2.Right && cam.Y < item2.Bottom)
					{
						cam.Y = vector.Y;
						speed.Y = 0f;
					}
				}
				level.Camera.Position = cam;
			}
			else
			{
				Vector2 vector2 = ((node <= 0) ? camStartCenter : nodes[node - 1]);
				Vector2 vector3 = nodes[node];
				float num = (vector2 - vector3).Length();
				(vector3 - vector2).SafeNormalize();
				if (nodePercent < 0.25f && node > 0)
				{
					Vector2 begin = Vector2.Lerp((node <= 1) ? camStartCenter : nodes[node - 2], vector2, 0.75f);
					Vector2 end = Vector2.Lerp(vector2, vector3, 0.25f);
					SimpleCurve simpleCurve = new SimpleCurve(begin, end, vector2);
					level.Camera.Position = simpleCurve.GetPoint(0.5f + nodePercent / 0.25f * 0.5f);
				}
				else if (nodePercent > 0.75f && node < nodes.Count - 1)
				{
					Vector2 value2 = nodes[node + 1];
					Vector2 begin2 = Vector2.Lerp(vector2, vector3, 0.75f);
					Vector2 end2 = Vector2.Lerp(vector3, value2, 0.25f);
					SimpleCurve simpleCurve2 = new SimpleCurve(begin2, end2, vector3);
					level.Camera.Position = simpleCurve2.GetPoint((nodePercent - 0.75f) / 0.25f * 0.5f);
				}
				else
				{
					level.Camera.Position = Vector2.Lerp(vector2, vector3, nodePercent);
				}
				level.Camera.Position += new Vector2(-160f, -90f);
				nodePercent -= value.Y * (maxspd / num) * Engine.DeltaTime;
				if (nodePercent < 0f)
				{
					if (node > 0)
					{
						node--;
						nodePercent = 1f;
					}
					else
					{
						nodePercent = 0f;
					}
				}
				else if (nodePercent > 1f)
				{
					if (node < nodes.Count - 1)
					{
						node++;
						nodePercent = 0f;
					}
					else
					{
						nodePercent = 1f;
						if (summit)
						{
							break;
						}
					}
				}
				float num2 = 0f;
				float num3 = 0f;
				for (int i = 0; i < nodes.Count; i++)
				{
					float num4 = (((i == 0) ? camStartCenter : nodes[i - 1]) - nodes[i]).Length();
					num3 += num4;
					if (i < node)
					{
						num2 += num4;
					}
					else if (i == node)
					{
						num2 += num4 * nodePercent;
					}
				}
				hud.TrackPercent = num2 / num3;
			}
			yield return null;
		}
		PlayerSprite playerSprite2 = player.Sprite;
		PlayerHair hair2 = player.Hair;
		visible = true;
		hair2.Visible = true;
		playerSprite2.Visible = visible;
		sprite.Play(animPrefix + "idle");
		Audio.Play("event:/ui/game/lookout_off");
		while ((hud.Easer = Calc.Approach(hud.Easer, 0f, Engine.DeltaTime * 3f)) > 0f)
		{
			level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
			yield return null;
		}
		bool atSummitTop = summit && node >= nodes.Count - 1 && nodePercent >= 0.95f;
		if (atSummitTop)
		{
			yield return 0.5f;
			float duration = 3f;
			float approach = 0f;
			Coroutine component = new Coroutine(level.ZoomTo(new Vector2(160f, 90f), 2f, duration));
			Add(component);
			while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting)
			{
				approach = Calc.Approach(approach, 1f, Engine.DeltaTime / duration);
				Audio.SetMusicParam("escape", approach);
				yield return null;
			}
		}
		if ((camStart - level.Camera.Position).Length() > 600f)
		{
			Vector2 was = level.Camera.Position;
			Vector2 direction = (was - camStart).SafeNormalize();
			float approach = (atSummitTop ? 1f : 0.5f);
			new FadeWipe(Scene, wipeIn: false).Duration = approach;
			for (float duration = 0f; duration < 1f; duration += Engine.DeltaTime / approach)
			{
				level.Camera.Position = was - direction * MathHelper.Lerp(0f, 64f, Ease.CubeIn(duration));
				yield return null;
			}
			level.Camera.Position = camStart + direction * 32f;
			new FadeWipe(Scene, wipeIn: true);
		}
		Audio.SetMusicParam("escape", 0f);
		level.ScreenPadding = 0f;
		level.ZoomSnap(Vector2.Zero, 1f);
		Scene.Remove(hud);
		interacting = false;
		player.StateMachine.State = 0;
		yield return null;
    }

    private static string? PlayOnLoad { get; set; }
    
    private static void LevelOnOnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (PlayOnLoad is { } onLoad) {
            PlayOnLoad = null;
            CelesteTASIntegration.LoadTas(onLoad);
        }
    }
}