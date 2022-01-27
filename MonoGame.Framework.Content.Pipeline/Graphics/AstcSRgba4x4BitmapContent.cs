using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public class AstcSRgba4x4BitmapContent : AstcBitmapContent
    {

        public AstcSRgba4x4BitmapContent(int width, int height)
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
            return "ASTC SRGBA 4x4 " + Width + "x" + Height;
        }
    }
}
