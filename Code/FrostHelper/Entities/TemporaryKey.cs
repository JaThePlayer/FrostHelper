using MonoMod;

namespace FrostHelper {
    [CustomEntity("FrostHelper/TemporaryKey")]
    public class TemporaryKey : Key {
        #region Hooks
        [OnLoad]
        public static void Load() {
            IL.Celeste.Key.Added += Key_Added;
        }

        [OnUnload]
        public static void Unload() {
            IL.Celeste.Key.Added -= Key_Added;
        }

        private static void Key_Added(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Entity>(nameof(Entity.Add)))) {
                /*
                  ParticleSystem particlesFG = (scene as Level).ParticlesFG;
                  var loc1 = this.shimmerParticles = new ParticleEmitter(particlesFG, Key.P_Shimmer, Vector2.Zero, new Vector2(6f, 6f), 1, 0.1f)
                + if (!ShouldSpawnParticles(this)) {
			        base.Add(loc1);
			        this.shimmerParticles.SimulateCycle();
                + }
                 */
                var label = cursor.DefineLabel();

                cursor.Emit(OpCodes.Pop); // pop the ldloc_1.
                // 'this' is already on the stack
                cursor.EmitCall(ShouldSpawnParticles);
                cursor.Emit(OpCodes.Brfalse, label);

                // restore the stack
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.Emit(OpCodes.Ldloc_1);

                cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<ParticleEmitter>(nameof(ParticleEmitter.SimulateCycle)));
                cursor.MarkLabel(label);
            }
        }

        private static bool ShouldSpawnParticles(Key key) {
            return !(key is TemporaryKey { EmitParticles: false });
        }
        #endregion

        public new bool Turning { get; private set; }

        public readonly bool EmitParticles;

        public TemporaryKey(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, id, data.NodesOffset(offset)) {
            this.follower = Get<Follower>();
            // Create sprite
            DynData<Key> dyndata = new DynData<Key>(this);
            sprite = dyndata.Get<Sprite>("sprite");
            Remove(sprite);
            sprite = new Sprite(GFX.Game, data.Attr("directory", "collectables/FrostHelper/keytemp") + "/");
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("idle", "idle", 0.1f);
            sprite.AddLoop("enter", "enter", 0.1f);
            sprite.Play("idle");
            Add(sprite);
            dyndata.Set("sprite", sprite);
            Follower follower = this.follower;
            follower.OnLoseLeader = (Action) Delegate.Combine(follower.OnLoseLeader, new Action(Dissolve));
            this.follower.PersistentFollow = false; // was false
            Add(new TransitionListener {
                OnOut = f => {
                    StartedUsing = false;
                    if (!IsUsed) {
                        Dissolve();
                    }
                }
            });

            EmitParticles = data.Bool("emitParticles", true);
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            if (scene is Level level) {
                start = Position;
                startLevel = level.Session.Level;
            }
        }

        public override void Update() {
            Session? session = (Scene as Level)?.Session;
            if (IsUsed && !wasUsed) {
                wasUsed = true;
            }
            if (!dissolved && !IsUsed && !base.Turning && session != null && session.Keys.Contains(ID)) {
                session.DoNotLoad.Remove(ID);
                session.Keys.Remove(ID);
                session.UpdateLevelStartDashes();
            }
            base.Update();
        }

        public void Dissolve() {
            if (!(dissolved || IsUsed || base.Turning)) {
                dissolved = true;
                if (follower.Leader != null) {
                    Player player = (follower.Leader.Entity as Player)!;
                    player.StrawberryCollectResetTimer = 2.5f;
                    follower.Leader.LoseFollower(follower);
                }
                Add(new Coroutine(DissolveRoutine(), true));
            }
        }

        private IEnumerator DissolveRoutine() {
            Level level = (Scene as Level)!;
            Session session = level.Session;
            if (session.DoNotLoad.Contains(ID))
                session.DoNotLoad.Remove(ID);
            if (session.Keys.Contains(ID))
                session.Keys.Remove(ID);
            session.UpdateLevelStartDashes();
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 0.5f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            int num;
            for (int i = 0; i < 6; i = num + 1) {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                num = i;
            }
            sprite.Scale = Vector2.Zero;
            Visible = false;

            if (level.Session.Level != startLevel) {
                RemoveSelf();
                yield break;
            }

            yield return 0.3f;

            dissolved = false;
            Audio.Play("event:/game/general/seed_reappear", Position);
            Position = start;
            sprite.Scale = Vector2.One;
            Visible = true;
            Collidable = true;
            level.Displacement.AddBurst(Position, 0.2f, 8f, 28f, 0.2f, null, null);
            yield break;
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            var session = (scene as Level)!.Session;
            if (session.DoNotLoad.Contains(ID))
                session.DoNotLoad.Remove(ID);
            if (session.Keys.Contains(ID))
                session.Keys.Remove(ID);
        }

        private Sprite sprite;

        private Follower follower;

        private Vector2 start;

        private string startLevel;

        private bool dissolved;

        private bool wasUsed;
    }
}

