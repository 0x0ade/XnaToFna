using Microsoft.Xna.Framework;
using System;

namespace XnaToFna {
    /// <summary>
    /// XnaToFna runtime helping class. Contains some substitutions to common XNA hacks.
    /// </summary>
    public static class XnaToFnaHelper {

        public static Game Game;

        public static XnaToFnaProxyForm ProxyForm = new XnaToFnaProxyForm();
        // The call contains the game window as the instance parameter.
        public static IntPtr GetProxyFormHandle(this GameWindow window)
            => ProxyForm.Handle;

        public static void Initialize(XnaToFnaGame game) {
            Game = game;
        }

        public static T GetService<T>() where T : class
            => (T) Game.Services.GetService(typeof(T));

        public static B GetService<A, B>() where A : class where B : class, A
            => Game.Services.GetService(typeof(A)) as B;

    }
}
