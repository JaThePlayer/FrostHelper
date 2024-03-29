﻿using Celeste.Mod.Entities;

namespace FrostHelper;

[CustomEntity("FrostHelper/CassetteTempoTrigger")]
public class CassetteTempoTrigger : Trigger {
    float Tempo;
    bool ResetOnLeave;
    float prevTempo;
    public CassetteTempoTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Tempo = data.Float("Tempo", 1f);
        ResetOnLeave = data.Bool("ResetOnLeave", false);
    }

    public override void OnEnter(Player player) {
        prevTempo = SceneAs<Level>().CassetteBlockTempo;
        SceneAs<Level>().CassetteBlockTempo = Tempo;
        SetManagerTempo(Tempo);
    }

    public override void OnLeave(Player player) {
        if (ResetOnLeave) {
            SceneAs<Level>().CassetteBlockTempo = prevTempo;
            SetManagerTempo(prevTempo);
        }
    }

    public void SetManagerTempo(float Tempo) {
        CassetteBlockManager manager = Scene.Tracker.GetEntity<CassetteBlockManager>();
        if (manager != null) {
            DynamicData.For(manager).Set("tempoMult", Tempo);
        }
    }
}
