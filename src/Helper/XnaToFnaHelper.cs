﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    /// <summary>
    /// XnaToFna runtime helping class. Contains some substitutions to common XNA hacks.
    /// </summary>
    public static class XnaToFnaHelper {

        public static XnaToFnaGame Game;

        public static int MaximumGamepadCount;

        public static void Initialize(XnaToFnaGame game) {
            Game = game;

            game.Window.ClientSizeChanged += SDLWindowSizeChanged;

            string maximumGamepadCountStr = Environment.GetEnvironmentVariable(
                "FNA_GAMEPAD_NUM_GAMEPADS"
            );
            if (string.IsNullOrEmpty(maximumGamepadCountStr) ||
                !int.TryParse(maximumGamepadCountStr, out MaximumGamepadCount) ||
                MaximumGamepadCount < 0) {
                MaximumGamepadCount = Enum.GetNames(typeof(PlayerIndex)).Length;
            }
            DeviceEvents.IsGamepadConnected = new bool[MaximumGamepadCount];

            PlatformHook("ApplyWindowChanges");
        }

        public static void Log(string s) {
            Console.Write("[XnaToFnaHelper] ");
            Console.WriteLine(s);
        }

        // The call contains the game window as the instance parameter.
        public static IntPtr GetProxyFormHandle(this GameWindow window) {
            if (GameForm.Instance == null) {
                Log("[ProxyForms] Creating game ProxyForms.GameForm");
                GameForm.Instance = new GameForm();
            }
            return GameForm.Instance.Handle;
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


        public static void SDLWindowSizeChanged(object sender, EventArgs e)
            => GameForm.Instance?.SDLWindowSizeChanged(sender, e);

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

            GameForm.Instance?.SDLWindowChanged(window, clientWidth, clientHeight, wantsFullscreen, screenDeviceName, ref resultDeviceName);

            if (resultDeviceName != screenDeviceName) {

            }
        }

    }
}