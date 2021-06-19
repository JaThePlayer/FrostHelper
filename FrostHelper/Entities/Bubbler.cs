using System;
using System.Linq;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using System.Collections;
using Celeste.Mod.Entities;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/Bubbler")]
    public class Bubbler : Entity
    {
        private Vector2[] nodes;
        private readonly bool visible;
        private Sprite sprite;
        private Sprite previewSprite;
        private Color color;

        public Bubbler(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            visible = data.Bool("visible", false);
            Collider = new Hitbox(14f, 14f, 0f, 0f);
            Collider.CenterOrigin();
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            color = ColorHelper.GetColor(data.Attr("color", "White"));

            nodes = data.NodesOffset(offset);

            if (visible)
            {
                Add(sprite = new Sprite(GFX.Game, "objects/FrostHelper/bubble"));
                sprite.AddLoop("idle", "", 0.1f);
                sprite.CenterOrigin();
                sprite.Play("idle", false, false);
                sprite.SetColor(color);
                Add(previewSprite = new Sprite(GFX.Game, "objects/FrostHelper/bubble"));
                previewSprite.AddLoop("idle", "", 0.1f);
                previewSprite.CenterOrigin();
                previewSprite.Play("idle", false, false);
                previewSprite.Position = nodes.Last() - Position;
                previewSprite.SetColor(new Color(color.R, color.G, color.B, 128f) * 0.3f);
            }
        }

        private void OnPlayer(Player player)
        {
            Collidable = false;
            if (nodes != null && nodes.Length >= 2)
            {
                if (visible)
                {
                    sprite.RemoveSelf();
                }
                Add(new Coroutine(NodeRoutine(player), true));
            }
        }

        private IEnumerator NodeRoutine(Player player)
        {
            //yield return 0.3f;
            bool flag = !player.Dead;
            if (flag)
            {
                Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
                player.Dashes = Math.Max(player.Dashes, player.MaxDashes);
                player.StartCassetteFly(nodes[1], nodes[0]);
                // Cursed on linux? D:
                //On.Celeste.Player.CassetteFlyEnd += Player_CassetteFlyEnd;

                On.Celeste.Player.NormalBegin += Player_NormalBegin;
            }
            yield break;
        }

        private void Player_NormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player self)
        {
            orig(self);
            if (visible)
            {
                previewSprite.RemoveSelf();
            }
            On.Celeste.Player.NormalBegin -= Player_NormalBegin;
        }
    }
}