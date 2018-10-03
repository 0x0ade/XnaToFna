using Microsoft.Xna.Framework;
using MonoMod.InlineRT;
using MonoMod.Utils;
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

            _Decompress = t_orig.GetMethod("Decompress", BindingFlags.Public | BindingFlags.Instance).GetFastDelegate();
        }

        private readonly object _;

        public LzxDecoder(int window) {
            _ = ctor.Invoke(new object[] { window });
        }

        public int Decompress(Stream inData, int inLen, Stream outData, int outLen) {
            return (int) _Decompress(_, inData, inLen, outData, outLen);
        }

        private static FastReflectionDelegate _Decompress;

    }
}
