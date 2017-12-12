using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Assumed to be a static class.
    public static class GamerPresenceExtensions {

        private static Type t_PropertyDictionary;
        private static ConstructorInfo ctor_PropertyDictionary;

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.GamerPresenceExtensions::SetPresenceModeString(Microsoft.Xna.Framework.GamerServices.GamerPresence,System.String)")]
        public static void SetPresenceModeString(/*GamerPresence*/ object presence, string value) {
            
        }

        [MonoModHook("Microsoft.Xna.Framework.GamerServices.PropertyDictionary Microsoft.Xna.Framework.GamerServices.GamerPresenceExtensions::SetPresenceModeString(Microsoft.Xna.Framework.GamerServices.GamerPresence)")]
        public static /*PropertyDictionary*/ object GetProperties(/*GamerPresence*/ object presence) {
            // Return an empty dictionary.
            if (t_PropertyDictionary == null) {
                t_PropertyDictionary = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.GamerServices.PropertyDictionary");
                ctor_PropertyDictionary = t_PropertyDictionary.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(Dictionary<string, object>) },
                    null
                );
            }
            return ctor_PropertyDictionary.Invoke(new object[] { new Dictionary<string, object>() });
        }

    }
}
