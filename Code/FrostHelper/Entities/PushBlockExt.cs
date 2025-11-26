namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/PushBlockExt")]
internal sealed class PushBlockExt : Solid {
    private readonly List<MTexture> stickyTextures;
    private readonly List<MTexture> unpushableTextures;
    private readonly List<MTexture> stickyAndUnpushableTextures;
    private readonly MTexture blockTexture;

    private readonly PushDirections stickyDirections;
    private readonly PushDirections unpushableDirections;

    private readonly char breakDebrisTileset;

    private Vector2 speed;
    private float gravitySpeed;
    private bool lastTouchingGround;
    
    public PushBlockExt(EntityData data, Vector2 offset) 
        : base(data.Position + offset, 32, 32, safe: false) {
        Collidable = true;
        Collider.Center = Vector2.Zero;
        
        stickyDirections = ReadFromData(data, "stickyTop", "stickyBottom", "stickyLeft", "stickyRight");
        unpushableDirections = ReadFromData(data, "unpushableTop", "unpushableBottom", "unpushableLeft", "unpushableRight");
        
        OnDashCollide = OnDashCollideImpl;

        var dir = data.Attr("directory", "objects/canyon/pushblock").AsSpan().TrimEnd('/');

        breakDebrisTileset = data.Char("breakDebrisTileset", '5');
        
        stickyTextures = GFX.Game.GetAtlasSubtextures($"{dir}/stickyGoo");
        unpushableTextures = GFX.Game.GetAtlasSubtextures($"{dir}/unpushable");
        stickyAndUnpushableTextures = GFX.Game.GetAtlasSubtextures($"{dir}/unpushableSticky");
        
        blockTexture = GFX.Game[$"{dir}/idle"];
    }
    
    private static PushDirections ReadFromData(EntityData data, string up, string down, string left, string right) {
        var dir = PushDirections.None;
        
        if (data.Bool(left))
            dir |= PushDirections.Left;
        if (data.Bool(right))
            dir |= PushDirections.Right;
        if (data.Bool(up))
            dir |= PushDirections.Up;
        if (data.Bool(down))
            dir |= PushDirections.Down;

        return dir;
    }

    SideType GetSideType(PushDirections sticky, PushDirections unpushable, PushDirections mask) {
        SideType type = SideType.Normal;

        if (sticky.HasFlag(mask)) {
            type |= SideType.Sticky;
        }
        if (unpushable.HasFlag(mask)) {
            type |= SideType.Unpushable;
        }

        return type;
    }

    List<MTexture>? GetSideTextures(PushDirections sticky, PushDirections unpushable, PushDirections mask) {
        return GetSideType(sticky, unpushable, mask) switch {
            SideType.Sticky => stickyTextures,
            SideType.Unpushable => unpushableTextures,
            SideType.StickyAndUnpushable => stickyAndUnpushableTextures,
            _ => null,
        };
    }

    public override void Update() {
        base.Update();
        
        // Gravity
        var gravity = GravityCheck();
        
        if (gravity)
        {
            if (gravitySpeed < 160f)
            {
                gravitySpeed += 480f * Engine.DeltaTime;
            }
            else
            {
                gravitySpeed = 160f;
            }
        }
        else
        {
            gravitySpeed = 0f;
        }
        
        if (Scene.OnInterval(0.03f)) {
            switch (speed.Y)
            {
                case > 0f:
                    ScrapeParticles(Vector2.UnitY);
                    break;
                case < 0f:
                    ScrapeParticles(-Vector2.UnitY);
                    break;
            }
            switch (speed.X)
            {
                case > 0f:
                    ScrapeParticles(Vector2.UnitX);
                    break;
                case < 0f:
                    ScrapeParticles(-Vector2.UnitX);
                    break;
            }
        }

        var level = SceneAs<Level>();

        if (MoveHExactCollideSolids((int) Math.Floor(speed.X * Engine.DeltaTime), false, null)) {
            speed.X = 0;
        }
        if (speed.X != 0)
        {
            speed.X = Calc.Approach(speed.X, 0, 960f * Engine.DeltaTime);
            if (Left < level.Bounds.Left || Right > level.Bounds.Right)
                speed.X = 0;
        }

        if (MoveVExactCollideSolids((int) Math.Floor((speed.Y + (gravity ? gravitySpeed : 0f)) * Engine.DeltaTime),
                false, null)) {
            speed.Y = 0;
        }
        
        if (speed.Y != 0)
        {
            if (Top < level.Bounds.Top)
                speed.Y = 0;
            if (Top > level.Bounds.Bottom)
                RemoveSelf();
            speed.Y = Calc.Approach(speed.Y, 0, 960f * Engine.DeltaTime);
        }
    }

    private bool GravityCheck() {
        var onGround = CollideCheck<Solid>(Position + Vector2.UnitY);
        
        if (onGround && !lastTouchingGround && gravitySpeed > 0f) {
            Audio.Play("event:/game/general/fallblock_impact", BottomCenter);
            StartShaking(0.2f);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
            LandParticles();
        }
        lastTouchingGround = onGround;
        
        if (onGround) {
            return false;
        }

        if (stickyDirections.HasFlag(PushDirections.Up) && CollideCheck<Solid>(Position - Vector2.UnitY)) {
            return false;
        }
        
        if (stickyDirections.HasFlag(PushDirections.Left) && CollideCheck<Solid>(Position - Vector2.UnitX)) {
            return false;
        }
        
        if (stickyDirections.HasFlag(PushDirections.Right) && CollideCheck<Solid>(Position + Vector2.UnitX)) {
            return false;
        }

        return true;
    }
    
    private void LandParticles()
    {
        int num = 0;
        while (num <= Width)
        {
            if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(num, 1f)))
            {
                SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, (float)-Math.PI / 2);
                float direction;
                if (num < Width / 2f)
                {
                    direction = (float)Math.PI;
                }
                else
                {
                    direction = 0f;
                }
                SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, direction);
            }
            num += 4;
        }
    }
    
    private void ScrapeParticles(Vector2 dir)
    {
        if (dir.X != 0f)
        {
            int x = 0;
            while (x < Width)
            {
                Vector2 bottomPos = new(Left + x, Bottom + 1);
                if (Scene.CollideCheck<Solid>(bottomPos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, bottomPos);
                }
                Vector2 topPos = new(Left + x, Top - 1);
                if (Scene.CollideCheck<Solid>(topPos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, topPos);
                }
                x += 8;
            }
        }
        else
        {
            int y = 0;
            while (y < Height)
            {
                Vector2 leftPos = new(Left - 1, Top + y);
                if (Scene.CollideCheck<Solid>(leftPos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, leftPos);
                }
                Vector2 rightPos = new(Right + 1, Top + y);
                if (Scene.CollideCheck<Solid>(rightPos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, rightPos);
                }
                y += 8;
            }
        }
    }

    public override void Render() {
        base.Render();
        
        blockTexture.DrawCentered(Position);
        
        if (GetSideTextures(stickyDirections, unpushableDirections, PushDirections.Up) is { Count: > 0 } top)
            top[0].DrawCentered(Position, Color.White, 1, 0);
        if (GetSideTextures(stickyDirections, unpushableDirections, PushDirections.Down) is { Count: > 0} bot)
            bot[0].DrawCentered(Position, Color.White, 1, MathHelper.Pi);
        if (GetSideTextures(stickyDirections, unpushableDirections, PushDirections.Left) is { Count: > 0} left)
            left[0].DrawCentered(Position, Color.White, 1, -MathHelper.PiOver2);
        if (GetSideTextures(stickyDirections, unpushableDirections, PushDirections.Right) is { Count: > 0} right)
            right[0].DrawCentered(Position, Color.White, 1, MathHelper.PiOver2);
    }

    private DashCollisionResults OnDashCollideImpl(Player player, Vector2 direction) {
        var dirEnum = direction switch {
            { X: 1 } => PushDirections.Left,
            { X: -1 } => PushDirections.Right,
            { Y: 1 } => PushDirections.Up,
            { Y: -1 } => PushDirections.Down,
            _ => PushDirections.Right,
        };

        if (unpushableDirections.HasFlag(dirEnum)) {
            return DashCollisionResults.NormalCollision;
        }
        
        if (CollideCheck<Solid>(Position + direction)) {
            return DashCollisionResults.NormalCollision;
        }

        speed = direction * 320;
        
        //if (delayBetweenImpactEffect <= 0)
            Audio.Play("event:/game/general/wall_break_stone", Position);
        // delayBetweenImpactEffect = 0.1f;
        
        for (int i = 1; i < 7; i++) {
            var from = player.Center;
            
            switch (direction) {
                case { X: -1 }:
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(TopRight + new Vector2(1, i * 4), breakDebrisTileset).BlastFrom(TopLeft - TopCenter - from));
                    break;
                case { X: 1 }:
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(-1, i * 4), breakDebrisTileset).BlastFrom(TopRight - TopCenter + from));
                    break;
                case { Y: -1 }:
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(BottomLeft + new Vector2(i * 4, 2), breakDebrisTileset).BlastFrom(BottomCenter + Vector2.UnitY));
                    break;
                case { Y: 1 }:
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(i * 4, -2), breakDebrisTileset).BlastFrom(TopCenter - Vector2.UnitY));
                    break;
            }
        }
        
        return DashCollisionResults.Rebound;
    }

    [Flags]
    private enum PushDirections {
        None = 0,
        Left  = 1,
        Right = 2,
        Up    = 4,
        Down  = 8,
    }

    [Flags]
    private enum SideType {
        Normal = 0,
        Sticky = 1,
        Unpushable = 2,
        
        StickyAndUnpushable = Sticky | Unpushable,
    }
}