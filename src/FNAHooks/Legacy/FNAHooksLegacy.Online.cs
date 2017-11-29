using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using MonoMod.Detour;
using MonoMod.InlineRT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    public static partial class ContentHelper {
        // Yo dawg, I heard you like patching...
        public static partial class FNAHooksLegacy {
            public static class Online {

                public static bool Enabled = true;
                private static bool IsHooked = false;

                public static void Hook() {
                    FNAHooksLegacy.Hook(IsHooked, typeof(ContentManager), "OpenStream", ref orig_OpenStream);
                    IsHooked = true;
                }

                public static Stream ForcedStream;
                private delegate Stream d_OpenStream(ContentManager self, string name);
                private static d_OpenStream orig_OpenStream;
                private static Stream OpenStream(ContentManager self, string name) {
                    if (!Enabled)
                        return orig_OpenStream(self, name);

                    bool isXTF = name.StartsWith("///XNATOFNA/");
                    if (isXTF)
                        name = name.Substring(17); // Removes //XNATOFNA/abcd/

                    Stream stream;
                    if (isXTF && ForcedStream != null) {
                        stream = ForcedStream;
                        ForcedStream = stream;
                    } else {
                        stream = orig_OpenStream(self, name);
                    }
                    return stream;
                }

            }
        }
    }
}
