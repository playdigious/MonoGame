// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;
using MonoGame.Framework.Utilities;

#if IOS
using UIKit;
using CoreGraphics;
using Foundation;
using System.Drawing;
#endif

#if OPENGL
using MonoGame.OpenGL;
using GLPixelFormat = MonoGame.OpenGL.PixelFormat;
using PixelFormat = MonoGame.OpenGL.PixelFormat;

#if ANDROID
using Android.Graphics;
#endif
#endif // OPENGL

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class TextureEOS2D : Texture2D
    {
        public TextureEOS2D(GraphicsDevice graphicsDevice) : base(graphicsDevice, 1, 1, false, SurfaceFormat.Color, SurfaceType.TextureExternalOES, false, 1)
        {

        }

        public int Width
        {
            set
            {
                width = value;
            }
        }

        public int Height
        {
            set
            {
                height = value;
            }
        }
    }
}
