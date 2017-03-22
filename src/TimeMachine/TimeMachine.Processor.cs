using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XnaToFna.TimeMachine.Framework.Graphics;

namespace XnaToFna.TimeMachine {
    public static class TimeMachineProcessor {

        public static void Log(string txt) {
            Console.Write("[XnaToFna] [TimeMachine] [Processor] ");
            Console.WriteLine(txt);
        }

        public static void SetupRelinker(XnaToFnaUtil xtf) {
            MonoModder modder = xtf.Modder;

            // Dynamic map generation - any hooks, proxies or additions.
            foreach (Type type in typeof(OldVertexElement).Assembly.GetTypes()) {
                string typeNameTo = type.FullName;
                if (!typeNameTo.StartsWith("XnaToFna.TimeMachine.Framework."))
                    continue;

                RelinkNameAttribute nameAttrib;

                string typeName;
                if ((nameAttrib = type.GetCustomAttribute<RelinkNameAttribute>()) != null) {
                    typeName = nameAttrib.Name;
                } else {
                    typeName = typeNameTo.Substring(31);
                    if (typeName.StartsWith("Old"))
                        typeName = typeName.Substring(3);
                }

                string typeNameFrom = "Microsoft.Xna.Framework." + typeName;

                if (type.GetCustomAttribute<RelinkTypeAttribute>() != null) {
                    modder.RelinkMap[typeNameFrom] = typeNameTo;
                } else {
                    foreach (MethodInfo method in type.GetMethods()) {
                        modder.Relink(method, typeNameFrom, typeNameTo);
                    }
                    foreach (PropertyInfo property in type.GetProperties()) {
                        modder.Relink(property.GetGetMethod(), typeNameFrom, typeNameTo);
                        modder.Relink(property.GetSetMethod(), typeNameFrom, typeNameTo);
                    }
                }
            }

            // Static map generation - any fine tuning and f.e. per-type namespace remaps.
            modder.RelinkNamespace(
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework",

                "Color"
            );

            modder.RelinkMap["Microsoft.Xna.Framework.Graphics.ResolveTexture2D"] =
                "Microsoft.Xna.Framework.Graphics.RenderTarget2D";
            modder.RelinkMap["Microsoft.Xna.Framework.Graphics.RenderTarget"] =
                "Microsoft.Xna.Framework.Graphics.RenderTarget2D";

        }

        internal static void Relink(this MonoModder modder, MethodInfo method, string typeFrom, string typeTo) {
            if (method == null)
                return;

            RelinkFindableIDAttribute fromID = method.GetCustomAttribute<RelinkFindableIDAttribute>();

            modder.RelinkMap[
                fromID != null ? string.Format(fromID.FindableID, typeFrom, typeTo) :
                method.GetFindableID(simple: true, type: typeFrom)
            ] =
                Tuple.Create(typeTo, method.Name);
        }

        internal static void RelinkNamespace(this MonoModder modder, string from, string to, params string[] types) {
            for (int i = 0; i < types.Length; i++) {
                string type = types[i];
                modder.RelinkMap[$"{from}.{type}"] = $"{to}.{type}";
            }
        }

    }
}
