using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace FrostHelper
{
    public class FrostHelperSession : Celeste.Mod.EverestModuleSession
    {
        public string LightningColorA { get; set; } = null;
        public string LightningColorB { get; set; } = null;
        public string LightningFillColor { get; set; } = "ffffff";
        public float LightningFillColorMultiplier { get; set; } = 0.1f;
    }
}
