#if OPENGL
// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// The default effect used by SpriteEOSBatch.
    /// </summary>
    public class SpriteEOSEffect : SpriteEffect
    {
        /// <summary>
        /// Creates a new SpriteEOSEffect.
        /// </summary>
        public SpriteEOSEffect(GraphicsDevice device)
            : base(device, EffectResource.SpriteEOSEffect.Bytecode)
        {
        }
    }
}
#endif
