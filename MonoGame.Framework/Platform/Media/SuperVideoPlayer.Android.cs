using Java.Lang;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer
        : Java.Lang.Object
        , IDisposable
        , Android.Graphics.SurfaceTexture.IOnFrameAvailableListener
        , Android.Media.MediaPlayer.IOnCompletionListener
    {
        private GraphicsDevice graphicsDevice;

        private bool disposed = false;
        private bool prepared = false;
        private bool surfaceTextureFrameAvailable = false;

        private Android.Graphics.SurfaceTexture surfaceTexture = null;
        private Android.Views.Surface surface = null;
        private TextureEOS2D eosTexture = null;
        private int currentPosition = 0;

        private Android.Media.MediaPlayer player;

        private void PlatformInitialize()
        {
            graphicsDevice = Game.Instance.GraphicsDevice;
            player = new Android.Media.MediaPlayer();
            player.SetOnCompletionListener(this);
            currentPosition = 0;

            // Set up eos texture
            eosTexture = new TextureEOS2D(graphicsDevice);

            // Set up surface
            surfaceTextureFrameAvailable = false;
            surfaceTexture = new Android.Graphics.SurfaceTexture(eosTexture.glTexture);
            surfaceTexture.SetOnFrameAvailableListener(this);
            surface = new Android.Views.Surface(surfaceTexture);
            player.SetSurface(surface);
        }

        private void PlatformPlay()
        {
            var assetFileDescriptor = Game.Activity.Assets.OpenFd(_videoPath);

            player.SetDataSource(assetFileDescriptor);
            player.Prepare();
            prepared = true;

            // Update eos size
            eosTexture.Width = player.VideoWidth;
            eosTexture.Height = player.VideoHeight;

            player.Start();
        }

        private Texture2D PlatformGetTexture()
        {
            if (surfaceTextureFrameAvailable)
            {
                // Latch the data
                surfaceTexture.UpdateTexImage();

                surfaceTextureFrameAvailable = false;
            }

            return eosTexture;
        }
        
        private void PlatformGetState(ref MediaState result)
        {
        }

        private void PlatformPause()
        {
            try
            {
                player.Pause();
            }
            catch (IllegalStateException _)
            {
#if DEBUG
                Console.WriteLine("Player not yet initialized! Not paused.");
#endif
            }
        }

        private void PlatformResume()
        {
            try
            {
                player.Start();
            }
            catch (IllegalStateException _)
            {
#if DEBUG
                Console.WriteLine("Player not yet initialized! Not resumed.");
#endif
            }
        }

        private void PlatformStop()
        {
            try
            {
                player.Stop();
            }
            catch (IllegalStateException _)
            {
#if DEBUG
                Console.WriteLine("Player not yet initialized! Not stopped.");
#endif
            }
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            if (prepared == false)
            {
                return TimeSpan.FromMilliseconds(0);
            }

            return TimeSpan.FromMilliseconds(currentPosition);
        }

        private TimeSpan PlatformGetDuration()
        {
            if (prepared == false)
            {
                return TimeSpan.FromMilliseconds(0);
            }

            return TimeSpan.FromMilliseconds(player.Duration);
        }

        private void PlatformDispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            prepared = false;

            if (player != null)
            {
                player.Release();
                player.Dispose();
                player = null;
            }

            if (eosTexture != null)
            {
                eosTexture.Dispose();
                eosTexture = null;
            }
        }

        /// <inheritdoc/>
        public void OnFrameAvailable(Android.Graphics.SurfaceTexture surfaceTexture)
        {
            if (disposed)
            {
                return;
            }

            // Prevent weird 100ms set after OnCompletion
            if (currentPosition != player.Duration)
            {
                currentPosition = player.CurrentPosition;
            }
            surfaceTextureFrameAvailable = true;
        }

        /// <inheritdoc/>
        public void OnCompletion(Android.Media.MediaPlayer mp)
        {
            currentPosition = player.Duration;
        }
    }
}
