namespace FrostHelper.Colliders;

internal sealed class SpinnerCollider : ColliderList {

    const float HitboxX = -8f, HitboxY = -3f, HitboxW = 16f, HitboxH = 4f;
    const float CircleRadius = 6f;

    internal static Collider[] MakeColliders(float scale) => [
        new Circle(CircleRadius * scale, 0f, 0f),
        new Hitbox(HitboxW * scale, HitboxH * scale, HitboxX * scale, HitboxY * scale)
    ];

    public SpinnerCollider() : base(MakeColliders(1f)) {
    }

    public override bool Collide(Hitbox hitbox) {
        var pos = Entity.Position;
        var hAbsLeft = hitbox.AbsoluteLeft;

        if ((pos.X + HitboxX + HitboxW) <= hAbsLeft)
            return false; // the rectangle extends out horizontally further than the circle, so if the x check fails, we don't need to do anything more

        var hW = hitbox.Width;
        if ((pos.X + HitboxX) >= hAbsLeft + hW)
            return false;  // the rectangle extends out horizontally further than the circle, so if the x check fails, we don't need to do anything more

        var hAbsTop = hitbox.AbsoluteTop;

        var bottomDist = pos.Y + HitboxY + HitboxH - hAbsTop;
        //if ((pos.Y + HitboxY + HitboxH + 5f) <= hAbsTop)
        if (bottomDist <= -5f)
            return false; // the hitbox is outside of both the rectangle AND the circle, no need to do anything more

        var hH = hitbox.Height;
        var topDist = pos.Y + HitboxY - (hAbsTop + hH);
        //if ((pos.Y + HitboxY - 3f) >= hAbsTop + hH)
        if (topDist >= 3f)
            return false; // the hitbox is outside of both the rectangle AND the circle, no need to do anything more

        // finish checking the actual rectangle
        //pos.Y + HitboxY + HitboxH > hAbsTop &&
        //pos.Y + HitboxY < hAbsTop + hH)
        if (bottomDist > 0f && topDist < 0f)
            return true;

        //if (Monocle.Collide.RectToCircle(hAbsLeft, hAbsTop, hW, hH, pos, CircleRadius))
        if (RectToCircle_NoHorizontal(hAbsLeft, hAbsTop, hW, hH, pos, CircleRadius))
            return true;

        return false;
    }

    // version of RectToCircle which does no horizontal checks, as we did those earlier
    private static bool RectToCircle_NoHorizontal(float rX, float rY, float rW, float rH, Vector2 cPosition, float cRadius) {
        if (cPosition.Y >= rY && cPosition.Y < rY + rH) {
            return true;
        }

        if (cPosition.Y < rY) {
            Vector2 lineFrom = new Vector2(rX, rY);
            Vector2 lineTo = new Vector2(rX + rW, rY);
            if (Monocle.Collide.CircleToLine(cPosition, cRadius, lineFrom, lineTo)) {
                return true;
            }
        }

        if (cPosition.Y >= rY + rH) {
            Vector2 lineFrom = new Vector2(rX, rY + rH);
            Vector2 lineTo = new Vector2(rX + rW, rY + rH);
            if (Monocle.Collide.CircleToLine(cPosition, cRadius, lineFrom, lineTo)) {
                return true;
            }
        }

        return false;
    }

    // hitbox checker:
    /*
    public override void Render(Camera camera, Color color) {
        base.Render(camera, color);
        var hitbox = new Hitbox(1, 1);
        for (float x = AbsoluteLeft; x <= AbsoluteRight; x++) {
            for (float y = AbsoluteTop; y <= AbsoluteBottom; y++) {
                hitbox.Left = x; 
                hitbox.Top = y;

                if (Collide(hitbox))
                    Draw.Pixel.Draw(new(x, y), Vector2.Zero, Color.Black * 0.8f);
            }
        }
    }*/
    
}
