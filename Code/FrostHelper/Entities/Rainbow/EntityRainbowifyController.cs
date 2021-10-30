using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostHelper {
    [CustomEntity("FrostHelper/EntityRainbowifyController")]
    [Tracked]
    public class EntityRainbowifyController : Entity {
        #region Hooks
        private static ILContext.Manipulator levelRenderManipulator;

        [OnLoad]
        public static void Load() {
            levelRenderManipulator = AllowColorChange((object self) => {
                var controller = (self as Level).Tracker?.GetEntity<EntityRainbowifyController>();
                return controller != null && controller.all;
            }, (object self) => {
                return Vector2.Zero;
            });

            IL.Celeste.Level.Render += levelRenderManipulator;
            //IL.Celeste.Player.Render += AllowColorChangeForEntity;
        }

        [OnUnload]
        public static void Unload() {
            IL.Celeste.Level.Render -= levelRenderManipulator;
            levelRenderManipulator = null;
            //IL.Celeste.Player.Render -= AllowColorChangeForEntity;
        }

        private static ILContext.Manipulator AllowColorChange(Func<object, bool> condition, Func<object, Vector2> positionGetter) {
            return (ILContext il) => {
                ILCursor cursor = new ILCursor(il);

                while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldarg_0); // this
                    cursor.EmitDelegate<Func<object, Color>>((object self) => {
                        if (condition(self)) {
                            return ColorHelper.GetHue(Engine.Scene, positionGetter(self));
                        } else {
                            return Color.White;
                        }

                    });
                    return;
                }
            };
        }

        private static void AllowColorChangeForEntity(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<object, Color>>((object self) => {
                    if ((self as Entity).Get<Rainbowifier>() != null) {
                        return ColorHelper.GetHue(Engine.Scene, (self as Entity).Position);
                    } else {
                        return Color.White;
                    }

                });
                return;
            }
        }
        #endregion

        private bool all;

        private Type[] Types;

        private List<Backdrop> affectedBackdrops;

        public EntityRainbowifyController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            string types = data.Attr("types");
            if (types == "all") {
                all = true;
            } else {
                Types = FrostModule.GetTypes(types);
            }

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (Types is null)
                return;

            foreach (var entity in scene.Entities) {
                if (Types.Contains(entity.GetType()))
                    entity.Add(new Rainbowifier());
            }

            affectedBackdrops = new List<Backdrop>();
            foreach (var backdrop in (scene as Level).Background.Backdrops) {
                if (Types.Contains(backdrop.GetType()))
                    affectedBackdrops.Add(backdrop);
            }
            //RemoveSelf();
        }

        public override void Render() {
            base.Render();
            if (affectedBackdrops is null)
                return;

            foreach (var backdrop in affectedBackdrops) {
                backdrop.Color = ColorHelper.GetHue(Scene, backdrop.Position);
            }
        }
    }
}
