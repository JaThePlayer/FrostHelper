using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/BadelineChaserBlockActivator")]
    [Tracked]
    public class BadelineChaserBlockActivator : Solid
    {
        public static MTexture BlockTextureSolid;
        static MTexture[,] nineSliceSolid;
        public static MTexture BlockTextureField;
        static MTexture[,] nineSliceField;

        public static string ActivateSfx = "event:/radleymctuneston_badelineblock_activate";
        public static string DeactivateSfx = "event:/radleymctuneston_badelineblock_deactivate";
        public static string BuzzSfx = "event:/radleymctuneston_badelineblock_buzz";

        public bool Solid;

        public static void Load()
        {
            BlockTextureSolid = GFX.Game["objects/FrostHelper/badelineChaserBlock/activator"];
            nineSliceSolid = new MTexture[3, 3];
            BlockTextureField = GFX.Game["objects/FrostHelper/badelineChaserBlock/activatorfield"];
            nineSliceField = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSliceSolid[i, j] = BlockTextureSolid.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    nineSliceField[i, j] = BlockTextureField.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
        }

        bool lastState;

        public Sprite Emblem;

        /// <summary>
        /// Whether this activator has already done collision checks this frame
        /// </summary>
        public bool DoneCollisionChecks = false;

        public BadelineChaserBlockActivator(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
        {
            Solid = data.Bool("solid", true);
            Collider = new Hitbox(data.Width, data.Height);
            Collidable = Solid;
            
            Emblem = new Sprite(GFX.Game, "objects/FrostHelper/badelineChaserBlock/emblem");
            Emblem.Add("loop", "field");
            //Emblem.Add("pressed", Reversed ? "pressedreverse" : "pressed");
            Emblem.CenterOrigin();
            Emblem.Play("loop");
            Depth = 8990;
        }
        
        public override void Update()
        {
            base.Update();
            
            if (!DoneCollisionChecks)
            {
                bool state = false;
                Entity activatorused = null;
                foreach (var activator in Scene.Tracker.GetEntities<BadelineChaserBlockActivator>())
                {
                    (activator as BadelineChaserBlockActivator).DoneCollisionChecks = true;
                    if (!state)
                    {
                        if ((activator as BadelineChaserBlockActivator).Solid && (activator as BadelineChaserBlockActivator).HasBaddyRider())
                        {
                            activatorused = activator;
                            state = true;
                            break;
                        }

                        foreach (var baddy in Scene.Tracker.GetEntities<BadelineOldsite>())
                        {
                            if ((activator as BadelineChaserBlockActivator).Solid)
                            {
                                // on the side
                                if (baddy.CollideCheck(activator, baddy.Position + (Vector2.UnitX * 2)) || baddy.CollideCheck(activator, baddy.Position + (Vector2.UnitX * -2)))
                                {
                                    // now let's see if badeline is grabbing
                                    Player player = Scene.Tracker.GetEntity<Player>();
                                    Player.ChaserState chaserState;
                                    DynData<BadelineOldsite> data = new DynData<BadelineOldsite>(baddy as BadelineOldsite);
                                    if (player != null && !player.Dead && (bool)data["following"] && player.GetChasePosition(Scene.TimeActive, (float)data["followBehindTime"] + (float)data["followBehindIndexDelay"], out chaserState))
                                    {
                                        string anim = chaserState.Animation.ToLower();
                                        if (anim.Contains("climb") || anim == "dangling" || anim == "wallslide")
                                        {
                                            activatorused = activator;
                                            state = true;
                                            break;
                                        }
                                    }
                                }
                            } else
                            {
                                activator.Collidable = true;
                                if (baddy.CollideCheck(activator))
                                {
                                    activatorused = activator;
                                    state = true;
                                    activator.Collidable = false;
                                    break;
                                }
                                activator.Collidable = false;
                            }
                        }
                    }
                }
                
                foreach (var block in Scene.Tracker.GetEntities<BadelineChaserBlock>())
                    (block as BadelineChaserBlock).SetState(state);
                if (lastState != state)
                {
                    var audio = Audio.Play(state ? ActivateSfx : DeactivateSfx);
                    audio.getVolume(out float volume, out float finalvolume);
                    audio.setVolume(volume * 60f);
                }
                lastState = state;
            }
            
        }

        public bool HasBaddyRider()
        {
            using (List<Entity>.Enumerator enumerator = Scene.Tracker.GetEntities<BadelineOldsite>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.CollideCheck(this, enumerator.Current.Position + (Vector2.UnitY * 2)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Render()
        {
            base.Render();
            //Emblem.Play(Collidable ? "solid" : "pressed");
            DrawBlock(Position, Width, Height, Solid ? nineSliceSolid : nineSliceField, Emblem, Color.White);
            DoneCollisionChecks = false;
        }

        // stolen from SwapBlock
        // TODO: Use more textures for the middle
        private void DrawBlock(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color tint)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, tint);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, tint);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, tint);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, tint);
            for (int i = 1; i < num - 1; i++)
            {
                ninSlice[1, 0].Draw(pos + new Vector2((float)(i * 8), 0f), Vector2.Zero, tint);
                ninSlice[1, 2].Draw(pos + new Vector2((float)(i * 8), height - 8f), Vector2.Zero, tint);
            }
            for (int j = 1; j < num2 - 1; j++)
            {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, (float)(j * 8)), Vector2.Zero, tint);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, (float)(j * 8)), Vector2.Zero, tint);
            }
            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    ninSlice[1, 1].Draw(pos + new Vector2((float)k, (float)l) * 8f, Vector2.Zero, tint);
                }
            }
            if (middle != null)
            {
                middle.Color = tint;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }
    }
}
