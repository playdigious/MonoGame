using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public class AstcSRgba5x5BitmapContent : AstcBitmapContent
    {

        public AstcSRgba5x5BitmapContent(int width, int height)
            : base(width, height)
        {
        }

        public override bool TryGetFormat(out SurfaceFormat format)
        {
            format = SurfaceFormat.SRgba5x5Astc;
            return true;
        }

        public override string ToString()
        {
            return "ASTC SRGBA 5x5 " + Width + "x" + Height;
        }
    }
}
