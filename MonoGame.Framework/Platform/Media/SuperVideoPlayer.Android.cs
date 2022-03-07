using Android.Media;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer : IDisposable
    {
        private MediaExtractor mediaExtractor;

        private volatile bool abort = false;
        private int lumaBytes;
        private int chromaBytes;
        private TimeSpan videoDuration = TimeSpan.Zero;

        private byte[] yuvBuffer;
        private int bufferedFrame = -1;
        private TimeSpan bufferedFrameTime = TimeSpan.Zero;

        private Texture2D lumaTex;
        private Texture2D chromaTex;
        private int renderedFrame = -1;
        private TimeSpan displayedFrameTime = TimeSpan.Zero;

        private MediaCodec decoder;
        private Thread decoderThread;
        private object decoderLock = new object();
        private const long DECODER_TIMEOUT_US = 1000000;

        private Stopwatch playbackStopwatch = new Stopwatch();

        public object GetLock()
        {
            return decoderLock;
        }

        private void PlatformInitialize()
        {
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
            var device = Game.Instance.GraphicsDevice;
            var assetFileDescriptor = Game.Activity.Assets.OpenFd(_videoPath);

            mediaExtractor = new MediaExtractor();
            mediaExtractor.SetDataSource(assetFileDescriptor);
            int trackNo = SelectVideoTrack(mediaExtractor);
            mediaExtractor.SelectTrack(trackNo);
            var format = mediaExtractor.GetTrackFormat(trackNo);

            int width = format.GetInteger(MediaFormat.KeyWidth);
            int height = format.GetInteger(MediaFormat.KeyHeight);
            videoDuration = TimeSpan.FromSeconds(format.GetLong(MediaFormat.KeyDuration) * 1e-6);
            lumaBytes = width * height;
            chromaBytes = width * height / 2;

            lumaTex = new Texture2D(device, width, height, false, SurfaceFormat.Alpha8);
            chromaTex = new Texture2D(device, width/2, height/2, false, SurfaceFormat.Rg16);
            yuvBuffer = new byte[lumaBytes + chromaBytes];

            // Init luma & chroma so the first frame isn't green
            Array.Fill<byte>(yuvBuffer, 0, 0, lumaBytes);  // Init luma buffer
            Array.Fill<byte>(yuvBuffer, 128, lumaBytes, chromaBytes);  // Init chroma buffer
            lumaTex.SetData(yuvBuffer, 0, lumaBytes);
            chromaTex.SetData(yuvBuffer, lumaBytes, chromaBytes);

            // Init decoder
            decoder = MediaCodec.CreateDecoderByType(format.GetString(MediaFormat.KeyMime));
            // Pass in null surface to configure the codec for ByteBuffer output
            decoder.Configure(format, surface: null, crypto: null, flags: MediaCodecConfigFlags.None);
            decoder.Start();

            // Init decoder thread
            decoderThread = new Thread(VideoLoop);
            decoderThread.Name = "VideoDecoder";
            decoderThread.Start();
        }

        private void VideoLoop()
        {
            bool inputDone = false;
            TimeSpan skipFramesUntil = TimeSpan.Zero;

            while (!abort)
            {
                if (!inputDone)
                {
                    // Skip frames
                    if (playbackStopwatch.IsRunning)
                    {
                        while (playbackStopwatch.Elapsed.TotalSeconds > (mediaExtractor.SampleTime * 1e-6))
                        {
                            skipFramesUntil = TimeSpan.FromSeconds(mediaExtractor.SampleTime * 1e-6);
                            if (!mediaExtractor.Advance())
                            {
                                break;
                            }
                        }
                    }

                    int index = decoder.DequeueInputBuffer(DECODER_TIMEOUT_US);  // if negative, all input buffers are busy

                    if (index >= 0)
                    {
                        var inputBuffer = decoder.GetInputBuffer(index);

                        int sampleSize = mediaExtractor.ReadSampleData(inputBuffer, 0);
                        if (sampleSize >= 0)
                        {
                            decoder.QueueInputBuffer(index, 0, sampleSize, mediaExtractor.SampleTime, 0);
                            mediaExtractor.Advance();
                        }
                        else
                        {
                            decoder.QueueInputBuffer(index, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                            inputDone = true;
                        }
                    }
                }

                {
                    var info = new MediaCodec.BufferInfo();
                    int index = decoder.DequeueOutputBuffer(info, DECODER_TIMEOUT_US);

                    if (index >= 0)
                    {
                        if (info.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
                        {
                            decoder.ReleaseOutputBuffer(index, false);
                            break;
                        }

                        var presentationTime = TimeSpan.FromSeconds(info.PresentationTimeUs * 1e-6);

                        // If we're skipping frames, don't update the texture
                        if (presentationTime < skipFramesUntil)
                        {
                            decoder.ReleaseOutputBuffer(index, false);
                            continue;
                        }

                        if (presentationTime < this.bufferedFrameTime)
                        {
                            Debug.WriteLine("Video: went back in time? " + presentationTime + " < " + this.bufferedFrameTime);
                        }

                        var thisFrame = bufferedFrame + 1;

                        decoder.GetOutputBuffer(index).Get(yuvBuffer);  // can we do bb.Array to skip a copy?
                        decoder.ReleaseOutputBuffer(index, false);

                        if (thisFrame == 0)
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

                        lock (decoderLock)
                        {
                            bufferedFrameTime = presentationTime;
                            bufferedFrame = thisFrame;  // signals that a new frame is ready once different from renderedFrame
                        }
                    }
                }
            }

            decoder.Stop();
            decoder.Release();
        }

        public void GetYUVTextures(out Texture2D lumaOut, out Texture2D chromaOut)
        {
            lock (decoderLock)
            {
                if (renderedFrame != bufferedFrame)  // new frame ready?
                {
                    lumaTex.SetData(yuvBuffer, 0, lumaBytes);
                    chromaTex.SetData(yuvBuffer, lumaBytes, chromaBytes);
                    renderedFrame = bufferedFrame;
                    displayedFrameTime = bufferedFrameTime;
                }
            }

            lumaOut = lumaTex;
            chromaOut = chromaTex;
        }

        private Texture2D PlatformGetTexture()
        {
            throw new InvalidOperationException("On Android, use GetYUVTextures");
        }
        
        private void PlatformGetState(ref MediaState result)
        {
            lock (decoderLock)
            {
                if (bufferedFrame < 0)  // not ready yet
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
            return bufferedFrameTime;
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

            lumaTex.Dispose();
            chromaTex.Dispose();

            lumaTex = null;
            chromaTex = null;
        }
    }
}
