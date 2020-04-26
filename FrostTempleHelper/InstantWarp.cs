using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostTempleHelper
{
    public class InstantWarp : Trigger
    {
        public string destLevel;
        public bool spawnThunder;
        public InstantWarp(EntityData data, Vector2 offset) : base(data, offset)
        {
            destLevel = data.Attr("destinationLevel", "a-0");
            spawnThunder = data.Bool("spawnThunder", false);
        }

        public override void OnEnter(Player player)
        {
            Level level = Engine.Scene as Level;
            level.OnEndOfFrame += delegate
            {
                new Vector2(level.LevelOffset.X + (float)level.Bounds.Width - player.X, player.Y - level.LevelOffset.Y);
                Vector2 levelOffset = level.LevelOffset;
                Vector2 value = player.Position - level.LevelOffset;
                Vector2 value2 = level.Camera.Position - level.LevelOffset;
                Facings facing = player.Facing;
                level.Remove(player);
                level.UnloadLevel();
                level.Session.Dreaming = true;
                level.Session.Level = destLevel;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                level.LoadLevel(Player.IntroTypes.Transition);
                level.Camera.Position = level.LevelOffset + value2;
                level.Session.Inventory.Dashes = 1;
                player.Dashes = Math.Min(player.Dashes, 1);
                level.Add(player);
                player.Position = level.LevelOffset + value;
                player.Facing = facing;
                player.Hair.MoveHairBy(level.LevelOffset - levelOffset);
                if (level.Wipe != null)
                {
                    level.Wipe.Cancel();
                }
                level.Flash(Color.White);
                level.Shake();
                if (spawnThunder)
                {
                    level.Add(new LightningStrike(new Vector2(player.X + 60f, level.Bounds.Bottom - 180), 10, 200f));
                    level.Add(new LightningStrike(new Vector2(player.X + 220f, level.Bounds.Bottom - 180), 40, 200f, 0.25f));
                    Audio.Play("event:/new_content/game/10_farewell/lightning_strike");
                }
            };
        }
    }
}
