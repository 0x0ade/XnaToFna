using Microsoft.Xna.Framework;
using MonoMod.Detour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna {
    public class LzxDecoder {

        private readonly static Type t_orig;
        private readonly static Type t_proxy;

        private readonly static ConstructorInfo ctor;

        static LzxDecoder() {
            t_orig = typeof(Game).Assembly.GetType("Microsoft.Xna.Framework.Content.LzxDecoder");
            t_proxy = typeof(LzxDecoder);

            ctor = t_orig.GetConstructor(new Type[] { typeof(int) });

            t_proxy.GetMethod("_Decompress", BindingFlags.NonPublic | BindingFlags.Static).Detour(
                t_orig.GetMethod("Decompress", BindingFlags.Public | BindingFlags.Instance)
            );
        }

        private readonly object _;

        public LzxDecoder(int window) {
            _ = ctor.Invoke(new object[] { window });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decompress(Stream inData, int inLen, Stream outData, int outLen) {
            return _Decompress(_, inData, inLen, outData, outLen);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int _Decompress(object self, Stream inData, int inLen, Stream outData, int outLen) {
            throw new InvalidProgramException("Method not detoured!");
        }

    }
}
