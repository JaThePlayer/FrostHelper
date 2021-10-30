using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace FrostHelper {
    [CustomEntity("FrostHelper/SidewaysTempleGate")]
    public class SidewaysTempleGate : Solid {
        public SidewaysTempleGate(Vector2 position, int height, Types type, string spriteName) : base(position, 8f, height, true) {
            holdingWaitTimer = 0.2f;
            Type = type;
            closedHeight = height;
            Add(sprite = GFX.SpriteBank.Create("templegate_" + spriteName));
            sprite.X = Collider.Width / 2f;
            sprite.Play("idle", false, false);
            Add(shaker = new Shaker(false, null));
            Depth = -9000;
            theoGate = spriteName.Equals("theo", StringComparison.InvariantCultureIgnoreCase);
            holdingCheckFrom = Position + new Vector2(Width / 2f, height / 2);
        }

        public SidewaysTempleGate(EntityData data, Vector2 offset) : this(data.Position + offset, data.Height, data.Enum("type", Types.NearestSwitch), data.Attr("sprite", "default")) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (Type == Types.CloseBehindPlayer) {
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (entity != null && entity.Left < Right && entity.Bottom >= Top && entity.Top <= Bottom) {
                    StartOpen();
                    Add(new Coroutine(CloseBehindPlayer(), true));
                }
            } else if (Type == Types.CloseBehindPlayerAlways) {
                StartOpen();
                Add(new Coroutine(CloseBehindPlayer(), true));
            } else if (Type == Types.CloseBehindPlayerAndTheo) {
                StartOpen();
                Add(new Coroutine(CloseBehindPlayerAndTheo(), true));
            } else if (Type == Types.HoldingTheo) {
                if (TheoIsNearby()) {
                    StartOpen();
                }
                Hitbox.Width = 16f;
            } else if (Type == Types.TouchSwitches) {
                Add(new Coroutine(CheckTouchSwitches(), true));
            }
            drawWidth = Math.Max(4f, Height);
        }

        public bool CloseBehindPlayerCheck() {
            Player entity = Scene.Tracker.GetEntity<Player>();
            return entity != null && entity.X < X;
        }

        public void SwitchOpen() {
            sprite.Play("open", false, false);
            Alarm.Set(this, 0.2f, delegate {
                shaker.ShakeFor(0.2f, false);
                Alarm.Set(this, 0.2f, new Action(Open), Alarm.AlarmMode.Oneshot);
            }, Alarm.AlarmMode.Oneshot);
        }

        public void Open() {
            Audio.Play(theoGate ? "event:/game/05_mirror_temple/gate_theo_open" : "event:/game/05_mirror_temple/gate_main_open", Position);
            holdingWaitTimer = 0.2f;
            drawHeightMoveSpeed = 200f;
            drawWidth = Height;
            shaker.ShakeFor(0.2f, false);
            SetHeight(0);
            sprite.Play("open", false, false);
            open = true;
        }

        public void StartOpen() {
            SetHeight(0);
            drawWidth = 4f;
            open = true;
        }

        public void Close() {
            Audio.Play(theoGate ? "event:/game/05_mirror_temple/gate_theo_close" : "event:/game/05_mirror_temple/gate_main_close", Position);
            holdingWaitTimer = 0.2f;
            drawHeightMoveSpeed = 300f;
            drawWidth = Math.Max(4f, Height);
            shaker.ShakeFor(0.2f, false);
            SetHeight(closedHeight);
            sprite.Play("hit", false, false);
            open = false;
        }

        private IEnumerator CloseBehindPlayer() {
            while (true) {
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (!lockState && entity != null && entity.Left > Right + 4f) {
                    break;
                }
                yield return null;
            }
            Close();
            yield break;
        }

        private IEnumerator CloseBehindPlayerAndTheo() {
            while (true) {
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (entity != null && entity.Left > Right + 4f) {
                    TheoCrystal entity2 = Scene.Tracker.GetEntity<TheoCrystal>();
                    if (!lockState && entity2 != null && entity2.Left > Right + 4f) {
                        break;
                    }
                }
                yield return null;
            }
            Close();
            yield break;
        }

        private IEnumerator CheckTouchSwitches() {
            while (!Switch.Check(Scene)) {
                yield return null;
            }
            sprite.Play("open", false, false);
            yield return 0.5f;
            shaker.ShakeFor(0.2f, false);
            yield return 0.2f;
            while (lockState) {
                yield return null;
            }
            Open();
            yield break;
        }

        public bool TheoIsNearby() {
            TheoCrystal entity = Scene.Tracker.GetEntity<TheoCrystal>();
            return entity == null || entity.X > X + 10f || Vector2.DistanceSquared(holdingCheckFrom, entity.Center) < (open ? 6400f : 4096f);
        }

        private void SetHeight(int height) {
            if (height < Collider.Height) {
                Collider.Height = height;
                return;
            }
            float y = Y;
            int num = (int) Collider.Height;
            if (Collider.Height < 64f) {
                Y -= 64f - Collider.Height;
                Collider.Height = 64f;
            }
            MoveVExact(height - num);
            Y = y;
            Collider.Height = height;
        }

        public override void Update() {
            base.Update();
            if (Type == Types.HoldingTheo) {
                if (holdingWaitTimer > 0f) {
                    holdingWaitTimer -= Engine.DeltaTime;
                } else if (!lockState) {
                    if (open && !TheoIsNearby()) {
                        Close();
                        Player player = CollideFirst<Player>(Position + new Vector2(8f, 0f));
                        if (player != null) {
                            player.Die(Vector2.Zero, false, true);
                        }
                    } else if (!open && TheoIsNearby()) {
                        Open();
                    }
                }
            }
            float num = Math.Max(4f, Height);
            if (drawWidth != num) {
                lockState = true;
                drawWidth = Calc.Approach(drawWidth, num, drawHeightMoveSpeed * Engine.DeltaTime);
                return;
            }
            lockState = false;
        }

        public override void Render() {
            Draw.Rect(X - 2f, Y - 8f, 14f, 10f, Color.Black);
            sprite.DrawSubrect(new Vector2(0f, Math.Sign(shaker.Value.Y)), new Rectangle((int) (sprite.Width - drawWidth), 0, (int) drawWidth, (int) sprite.Height));
        }

        private const int OpenHeight = 0;

        private const float HoldingWaitTime = 0.2f;

        private const float HoldingOpenDistSq = 4096f;

        private const float HoldingCloseDistSq = 6400f;

        private const int MinDrawHeight = 4;

        public Types Type;

        public bool ClaimedByASwitch;

        private bool theoGate;

        private int closedHeight;

        private Sprite sprite;

        private Shaker shaker;

        private float drawWidth;

        private float drawHeightMoveSpeed;

        private bool open;

        private float holdingWaitTimer;

        private Vector2 holdingCheckFrom;

        private bool lockState;

        public enum Types {
            NearestSwitch,
            CloseBehindPlayer,
            CloseBehindPlayerAlways,
            HoldingTheo,
            TouchSwitches,
            CloseBehindPlayerAndTheo
        }
    }
}
