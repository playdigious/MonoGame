#if false
using Android.Media;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer
        : Java.Lang.Object
        , IDisposable
        , Android.Graphics.SurfaceTexture.IOnFrameAvailableListener
    {
        private GraphicsDevice graphicsDevice;

        private int width;
        private int height;

        private MediaExtractor mediaExtractor;

        private volatile bool abort = false;
        private volatile bool gotFirstFrame = false;
        private volatile bool decoderSurfaceTextureFrameAvailable = false;

        private TimeSpan videoDuration = TimeSpan.Zero;
        private long decodedTimestampNanoseconds = 0;

        private int decoderGLTextureName = -1;
        private int decoderGLFramebufferName = -1;
        private byte[] rgbaBuffer;

        private Android.Graphics.SurfaceTexture decoderSurfaceTexture = null;
        private Android.Views.Surface decoderSurface = null;
        private Texture2D gameTexture = null;

        private MediaCodec decoder;
        private Thread decoderThread;
        private object decoderLock = new object();
        private const long DECODER_TIMEOUT_US = 1000000;

        private Stopwatch playbackStopwatch = new Stopwatch();
        private TimeSpan skipFramesUntil = TimeSpan.Zero;

        private void PlatformInitialize()
        {
            graphicsDevice = Game.Instance.GraphicsDevice;
        }

        private static int SelectVideoTrack(MediaExtractor mediaExtractor)
        {
            for (int i = 0; i < mediaExtractor.TrackCount; i++)
            {
                var format = mediaExtractor.GetTrackFormat(i);
                string mime = format.GetString(MediaFormat.KeyMime);
                if (mime.StartsWith("video/"))
                {
                    return i;
                }
            }
            throw new InvalidOperationException("No video track in media");
        }

        private void PlatformPlay()
        {
            var assetFileDescriptor = Game.Activity.Assets.OpenFd(_videoPath);

            mediaExtractor = new MediaExtractor();
            mediaExtractor.SetDataSource(assetFileDescriptor);
            int trackNo = SelectVideoTrack(mediaExtractor);
            mediaExtractor.SelectTrack(trackNo);

            MediaFormat format = mediaExtractor.GetTrackFormat(trackNo);
            string mime = format.GetString(MediaFormat.KeyMime);

            width = format.GetInteger(MediaFormat.KeyWidth);
            height = format.GetInteger(MediaFormat.KeyHeight);
            videoDuration = TimeSpan.FromSeconds(format.GetLong(MediaFormat.KeyDuration) * 1e-6);

            // Set up public texture
            gameTexture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            rgbaBuffer = new byte[width * height * 4];

            // Prime texture to avoid displaying an initial garbage frame
            // if the game attempts to draw the video before the first frame was decoded.
            gameTexture.SetData(rgbaBuffer);

            // Set up decoder texture
            Threading.BlockOnUIThread(GenerateTextureExternalOES);

            // Set up decoder surface
            decoderSurfaceTextureFrameAvailable = false;
            decoderSurfaceTexture = new Android.Graphics.SurfaceTexture(decoderGLTextureName);
            decoderSurfaceTexture.SetOnFrameAvailableListener(this);
            decoderSurface = new Android.Views.Surface(decoderSurfaceTexture);

            // Init decoder
            decoder = MediaCodec.CreateDecoderByType(mime);
            decoder.Configure(format, decoderSurface, null, MediaCodecConfigFlags.None);
            decoder.Start();

            // Init decoder thread
            decoderThread = new Thread(VideoLoop);
            decoderThread.Name = "VideoDecoder";
            decoderThread.Start();
        }

        private void GenerateTextureExternalOES()
        {
            GL.GenTextures(1, out decoderGLTextureName);
            GraphicsExtensions.CheckGLError();
            GL.BindTexture(TextureTarget.TextureExternalOES, decoderGLTextureName);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureExternalOES, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureExternalOES, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureExternalOES, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);  // MUST be ClampToEdge for Huawei devices
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureExternalOES, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GraphicsExtensions.CheckGLError();

            graphicsDevice.framebufferHelper.GenFramebuffer(out decoderGLFramebufferName);
        }

        private void VideoLoop()
        {
            bool inputDone = false;
            bool outputDone = false;

            while (!abort && !outputDone)
            {
#if false
                if (!inputDone && playbackStopwatch.IsRunning)  // Skip frames
                {
                    while (playbackStopwatch.Elapsed.TotalSeconds > (mediaExtractor.SampleTime * 1e-6))
                    {
                        skipFramesUntil = TimeSpan.FromSeconds(mediaExtractor.SampleTime * 1e-6);
                        if (!mediaExtractor.Advance())
                        {
                            inputDone = true;
                            break;
                        }
                    }
                }
#endif

                if (!inputDone)
                {
                    int index = decoder.DequeueInputBuffer(DECODER_TIMEOUT_US);  // if negative, all input buffers are busy

                    if (index >= 0)
                    {
                        inputDone = OnInputBufferAvailable(index);
                    }
                }

                var outputBufferInfo = new MediaCodec.BufferInfo();
                int outputBufferIndex = decoder.DequeueOutputBuffer(outputBufferInfo, DECODER_TIMEOUT_US);

                if (outputBufferIndex >= 0)
                {
                    outputDone = OnOutputBufferAvailable(outputBufferInfo, outputBufferIndex);
                }
            }

            decoder.Stop();
            decoder.Release();
        }

        private bool OnInputBufferAvailable(int index)
        {
            var inputBuffer = decoder.GetInputBuffer(index);

            int sampleSize = mediaExtractor.ReadSampleData(inputBuffer, 0);
            if (sampleSize >= 0)
            {
                decoder.QueueInputBuffer(index, 0, sampleSize, mediaExtractor.SampleTime, 0);
                mediaExtractor.Advance();
                return false;  // input not done yet
            }
            else
            {
                decoder.QueueInputBuffer(index, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                return true;  // input done
            }
        }

        private bool OnOutputBufferAvailable(MediaCodec.BufferInfo info, int index)
        {
            if (info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
            {
                decoder.ReleaseOutputBuffer(index, false);
                return true;  // output done
            }

            var presentationTime = TimeSpan.FromSeconds(info.PresentationTimeUs * 1e-6);

            // If we're skipping frames, don't update the texture
            if (presentationTime < skipFramesUntil)
            {
                decoder.ReleaseOutputBuffer(index, false);
                return false;  // output not done
            }

            bool doRender = info.Size != 0;

            // As soon as we call releaseOutputBuffer, the buffer will be forwarded to
            // SurfaceTexture to convert to a texture. The API doesn't guarantee that the texture
            // will be available before the call returns, so we need to wait for the
            // onFrameAvailable callback to fire.
            decoder.ReleaseOutputBuffer(index, doRender);

            if (!gotFirstFrame)
            {
                playbackStopwatch.Restart();
            }
            else
            {
                // Sleep until presentation time before committing the buffer
                var delta = presentationTime - playbackStopwatch.Elapsed;
                if (delta.Milliseconds > 0)
                {
                    //Debug.WriteLine("Video: sleep " + delta.Milliseconds + " ms before presenting");
                    Thread.Sleep(delta.Milliseconds);
                }
            }

            gotFirstFrame = true;

            return false;  // output not done
        }

        // Must run on main thread!
        private void UpdateTexImage()
        {
            // Save framebuffer
            int lastFB = graphicsDevice.framebufferHelper.BoundFramebuffer;

            // Latch the data
            decoderSurfaceTexture.UpdateTexImage();
            decodedTimestampNanoseconds = decoderSurfaceTexture.Timestamp;

            // Convert to RGBA
            Game.Instance.GraphicsDevice.framebufferHelper.BindFramebuffer(decoderGLFramebufferName);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureExternalOES, decoderGLTextureName, 0);
            GraphicsExtensions.CheckGLError();
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, rgbaBuffer);
            GraphicsExtensions.CheckGLError();

            // Make texture available for consumption by game
            gameTexture.SetData(rgbaBuffer);

            // Restore framebuffer
            Game.Instance.GraphicsDevice.framebufferHelper.BindFramebuffer(lastFB);
        }

        private Texture2D PlatformGetTexture()
        {
            if (decoderSurfaceTextureFrameAvailable)
            {
                UpdateTexImage();

                decoderSurfaceTextureFrameAvailable = false;
            }

            return gameTexture;
        }
        
        private void PlatformGetState(ref MediaState result)
        {
            lock (decoderLock)
            {
                if (abort)
                    result = MediaState.Stopped;
                else if (!gotFirstFrame)  // not ready yet
                    result = MediaState.Paused;
                else if (decoderThread == null || !decoderThread.IsAlive)
                    result = MediaState.Stopped;
                else
                    result = MediaState.Playing;
            }
        }

        private void PlatformPause()
        {
            Debug.WriteLine("TODO: Pause Android video");
        }

        private void PlatformResume()
        {
            Debug.WriteLine("TODO: Resume Android video");
        }

        private void PlatformStop()
        {
            throw new NotImplementedException("Stop on android player");
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            return TimeSpan.FromSeconds(decodedTimestampNanoseconds * 1e-9);
        }

        private TimeSpan PlatformGetDuration()
        {
            return videoDuration;
        }

        private void PlatformDispose(bool disposing)
        {
            if (abort)  // already been here
            {
                return;
            }

            abort = true;

            if (decoderThread != null && decoderThread.IsAlive)
            {
                decoderThread.Join();
                decoderThread = null;
                Debug.WriteLine("Video: Decoder thread joined.");
            }

            if (decoderGLTextureName > 0)
            {
                GL.DeleteTextures(1, ref this.decoderGLTextureName);
                decoderGLTextureName = 0;
            }

            if (decoderGLFramebufferName > 0)
            {
                graphicsDevice.framebufferHelper.DeleteFramebuffer(decoderGLFramebufferName);
                decoderGLFramebufferName = 0;
            }

            if (gameTexture != null)
            {
                gameTexture.Dispose();
                gameTexture = null;
            }
        }

        public void OnFrameAvailable(Android.Graphics.SurfaceTexture surfaceTexture)
        {
            if (abort)
            {
                return;
            }

            decoderSurfaceTextureFrameAvailable = true;
        }
    }
}
#endif
