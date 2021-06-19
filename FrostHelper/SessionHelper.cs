using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FrostHelper
{
    public static class SessionHelper
    {
        public static void WriteColorToSession(Session session, string baseFlag, Color color)
        {
            session.SetCounter(baseFlag, Convert.ToInt32(color.R.ToString("x2") + color.G.ToString("x2") + color.B.ToString("x2"), 16));
            session.SetCounter($"{baseFlag}Alpha", color.A);
            session.SetCounter($"{baseFlag}Set", 1);
        }

        public static Color ReadColorFromSession(Session session, string baseFlag, Color baseColor)
        {
            if (session.GetCounter($"{baseFlag}Set") == 1)
            {
                Color c = Calc.HexToColor(session.GetCounter(baseFlag));
                c.A = (byte)session.GetCounter($"{baseFlag}Alpha");
                return c;
            }
            return baseColor;
        }
    }
}
