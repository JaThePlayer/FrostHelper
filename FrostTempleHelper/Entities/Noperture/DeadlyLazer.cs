using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostTempleHelper.Entities.azcplo1k
{
    [CustomEntity("noperture/deadlyLazer")]
    class uadjaiz : Entity {
        public uadjaiz(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Collider = new Hitbox(4f, data.Height+1f, 2f);
            Add(new PlayerCollider((player) => { player.Die(Vector2.Zero); }));
        }

        public override void Render()
        {
            base.Render();
            Draw.Rect(new Rectangle((int)X+3, (int)Y, 2, (int)Height +1), Color.Red);
            Draw.HollowRect(Collider, Color.Red* 0.33f);

            SceneAs<Level>().Particles.Emit(BadelineOldsite.P_Vanish, 1, new Vector2(X+3f,Y+Height), Vector2.UnitY*3, Color.Red);
        }
    }
}
