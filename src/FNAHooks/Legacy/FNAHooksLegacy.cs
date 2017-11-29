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

            internal static MethodBase Find(Type type, string name) {
                MethodBase found = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (found != null)
                    return found;

                if (name.StartsWith("get_") || name.StartsWith("set_")) {
                    PropertyInfo prop = type.GetProperty(name.Substring(4), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (name[0] == 'g')
                        found = prop.GetGetMethod(true);
                    else
                        found = prop.GetSetMethod(true);
                }

                return found;
            }

            internal static void Hook<T>(bool isHooked, Type type, string name, ref T trampoline) {
                MethodBase from;
                MethodBase to;
                if (name.StartsWith(".ctor[")) {
                    ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    int index;
                    if (int.TryParse(name.Substring(6, name.Length - 6 - 1), out index))
                        from = ctors[index];
                    else
                        from = ctors[0];
                    to = Find(typeof(T).DeclaringType, "ctor_" + type.Name);
                } else {
                    from = Find(type, name);
                    to = Find(typeof(T).DeclaringType, name);
                }

                // Keeps the detour intact. The JIT likes to revert our changes...
                if (isHooked) {
                    RuntimeDetour.Refresh(from);
                    return;
                }

                T tmp =
                    from
                    .Detour<T>(
                        to
                    );
                if (trampoline == null)
                    trampoline = tmp;
            }

        }
    }
}
