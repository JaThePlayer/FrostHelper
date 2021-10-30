using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace FrostHelper.Components {
    /// <summary>
    /// Combines many images into one component
    /// </summary>
    public class MultiImage : Component {
        public Image[] Images;
        public MultiImage(List<Image> images) : base(false, true) {
            Images = images.ToArray();
        }

        public MultiImage(Image[] images) : base(false, true) {
            Images = images;
        }

        /// <summary>
        /// Sets the color of all images in this MultiImage
        /// </summary>
        /// <param name="color"></param>
        public void SetColorOfAllImages(Color color) {
            for (int i = 0; i < Images.Length; i++) {
                Images[i].SetColor(color);
            }
        }

        private Color addColors(Color a, Color b) {
            return new Color(a.R + b.R, a.G + b.G, a.B + b.B);
        }

        private Color multColors(Color a, Color b) {
            return new Color(a.R * b.R, a.G * b.G, a.B * b.B);
        }

        public override void Render() {
            foreach (var image in Images) {
                image.Texture?.Draw(Entity.Position + image.RenderPosition, image.Origin, image.Color, image.Scale, image.Rotation, image.Effects);
            }
        }
    }
}
