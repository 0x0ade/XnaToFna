using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.Forms;

namespace XnaToFna {
    /// <summary>
    /// XnaToFna runtime helping class. Contains some substitutions to common XNA hacks.
    /// </summary>
    public static class XnaToFnaHelper {

        public static XnaToFnaGame Game;

        // The call contains the game window as the instance parameter.
        public static IntPtr GetProxyFormHandle(this GameWindow window)
            => IntPtr.Zero;

        public static void Initialize(XnaToFnaGame game) {
            Game = game;

            game.Window.ClientSizeChanged += ProxyControl.Form.SDLWindowSizeChanged;

            PlatformHook("ApplyWindowChanges");
        }

        public static void Log(string s) {
            Console.Write("[XnaToFnaHelper] ");
            Console.WriteLine(s);
        }

        public static T GetService<T>() where T : class
            => (T) Game.Services.GetService(typeof(T));

        public static B GetService<A, B>() where A : class where B : class, A
            => Game.Services.GetService(typeof(A)) as B;

        public static void PlatformHook(string name) {
            Type t_Helper = typeof(XnaToFnaHelper);

            Assembly fna = Assembly.GetAssembly(typeof(Game));
            FieldInfo field = fna.GetType("Microsoft.Xna.Framework.FNAPlatform").GetField(name);

            // Store the original delegate into fna_name.
            t_Helper.GetField($"fna_{name}").SetValue(null, field.GetValue(null));
            // Replace the value with the new method.
            field.SetValue(null, Delegate.CreateDelegate(fna.GetType($"Microsoft.Xna.Framework.FNAPlatform+{name}Func"), t_Helper.GetMethod(name)));
        }

        public static MulticastDelegate fna_ApplyWindowChanges;
        public static void ApplyWindowChanges(
            IntPtr window,
            int clientWidth,
            int clientHeight,
            bool wantsFullscreen,
            string screenDeviceName,
            ref string resultDeviceName
        ) {
            object[] args = { window, clientWidth, clientHeight, wantsFullscreen, screenDeviceName, resultDeviceName };
            fna_ApplyWindowChanges.DynamicInvoke(args);
            resultDeviceName = (string) args[5];

            ProxyControl.Form.SDLWindowChanged(window, clientWidth, clientHeight, wantsFullscreen, screenDeviceName, ref resultDeviceName);
        }

    }
}
