using System.Reflection;
using System.Runtime.InteropServices;
using System;
using MonoMod.Utils;

namespace XnaToFna {
    public static class PInvokeHelper {

        private static IntPtr _PThread;
        public static IntPtr PThread {
            get {
                if (_PThread != IntPtr.Zero)
                    return _PThread;

                if (!DynDll.DllMap.ContainsKey("pthread")) {
                    if ((PlatformHelper.Current & Platform.MacOS) == Platform.MacOS)
                        DynDll.DllMap["pthread"] = "libpthread.dylib";
                    else
                        DynDll.DllMap["pthread"] = "libpthread.so";
                }

                return _PThread = DynDll.OpenLibrary("pthread");
            }
        }

        // Windows
        [DllImport("kernel32")]
        private static extern uint GetCurrentThreadId();
        // Linux
        private delegate ulong d_pthread_self();
        private static d_pthread_self pthread_self;

        public static ulong CurrentThreadId {
            get {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    return GetCurrentThreadId();

                return (pthread_self = pthread_self ?? PThread.GetFunction("pthread_self").AsDelegate<d_pthread_self>())?.Invoke() ?? 0;
            }
        }

    }
}
