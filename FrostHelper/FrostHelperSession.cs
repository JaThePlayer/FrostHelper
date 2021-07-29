using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper
{
    public class FrostHelperSession : Celeste.Mod.EverestModuleSession
    {
        public Color LightningColorA { get; set; }
        public Color LightningColorB { get; set; }
        public string LightningFillColor { get; set; }
        public float LightningFillColorMultiplier { get; set; } = 0.1f;
    }
}
