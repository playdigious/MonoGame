using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class SuperVideoPlayer : IDisposable
    {

        private MediaState _state;
        private string _videoPath;

        #region Properties

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public TimeSpan PlayPosition
        {
            get
            {
#if IOS || ANDROID
                return PlatformGetPlayPosition();
#else
                return TimeSpan.Zero;
#endif
            }
        }

        public TimeSpan Duration
        {
            get
            {
#if IOS || ANDROID
                return PlatformGetDuration();
#else
                return TimeSpan.Zero;
#endif
            }
        }

        public MediaState State
        {
            get
            {
#if IOS || ANDROID
                PlatformGetState(ref _state);
#endif
                return _state;

            }
        }

        public string VideoPath { get { return _videoPath; } }

#endregion

#region Public API

        public SuperVideoPlayer()
        {
            _state = MediaState.Stopped;
#if IOS || ANDROID
            PlatformInitialize();
#endif
        }

        public Texture2D GetTexture()
        {
#if !IOS && !ANDROID
            throw new NotImplementedException();
#else
            Texture2D texture = PlatformGetTexture();

            if (texture == null)
            {
                throw new InvalidOperationException("Platform returned a null texture");
            }

            return texture;
#endif
        }

        public void Pause()
        {
#if IOS || ANDROID
            PlatformPause();
#endif

            _state = MediaState.Paused;
        }

        public void Play(string path)
        {
            if (path == "")
                throw new ArgumentException("Video path to play is empty");

            if (path == _videoPath)
            {
                var state = State;

                if (state == MediaState.Playing)
                    return;

                if (state == MediaState.Paused)
                {
#if IOS || ANDROID
                    PlatformResume();
#endif
                    return;
                }
            }

            _videoPath = path;
#if IOS || ANDROID
            PlatformPlay();
#endif
            _state = MediaState.Playing;
        }

        public void Resume()
        {
            if (_videoPath == "")
                return;

            var state = State;

            if (state == MediaState.Playing)
                return;

            if (state == MediaState.Stopped)
            {
#if IOS || ANDROID
                PlatformPlay();
#endif
                return;
            }

#if IOS || ANDROID
            PlatformResume();
#endif

            _state = MediaState.Playing;
        }

        public void Stop()
        {
            if (_videoPath == "")
                return;
#if IOS || ANDROID
            PlatformStop();
#endif
            _state = MediaState.Stopped;
        }

#endregion

#region IDisposable Implementation

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
#if IOS || ANDROID
                PlatformDispose(disposing);
#endif
                IsDisposed = true;
            }
        }

#endregion
    }
}
