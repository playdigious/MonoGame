// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using MonoGame.Framework.Utilities;

namespace MonoGame.OpenGL
{
    internal static class Angle
    {
        private static bool _resolved;
        private static IntPtr _egl;
        private static IntPtr _gles;

        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return _gles != IntPtr.Zero;
            }
        }

        public static IntPtr GlesLibrary
        {
            get
            {
                Resolve();
                return _gles;
            }
        }

        public static IntPtr EglLibrary
        {
            get
            {
                Resolve();
                return _egl;
            }
        }

        private static void Resolve()
        {
            if (_resolved)
                return;
            _resolved = true;

            if (Runtime.Arch != Arch.SIMULATOR)
                return;

            string frameworks = NSBundle.MainBundle.PrivateFrameworksPath;
            string gles = Path.Combine(frameworks, "libGLESv2.framework", "libGLESv2");
            string egl = Path.Combine(frameworks, "libEGL.framework", "libEGL");
            if (!File.Exists(gles) || !File.Exists(egl))
                return;

            _gles = FuncLoader.LoadLibrary(gles);
            _egl = FuncLoader.LoadLibrary(egl);
            if (_gles == IntPtr.Zero || _egl == IntPtr.Zero)
            {
                _gles = IntPtr.Zero;
                _egl = IntPtr.Zero;
            }
            Console.WriteLine(_gles != IntPtr.Zero ? "MonoGame: ANGLE (Metal) renderer" : "MonoGame: ANGLE failed to load, software OpenGL ES");
        }
    }

    internal class AngleGraphicsContext : IGraphicsContext
    {
        private const int EGL_NONE = 0x3038;
        private const int EGL_RED_SIZE = 0x3024;
        private const int EGL_GREEN_SIZE = 0x3023;
        private const int EGL_BLUE_SIZE = 0x3022;
        private const int EGL_ALPHA_SIZE = 0x3021;
        private const int EGL_DEPTH_SIZE = 0x3025;
        private const int EGL_STENCIL_SIZE = 0x3026;
        private const int EGL_SURFACE_TYPE = 0x3033;
        private const int EGL_WINDOW_BIT = 0x0004;
        private const int EGL_RENDERABLE_TYPE = 0x3040;
        private const int EGL_OPENGL_ES2_BIT = 0x0004;
        private const int EGL_OPENGL_ES3_BIT = 0x0040;
        private const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
        private const int EGL_OPENGL_ES_API = 0x30A0;
        private const int EGL_PLATFORM_ANGLE_ANGLE = 0x3202;
        private const int EGL_PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
        private const int EGL_PLATFORM_ANGLE_TYPE_METAL_ANGLE = 0x3489;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetPlatformDisplayDelegate(int platform, IntPtr nativeDisplay, int[] attribs);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitializeDelegate(IntPtr display, out int major, out int minor);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool BindApiDelegate(int api);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ChooseConfigDelegate(IntPtr display, int[] attribs, IntPtr[] configs, int size, out int count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateContextDelegate(IntPtr display, IntPtr config, IntPtr share, int[] attribs);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateWindowSurfaceDelegate(IntPtr display, IntPtr config, IntPtr window, int[] attribs);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool MakeCurrentDelegate(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SwapBuffersDelegate(IntPtr display, IntPtr surface);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SwapIntervalDelegate(IntPtr display, int interval);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetCurrentContextDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool DestroyDelegate(IntPtr display, IntPtr obj);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetErrorDelegate();

        private static GetPlatformDisplayDelegate _getPlatformDisplay;
        private static InitializeDelegate _initialize;
        private static BindApiDelegate _bindApi;
        private static ChooseConfigDelegate _chooseConfig;
        private static CreateContextDelegate _createContext;
        private static CreateWindowSurfaceDelegate _createWindowSurface;
        private static MakeCurrentDelegate _makeCurrent;
        private static SwapBuffersDelegate _swapBuffers;
        private static SwapIntervalDelegate _swapInterval;
        private static GetCurrentContextDelegate _getCurrentContext;
        private static DestroyDelegate _destroySurface;
        private static DestroyDelegate _destroyContext;
        private static GetErrorDelegate _getError;

        private static IntPtr _display;
        private static IntPtr _config;

        private IntPtr _context;
        private IntPtr _surface;
        private int _swapIntervalValue = 1;

        public AngleGraphicsContext(IntPtr layer, bool depthStencil)
        {
            EnsureDisplay(depthStencil);

            _context = _createContext(_display, _config, IntPtr.Zero, new[] { EGL_CONTEXT_CLIENT_VERSION, 3, EGL_NONE });
            if (_context == IntPtr.Zero)
                _context = _createContext(_display, _config, IntPtr.Zero, new[] { EGL_CONTEXT_CLIENT_VERSION, 2, EGL_NONE });
            if (_context == IntPtr.Zero)
                throw new InvalidOperationException("eglCreateContext failed: 0x" + _getError().ToString("X"));

            _surface = _createWindowSurface(_display, _config, layer, new[] { EGL_NONE });
            if (_surface == IntPtr.Zero)
                throw new InvalidOperationException("eglCreateWindowSurface failed: 0x" + _getError().ToString("X"));
        }

        private static T Load<T>(string name)
        {
            return FuncLoader.LoadFunction<T>(Angle.EglLibrary, name, true);
        }

        private static void EnsureDisplay(bool depthStencil)
        {
            if (_display != IntPtr.Zero)
                return;

            _getPlatformDisplay = Load<GetPlatformDisplayDelegate>("eglGetPlatformDisplayEXT");
            _initialize = Load<InitializeDelegate>("eglInitialize");
            _bindApi = Load<BindApiDelegate>("eglBindAPI");
            _chooseConfig = Load<ChooseConfigDelegate>("eglChooseConfig");
            _createContext = Load<CreateContextDelegate>("eglCreateContext");
            _createWindowSurface = Load<CreateWindowSurfaceDelegate>("eglCreateWindowSurface");
            _makeCurrent = Load<MakeCurrentDelegate>("eglMakeCurrent");
            _swapBuffers = Load<SwapBuffersDelegate>("eglSwapBuffers");
            _swapInterval = Load<SwapIntervalDelegate>("eglSwapInterval");
            _getCurrentContext = Load<GetCurrentContextDelegate>("eglGetCurrentContext");
            _destroySurface = Load<DestroyDelegate>("eglDestroySurface");
            _destroyContext = Load<DestroyDelegate>("eglDestroyContext");
            _getError = Load<GetErrorDelegate>("eglGetError");

            IntPtr display = _getPlatformDisplay(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_METAL_ANGLE, EGL_NONE });
            if (display == IntPtr.Zero || !_initialize(display, out _, out _))
                throw new InvalidOperationException("eglInitialize failed: 0x" + _getError().ToString("X"));
            _bindApi(EGL_OPENGL_ES_API);

            int depth = depthStencil ? 24 : 0;
            int stencil = depthStencil ? 8 : 0;
            IntPtr[] configs = new IntPtr[1];
            int count;
            bool chosen = _chooseConfig(display, new[] {
                    EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                    EGL_DEPTH_SIZE, depth, EGL_STENCIL_SIZE, stencil,
                    EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
                    EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT,
                    EGL_NONE }, configs, 1, out count);
            if (!chosen || count == 0)
                chosen = _chooseConfig(display, new[] {
                    EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                    EGL_DEPTH_SIZE, depth, EGL_STENCIL_SIZE, stencil,
                    EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
                    EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT,
                    EGL_NONE }, configs, 1, out count);
            if (!chosen || count == 0)
                throw new InvalidOperationException("eglChooseConfig failed: 0x" + _getError().ToString("X"));

            _config = configs[0];
            _display = display;
        }

        public bool IsCurrent
        {
            get { return _context != IntPtr.Zero && _getCurrentContext() == _context; }
        }

        public bool IsDisposed
        {
            get { return _context == IntPtr.Zero; }
        }

        public int SwapInterval
        {
            get { return _swapIntervalValue; }
            set
            {
                _swapIntervalValue = value;
                _swapInterval(_display, value);
            }
        }

        public void MakeCurrent(IWindowInfo info)
        {
            if (!_makeCurrent(_display, _surface, _surface, _context))
                throw new InvalidOperationException("eglMakeCurrent failed: 0x" + _getError().ToString("X"));
        }

        public void SwapBuffers()
        {
            if (!_swapBuffers(_display, _surface))
                throw new InvalidOperationException("eglSwapBuffers failed: 0x" + _getError().ToString("X"));
        }

        public void Dispose()
        {
            if (_context == IntPtr.Zero)
                return;
            _makeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_surface != IntPtr.Zero)
                _destroySurface(_display, _surface);
            _destroyContext(_display, _context);
            _surface = IntPtr.Zero;
            _context = IntPtr.Zero;
        }
    }
}
