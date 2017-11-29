using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Mono.Cecil;
using MonoMod;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public class ContentHelperGame : Game {

        public readonly GraphicsDeviceManager GraphicsDeviceManager;

        public Thread GameThread { get; protected set; }

        public readonly Queue<Action> ActionQueue = new Queue<Action>();

        public ContentHelperGame() {
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
        }

        protected override void Initialize() {
            Window.Title = "XnaToFna ContentHelper Game (ignore me!)";
            base.Initialize();
            while (ActionQueue.Count > 0)
                ActionQueue.Dequeue()();
            Exit();
        }

        protected override void Dispose(bool disposing) {
            try {
                base.Dispose(disposing);
            } catch (Exception e) {
                // Fail non-critically.
                Console.WriteLine(GetType().FullName + " failed disposing: " + e);
            }
        }

    }
}
