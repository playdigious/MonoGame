using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public class AstcRgba6x6BitmapContent : AstcBitmapContent
    {

        public AstcRgba6x6BitmapContent(int width, int height)
            : base(width, height)
        {
        }

        public override bool TryGetFormat(out SurfaceFormat format)
        {
            format = SurfaceFormat.Rgba6x6Astc;
            return true;
        }

        public override string ToString()
        {
            return "ASTC ARGB 6x6 " + Width + "x" + Height;
        }
    }
}
