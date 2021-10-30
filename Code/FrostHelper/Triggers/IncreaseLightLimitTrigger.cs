#if FAILED_INCREASE_LIGHT_LIMIT
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

namespace FrostHelper.Triggers {
    [CustomEntity("FrostHelper/IncreaseLightLimitController")]
    [Tracked]
    public class IncreaseLightLimitTrigger : Entity {
        public int NewLimit;

        public IncreaseLightLimitTrigger(EntityData data, Vector2 offset) : base() {
            NewLimit = data.Int("newLimit", 128);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            (scene as Level).Session.SetCounter(API.API.LightLimitCounter, NewLimit);

            //(scene as Level).Lighting = new();
            //scene.Remove((scene as Level).Lighting);
            //scene.Add((scene as Level).Lighting = new LightingRenderer());
        }



        //[OnLoad]
        public static void Load() {
            IL.Celeste.LightingRenderer.BeforeRender += UnhardcodeMethod;
            IL.Celeste.LightingRenderer.ClearDirtyLights += UnhardcodeMethod;
            IL.Celeste.LightingRenderer.DrawLightGradients += UnhardcodeMethod;
            IL.Celeste.LightingRenderer.DrawLightOccluders += UnhardcodeMethod;
            IL.Celeste.LightingRenderer.ctor += UnhardcodeCtor;
        }

        //[OnUnload]
        public static void Unload() {
            IL.Celeste.LightingRenderer.BeforeRender -= UnhardcodeMethod;
            IL.Celeste.LightingRenderer.ClearDirtyLights -= UnhardcodeMethod;
            IL.Celeste.LightingRenderer.DrawLightGradients -= UnhardcodeMethod;
            IL.Celeste.LightingRenderer.DrawLightOccluders -= UnhardcodeMethod;
            IL.Celeste.LightingRenderer.ctor -= UnhardcodeCtor;
        }

        public static void UnhardcodeCtor(ILContext il) {
            var cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(11520))) {
                cursor.Call(typeof(IncreaseLightLimitTrigger).GetMethod(nameof(ChangeVertsLimit)));
                Console.WriteLine("changed vert limit .ctor");
            }

            cursor.Index = 0;

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(384))) {
                cursor.Call(typeof(IncreaseLightLimitTrigger).GetMethod(nameof(ChangeResultVertsLimit)));
                Console.WriteLine("changed result vert limit .ctor");
            }

            cursor.Index = 0;

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaLightLimit))) {
                cursor.Call(typeof(IncreaseLightLimitTrigger).GetMethod(nameof(ChangeLimit)));
                Console.WriteLine("changed limit .ctor");
            }
        }

        public static void UnhardcodeMethod(ILContext il) {
            var cursor = new ILCursor(il);
            Console.WriteLine(il.Method);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaLightLimit))) {
                cursor.Call(typeof(IncreaseLightLimitTrigger).GetMethod(nameof(ChangeLimit)));
                Console.WriteLine("CHANGED LIMIT!!!!!");
            }
        }

        public static int ChangeResultVertsLimit(int prev) {
            var limit = API.API.GetLightLimit();

            return limit == 0 ? prev : (limit * ResultVertsPerLight);
        }

        public static int ChangeVertsLimit(int prev) {
            var limit = API.API.GetLightLimit();

            return limit == 0 ? prev : (limit * VertexesPerLight);
        }

        public static int ChangeLimit(int prev) {
            var limit = API.API.GetLightLimit();//FrostModule.GetCurrentLevel()?.Session?.GetCounter(API.API.LightLimitCounter) ?? 0;
            return limit == 0 ? prev : limit;
        }

        public const int VanillaLightLimit = 64;
        public const int ResultVertsPerLight = 384 / VanillaLightLimit;
        public const int VertexesPerLight = 11520 / VanillaLightLimit;
    }
}
#endif