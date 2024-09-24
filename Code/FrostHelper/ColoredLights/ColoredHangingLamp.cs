using FrostHelper.Helpers;
using System.Runtime.InteropServices;

// Because this used to be an entity from Colored Lights, make sure the C# type name for old entities stays correct,
// just in case some map relies on it. For new maps, only the Frost Helper placement is available, and those will use
// the type in the FrostHelper namespace, to avoid confusion in places that expose type names to the user.
namespace ColoredLights {
    [CustomEntity("coloredlights/hanginglamp")]
    internal sealed class ColoredHangingLamp : FrostHelper.ColoredHangingLamp {
        public ColoredHangingLamp(EntityData e, Vector2 position) : base(e, position)
        {
        }
    }
}

namespace FrostHelper {
    [CustomEntity("FrostHelper/ColoredHangingLamp")]
    internal class ColoredHangingLamp : Entity
    {
        public readonly int Length;

        private readonly List<Image> _images;

        private readonly BloomPoint _bloom;

        private readonly VertexLight _light;

        private float _speed;

        private float _rotation;

        private float _soundDelay;

        private readonly string _sfxPath;
        
        private readonly SoundSource _sfx;

        private readonly Color _outlineColor;

        private void AddImage(Image img) {
            img.Entity = this;
            _images.Add(img);
        }

        public ColoredHangingLamp(EntityData e, Vector2 position) : base(position + e.Position) {
            _sfxPath = e.Attr("sfx", "event:/game/02_old_site/lantern_hit");
            _outlineColor = e.GetColor("spriteOutlineColor", "000000");
            
            _images = [];
            _speed = 0f;
            _rotation = 0f;
            _soundDelay = 0f;
            Position += Vector2.UnitX * 4f;
            Length = int.Max(8, Math.Max(8, e.Height));
            Depth = 2000;
        
            MTexture mtexture = GFX.Game[e.Attr("sprite", "objects/hanginglamp")];
            var imageColor = e.GetColor("spriteColor", "ffffff");
            
            var middleSubtexture = mtexture.GetSubtexture(0, 8, 8, 8);
            for (int i = 0; i < Length - 8; i += 8)
            {
                AddImage(new Image(middleSubtexture) {
                    Origin = new(4f, -i),
                    Color = imageColor,
                });
            }
        
            // lantern
            AddImage(new Image(mtexture.GetSubtexture(0, 16, 8, 8)) {
                Origin = new(4f, -(Length - 8)),
                Color = imageColor,
            });
        
            // base - needs to be added last as Update depends on that
            AddImage(new Image(mtexture.GetSubtexture(0, 0, 8, 8)) {
                Origin = new(4f, 0f),
                Color = imageColor,
            });
            
            Add(_bloom = new BloomPoint(Vector2.UnitY * (Length - 4), e.Float("bloomAlpha", 1f), e.Float("bloomRadius", 48f)));
            Add(_light = new VertexLight(Vector2.UnitY * (Length - 4), e.GetColor("color", "ffffff"), e.Float("alpha", 1f), e.Int("startFade", 24), e.Int("endFade", 48)));
            Add(_sfx = new SoundSource());
            Collider = new Hitbox(8f, Length, -4f, 0f);
        }

        public override void Update()
        {
            base.Update();
            _soundDelay -= Engine.DeltaTime;
            if (Scene.Tracker.GetEntity<Player>() is {} player && Collider.Collide(player))
            {
                _speed = -player.Speed.X * 0.005f * ((player.Y - Y) / Length);

                if (float.Abs(_speed) < 0.1f)
                {
                    _speed = 0f;
                }
                else if (_soundDelay <= 0f)
                {
                    _sfx.Play(_sfxPath);
                    _soundDelay = 0.25f;
                }
            }
        
            if (_rotation == 0f && _speed == 0f)
                return;
            
            var num = (float.Sign(_rotation) == float.Sign(_speed)) ? 8f : 6f;
            if (float.Abs(_rotation) < 0.5f)
                num *= 0.5f;
            if (float.Abs(_rotation) < 0.25f)
                num *= 0.5f;
            
            var prevRotation = _rotation;
            _speed += -float.Sign(_rotation) * num * Engine.DeltaTime;
            _rotation += _speed * Engine.DeltaTime;
            _rotation = Calc.Clamp(_rotation, -0.4f, 0.4f);

            if (float.Abs(_rotation) < 0.02f && float.Abs(_speed) < 0.2f)
            {
                _rotation = _speed = 0f;
            }
            else if (float.Sign(_rotation) != float.Sign(prevRotation) && _soundDelay <= 0f && float.Abs(_speed) > 0.5f)
            {
                _sfx.Play(_sfxPath);
                _soundDelay = 0.25f;
            }

            // Skip enumerating the last element, as that's the base sprite, which shouldn't get its rotation set!
            var images = CollectionsMarshal.AsSpan(_images);
            for (int i = 0; i < images.Length - 1; i++) {
                images[i].Rotation = _rotation;
            }

            var position = Calc.AngleToVector(_rotation + MathHelper.PiOver2, Length - 4f);
            _bloom.Position = _light.Position = position;
            _sfx.Position = position;
        }
    
        public override void Render() {
            // Intentionally skip base.Render() to dodge hooks etc
        
            var images = CollectionsMarshal.AsSpan(_images);
        
            var bounds = RectangleExt.FromPoints(Position, Position + Calc.AngleToVector(_rotation + MathHelper.PiOver2, Length));
            if (!CameraCullHelper.IsRectangleVisible(bounds, lenience: 8f))
                return;

            if (_outlineColor != default) {
                // OutlineHelper isn't quite accurate for rotated sprites and it doesn't seem fixable unfortunately :/
                if (_rotation == 0f) {
                    foreach (var img in images) {
                        OutlineHelper.RenderOutline(img, _outlineColor, isEightWay: true);
                    }
                } else {
                    foreach (var img in images) {
                        img.DrawOutlineFast(_outlineColor);
                    }
                }
            }

            foreach (var img in images) {
                img.Render();
            }
        }
    }
}