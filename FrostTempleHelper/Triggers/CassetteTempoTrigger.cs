using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace FrostHelper
{
    public class CassetteTempoTrigger : Trigger
    {
        float Tempo;
        bool ResetOnLeave;
        float prevTempo;
        public CassetteTempoTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Tempo = data.Float("Tempo", 1f);
            ResetOnLeave = data.Bool("ResetOnLeave", false);
        }

        public override void OnEnter(Player player)
        {
            prevTempo = SceneAs<Level>().CassetteBlockTempo;
            SceneAs<Level>().CassetteBlockTempo = Tempo;
            SetManagerTempo(Tempo);
        }

        public override void OnLeave(Player player)
        {
            if (ResetOnLeave)
            {
                SceneAs<Level>().CassetteBlockTempo = prevTempo;
                SetManagerTempo(prevTempo);
            }
        }

        public void SetManagerTempo(float Tempo)
        {
            // let me speak to your manager... to change the tempo
            CassetteBlockManager manager = Scene.Tracker.GetEntity<CassetteBlockManager>();
            if (manager != null)
            {
                DynData<CassetteBlockManager> data = new DynData<CassetteBlockManager>(manager);
                data.Set("tempoMult", Tempo);
            }
        }
    }
}
