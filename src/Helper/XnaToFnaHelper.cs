﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static void MainHook(string[] args) {
            // Future pre-Initialize runtime fancyness goes in here.
        }

        public static void Initialize(XnaToFnaGame game) {
            Game = game;

            TextInputEXT.TextInput += KeyboardEvents.CharEntered;

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

        // Only used when --hook-istrialmode / --arr is provided when patching.
        public static bool get_IsTrialMode()
            => Environment.GetEnvironmentVariable("XNATOFNA_ISTRIALMODE") != "0";

        // This needs to be hooked to check for the env vars at runtime.
        public static void ApplyChanges(GraphicsDeviceManager self) {
            string forceFullscreen = Environment.GetEnvironmentVariable("XNATOFNA_DISPLAY_FULLSCREEN");
            if (forceFullscreen == "0")
                self.IsFullScreen = false;
            else if (forceFullscreen == "1")
                self.IsFullScreen = true;

            int forceWidth;
            if (int.TryParse(Environment.GetEnvironmentVariable("XNATOFNA_DISPLAY_WIDTH") ?? "", out forceWidth))
                self.PreferredBackBufferWidth = forceWidth;
            int forceHeight;
            if (int.TryParse(Environment.GetEnvironmentVariable("XNATOFNA_DISPLAY_HEIGHT") ?? "", out forceHeight))
                self.PreferredBackBufferHeight = forceHeight;
            string[] forceSize = (Environment.GetEnvironmentVariable("XNATOFNA_DISPLAY_SIZE") ?? "").Split('x');
            if (forceSize.Length == 2) {
                if (int.TryParse(forceSize[0], out forceWidth))
                    self.PreferredBackBufferWidth = forceWidth;
                if (int.TryParse(forceSize[1], out forceHeight))
                    self.PreferredBackBufferHeight = forceHeight;
            }

            self.ApplyChanges();
        }
        
        public static void PreUpdate(GameTime time) {
            // Don't ask me why some games use Win32 calls instead of Keyboard.GetState()...
            KeyboardEvents.Update();
            // ... or USING SetWindowsHookEx TO GET THE MOUSE STATE instead of Mouse.GetState()...
            MouseEvents.Update();
            // ... or listening to ALL SYSTEM DEVICE CHANGES instead of GamePad.GetState()...
            DeviceEvents.Update();
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
                // TODO: Does a WM_ message exist for this? Why did I put this empty check in here? git blame myself.
            }
        }

    }
}
