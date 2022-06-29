using AVFoundation;
using Foundation;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer : IDisposable
    {

        private AVPlayer _avPlayer;
        private AVAsset _avAsset;
        private AVPlayerItem _avPlayerItem;
        private AVPlayerItemVideoOutput _avPlayerItemVideoOutput;
        private Texture2D _lastTexture;
        private CoreVideo.CVPixelBufferAttributes _videoPixelBufferAttributes;

        private void PlatformInitialize()
        {
            _videoPixelBufferAttributes = new CoreVideo.CVPixelBufferAttributes();
            _videoPixelBufferAttributes.PixelFormatType = CoreVideo.CVPixelFormatType.CV32BGRA;
        }

        /// <summary>
        /// Returns texture according to the current video playing
        /// Warning! You have to swizzle R and B channel on the returned texture to get the right color.
        /// </summary>
        /// <returns></returns>
        private Texture2D PlatformGetTexture()
        {
            if (_avPlayer != null && State == MediaState.Playing)
            {
                var output = _avPlayer.CurrentItem.Outputs[0] as AVPlayerItemVideoOutput;
                var cmTime = new CoreMedia.CMTime();
                CoreVideo.CVPixelBuffer pixelBuffer = output.CopyPixelBuffer(_avPlayer.CurrentTime, ref cmTime);

                if (pixelBuffer != null)
                {
                    int bufferWidth = (int)pixelBuffer.Width;
                    int bufferHeight = (int)pixelBuffer.Height;
                    pixelBuffer.Lock(CoreVideo.CVPixelBufferLock.ReadOnly);

                    _lastTexture = new Texture2D(Game.Instance.GraphicsDevice,
                                                bufferWidth,
                                                bufferHeight,
                                                false,
                                                SurfaceFormat.Color);

                    IntPtr pixelBufferPtr = pixelBuffer.BaseAddress;
                    byte[] buffer = new byte[bufferWidth * bufferHeight * 4];
                    Marshal.Copy(pixelBufferPtr, buffer, 0, bufferWidth * bufferHeight * 4);
                    _lastTexture.SetData(buffer);
                }
            }

            if (_lastTexture == null)
            {
                _lastTexture = new Texture2D(Game.Instance.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            }

            return _lastTexture;
        }


        private void PlatformGetState(ref MediaState result)
        {
            // Force stopped state when duration is full
            if (_avPlayer == null)
            {
                return;
            }

            if (PlayPosition == TimeSpan.Zero && Duration == TimeSpan.Zero)
            {
                return;
            }

            if (PlayPosition >= Duration)
            {
                result = MediaState.Stopped;
            }
        }

        private void PlatformPause()
        {
            if (_avPlayer == null) return;

            _avPlayer.Pause();
        }

        private void PlatformResume()
        {
            if (_avPlayer == null) return;

            _avPlayer.Play();
        }

        private void PlatformPlay()
        {
            _avAsset = AVAsset.FromUrl(NSUrl.FromFilename(_videoPath));

            _avPlayerItem = new AVPlayerItem(_avAsset);
            _avPlayerItemVideoOutput = new AVPlayerItemVideoOutput(_videoPixelBufferAttributes);
            _avPlayerItem.AddOutput(_avPlayerItemVideoOutput);
            _avPlayer = new AVPlayer(_avPlayerItem);

            _avPlayer.Play();
        }

        private void PlatformStop()
        {
            if (_avPlayer == null) return;

            _avPlayer.Pause();
            _avPlayer.ReplaceCurrentItemWithPlayerItem(null);
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            if (_avPlayerItem == null) return TimeSpan.Zero;

            return TimeSpan.FromSeconds(_avPlayerItem.CurrentTime.Seconds);
        }

        private TimeSpan PlatformGetDuration()
        {
            if (_avAsset == null) return TimeSpan.Zero;

            return TimeSpan.FromSeconds(_avAsset.Duration.Seconds);
        }

        private void PlatformDispose(bool disposing)
        {
            _avAsset.Dispose();
            _avPlayer.Dispose();
            _avPlayerItem.Dispose();
            _avPlayerItemVideoOutput.Dispose();
        }

        private void PlatformSetVolume(float value)
        {
            if (_avPlayer == null) return;

            _avPlayer.Volume = value;
        }
    }
}
