using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    public static class DeviceEvents {

        public enum Events {
            DBT_CONFIGCHANGECANCELED = 0x0019,
            DBT_CONFIGCHANGED = 0x0018,
            DBT_CUSTOMEVENT = 0x8006,
            DBT_DEVICEARRIVAL = 0x8000,
            DBT_DEVICEQUERYREMOVE = 0x8001,
            DBT_DEVICEQUERYREMOVEFAILED = 0x8002,
            DBT_DEVICEREMOVECOMPLETE = 0x8004,
            DBT_DEVICEREMOVEPENDING = 0x8003,
            DBT_DEVICETYPESPECIFIC = 0x8005,
            DBT_DEVNODES_CHANGED = 0x0007,
            DBT_QUERYCHANGECONFIG = 0x0017,
            DBT_USERDEFINED = 0xFFFF
        }

        public static bool[] IsGamepadConnected = new bool[0];

        public static void DeviceChange(Events e, IntPtr data)
            // Device changes like those affect all windows.
            => PInvoke.CallHooks(Messages.WM_DEVICECHANGE, (IntPtr) e, data, allWindows : true);

        // The games I've seen don't care about what connects / disconnects; they just listen to the message ID.
        public static void GamepadConnected(int i)
            => DeviceChange(Events.DBT_DEVICEARRIVAL, IntPtr.Zero);
        public static void GamepadDisconnected(int i)
            => DeviceChange(Events.DBT_DEVICEREMOVECOMPLETE, IntPtr.Zero);

        public static void Update() {
            for (int i = 0; i < IsGamepadConnected.Length; i++) {
                bool connected = GamePad.GetState((PlayerIndex) i).IsConnected;
                if (connected && !IsGamepadConnected[i])
                    GamepadConnected(i);
                else if (!connected && IsGamepadConnected[i])
                    GamepadDisconnected(i);
                IsGamepadConnected[i] = connected;
            }
        }

    }

    public static partial class PInvokeHooks {
        // None.
    }
}
