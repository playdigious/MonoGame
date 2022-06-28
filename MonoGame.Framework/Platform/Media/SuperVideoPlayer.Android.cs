using Java.Lang;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer
    {
        private GraphicsDevice graphicsDevice;

        private bool disposed = false;
        private bool prepared = false;
        private bool surfaceTextureFrameAvailable = false;

        private Android.Graphics.SurfaceTexture surfaceTexture = null;
        private Android.Views.Surface surface = null;
        private TextureOES2D oesTexture = null;
        private RenderTarget2D texture = null;
        private SpriteOESBatch spriteOESBatch = null;
        private int currentPosition = 0;

        private Android.Media.MediaPlayer player;

        private void PlatformInitialize()
        {
            graphicsDevice = Game.Instance.GraphicsDevice;
            player = new Android.Media.MediaPlayer();
            player.Completion += Player_Completion;
            currentPosition = 0;

            // Set up oes texture
            oesTexture = new TextureOES2D(graphicsDevice);

            // Set up sprite batch
            spriteOESBatch = new SpriteOESBatch(graphicsDevice);

            // Set up surface
            surfaceTextureFrameAvailable = false;
            surfaceTexture = new Android.Graphics.SurfaceTexture(oesTexture.glTexture);
            surfaceTexture.FrameAvailable += SurfaceTexture_FrameAvailable;
            surface = new Android.Views.Surface(surfaceTexture);
            player.SetSurface(surface);
        }

        private void PlatformPlay()
        {
            var assetFileDescriptor = Game.Activity.Assets.OpenFd(_videoPath);

            player.SetDataSource(assetFileDescriptor);
            player.Prepare();
            prepared = true;

            // Update oes size
            oesTexture.Width = player.VideoWidth;
            oesTexture.Height = player.VideoHeight;

            // Set up render target
            if (texture == null || texture.Width != player.VideoWidth || texture.Height != player.VideoHeight)
            {
                if (texture != null)
                {
                    texture.Dispose();
                    texture = null;
                }

                texture = new RenderTarget2D(graphicsDevice, player.VideoWidth, player.VideoHeight);
            }

            player.Start();
        }

        private Texture2D PlatformGetTexture()
        {
            if (surfaceTextureFrameAvailable)
            {
                // Latch the data
                surfaceTexture.UpdateTexImage();

                surfaceTextureFrameAvailable = false;

                graphicsDevice.SetRenderTarget(texture);
                spriteOESBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                spriteOESBatch.Draw(oesTexture, new Rectangle(0, 0, texture.Width, texture.Height), Color.White);
                spriteOESBatch.End();
                graphicsDevice.SetRenderTarget(null);
            }

            return texture;
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

        private void PlatformSetVolume(float value)
        {
            if (player != null)
            {
                player.SetVolume(value, value);
            }
        }

        private void PlatformDispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            prepared = false;

            graphicsDevice = null;

            if (player != null)
            {
                player.Completion -= Player_Completion;
                player.Release();
                player.Dispose();
                player = null;
            }

            if (oesTexture != null)
            {
                oesTexture.Dispose();
                oesTexture = null;
            }

            if (surfaceTexture != null)
            {
                surfaceTexture.FrameAvailable -= SurfaceTexture_FrameAvailable;
                surfaceTexture.Release();
                surfaceTexture.Dispose();
                surfaceTexture = null;
            }

            if (surface != null)
            {
                surface.Release();
                surface.Dispose();
                surface = null;
            }
        }

        private void SurfaceTexture_FrameAvailable(object sender, Android.Graphics.SurfaceTexture.FrameAvailableEventArgs e)
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

        private void Player_Completion(object sender, EventArgs e)
        {
            currentPosition = player.Duration;
        }
    }
}
