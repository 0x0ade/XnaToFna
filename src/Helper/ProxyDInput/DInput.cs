using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.ProxyForms;

namespace XnaToFna.ProxyDInput {
    public static class DInput { // original: public class DInput : IDisposable

        // Either make this fake DInput act as a "proxy" or just fail initializing.
        // Disabled by default.
        public static bool IsProxy = Environment.GetEnvironmentVariable("XTF_PROXY_DINPUT") == "1";

        // DInput doesn't cache, but whatever.
        public static DInputState[] States = new DInputState[0];
        public static DInputState StateDefault = new DInputState();

        public static bool Initialize() {
            if (!IsProxy) {
                XnaToFnaHelper.Log("[ProxyDInput] ProxyDInput disabled by default - 'export XTF_PROXY_DINPUT=1' to enable");
                return false;
            }
            XnaToFnaHelper.Log("[ProxyDInput] Initializing ProxyDInput");

            States = new DInputState[XnaToFnaHelper.MaximumGamepadCount];
            for (int i = 0; i < States.Length; i++)
                States[i] = new DInputState();

            return true;
        }

        public static void Terminate() {
        }

        public static void EnumGamepads() {
        }

        public static void Update() {
            for (int player = 0; player < States.Length; player++) {
                GamePadState stateX = GamePad.GetState((PlayerIndex) player);
                DInputState stateD = States[player];
                stateD.connected = stateX.IsConnected;
                if (!stateD.connected)
                    continue;
                // TODO Finalize DInput mappings

                GamePadThumbSticks sticks = stateX.ThumbSticks;
                stateD.leftX = sticks.Left.X;
                stateD.leftY = sticks.Left.Y;
                stateD.leftZ = 0f; // ???
                stateD.rightX = sticks.Right.X;
                stateD.rightY = sticks.Right.Y;
                stateD.rightZ = 0f; // ???

                GamePadTriggers triggers = stateX.Triggers;
                stateD.slider1 = stateX.Triggers.Left;
                stateD.slider2 = stateX.Triggers.Right;

                GamePadDPad dpad = stateX.DPad;
                stateD.left = dpad.Left == ButtonState.Pressed;
                stateD.right = dpad.Right == ButtonState.Pressed;
                stateD.up = dpad.Up == ButtonState.Pressed;
                stateD.down = dpad.Down == ButtonState.Pressed;

                GamePadButtons buttonsX = stateX.Buttons;
                List<bool> buttonsD = stateD.buttons ?? new List<bool>();
                for (int i = buttonsD.Count; i < 13; i++)
                    buttonsD.Add(false);
                while (buttonsD.Count > 13)
                    buttonsD.RemoveAt(0);

                buttonsD[0] = buttonsX.X == ButtonState.Pressed;
                buttonsD[1] = buttonsX.A == ButtonState.Pressed;
                buttonsD[2] = buttonsX.B == ButtonState.Pressed;
                buttonsD[3] = buttonsX.Y == ButtonState.Pressed;
                buttonsD[4] = buttonsX.LeftShoulder == ButtonState.Pressed;
                buttonsD[5] = buttonsX.RightShoulder == ButtonState.Pressed;
                buttonsD[6] = triggers.Left >= 0.999f;
                buttonsD[7] = triggers.Right >= 0.999f;
                buttonsD[8] = buttonsX.Back == ButtonState.Pressed;
                buttonsD[9] = buttonsX.Start == ButtonState.Pressed;
                buttonsD[10] = buttonsX.BigButton == ButtonState.Pressed; // ???
                buttonsD[11] = buttonsX.LeftStick == ButtonState.Pressed;
                buttonsD[12] = buttonsX.RightStick == ButtonState.Pressed;

                stateD.buttons = buttonsD;
            }
        }

        public static DInputState GetState(int player)
            => player >= States.Length ? StateDefault : States[player];

        public static string GetProductName(int player) {
            if (player >= States.Length)
                return string.Empty;

            // Maybe ask SDL instead?
            return $"ProxyDInput #{player + 1}";
        }

        public unsafe static string GetProductGUID(int player)
            => player >= States.Length ? string.Empty : GamePad.GetGUIDEXT((PlayerIndex) player);

    }
}
