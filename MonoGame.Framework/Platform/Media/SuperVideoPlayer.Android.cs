using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial  class SuperVideoPlayer : IDisposable
    {
        private Texture2D _lastTexture;

        private void PlatformInitialize()
        {
            
        }

        /// <summary>
        /// Returns texture according to the current video playing
        /// Warning! You have to swizzle R and B channel on the returned texture to get the right color.
        /// </summary>
        /// <returns></returns>
        private Texture2D PlatformGetTexture()
        {
            if (_lastTexture == null)
            {
                _lastTexture = new Texture2D(Game.Instance.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            }

            return _lastTexture;
        }

        private void PlatformGetState(ref MediaState result)
        {
        }

        private void PlatformPause()
        {
        }

        private void PlatformResume()
        {
        }

        private void PlatformPlay()
        {
        }

        private void PlatformStop()
        {
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            return TimeSpan.Zero;
        }

        private TimeSpan PlatformGetDuration()
        {
            return TimeSpan.Zero;
        }

        private void PlatformDispose(bool disposing)
        {
        }
    }
}
