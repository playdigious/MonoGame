using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public class AstcSRgba6x6BitmapContent : AstcBitmapContent
    {

        public AstcSRgba6x6BitmapContent(int width, int height)
            : base(width, height)
        {
        }

        public override bool TryGetFormat(out SurfaceFormat format)
        {
            format = SurfaceFormat.SRgba6x6Astc;
            return true;
        }

        public override string ToString()
        {
            return "ASTC SRGBA 6x6 " + Width + "x" + Height;
        }
    }
}
