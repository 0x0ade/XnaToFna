using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public static class __PropertyDictionary__ {

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.PropertyDictionary::SetValue(System.String,System.String)")]
        public static void SetValue(/*PropertyDictionary*/ object dictionary, string key, string value) {

        }

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.PropertyDictionary::SetValue(System.String,System.Int32)")]
        public static void SetValue(/*PropertyDictionary*/ object dictionary, string key, int value) {

        }

    }
}
