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

        public static void SetupRelinkMap(XnaToFnaUtil xtf) {
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

                string nameFrom = "Microsoft.Xna.Framework." + typeName;

                if (type.GetCustomAttribute<RelinkTypeAttribute>() != null) {
                    modder.RelinkMap[nameFrom] = typeNameTo;
                } else {
                    foreach (MethodInfo method in type.GetMethods()) {
                        modder.RelinkMap[method.GetFindableID(simple: true, type: nameFrom)] =
                            Tuple.Create(typeNameTo, method.Name);
                    }
                }
            }

            // Static map generation - any fine tuning and f.e. per-type namespace remaps.
            xtf.RelinkNamespace(
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework",

                "Color"
            );

        }

        public static void RelinkNamespace(this XnaToFnaUtil xtf, string from, string to, params string[] types) {
            MonoModder modder = xtf.Modder;
            for (int i = 0; i < types.Length; i++) {
                string type = types[i];
                modder.RelinkMap[$"{from}.{type}"] = $"{to}.{type}";
            }
        }

    }
}
