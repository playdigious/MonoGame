using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public class AstcRgba5x5BitmapContent : AstcBitmapContent
    {

        public AstcRgba5x5BitmapContent(int width, int height)
            : base(width, height)
        {
        }

        public override bool TryGetFormat(out SurfaceFormat format)
        {
            format = SurfaceFormat.Rgba5x5Astc;
            return true;
        }

        public override string ToString()
        {
            return "ASTC ARGB 5x5 " + Width + "x" + Height;
        }
    }
}
