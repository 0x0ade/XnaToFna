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
                    string[] typeNameParts = typeName.Split('.');
                    typeName = typeNameParts[typeNameParts.Length - 1];

                    if (typeName.StartsWith("Old"))
                        typeName = typeName.Substring(3);

                    if (typeNameParts.Length != 1) {
                        StringBuilder typeNameBuilder = new StringBuilder();
                        for (int i = 0; i < typeNameParts.Length - 1; i++)
                            typeNameBuilder.Append(typeNameParts[i]).Append('.');
                        typeNameBuilder.Append(typeName);
                        typeName = typeNameBuilder.ToString();
                    }
                }

                string typeNameFrom = "Microsoft.Xna.Framework." + typeName;

                if (type.GetCustomAttribute<RelinkTypeInTheMiddleAttribute>() != null) {
                    modder.RelinkMap[typeNameFrom] = typeNameTo;
                    foreach (ConstructorInfo ctor in typeof(XnaToFnaGame).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        modder.Relink(ctor, typeNameFrom, typeNameTo);
                    }
                    foreach (MethodInfo method in typeof(XnaToFnaGame).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        modder.Relink(method, typeNameFrom, typeNameTo);
                    }
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        modder.Relink(property.GetGetMethod(), typeNameFrom, typeNameTo);
                        modder.Relink(property.GetSetMethod(), typeNameFrom, typeNameTo);
                    }

                } else if (type.GetCustomAttribute<RelinkTypeAttribute>() != null) {
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

        private static void Relink(this MonoModder modder, ConstructorInfo ctor, string typeFrom, string typeTo) {
            if (ctor == null)
                return;

            RelinkFindableIDAttribute fromID = ctor.GetCustomAttribute<RelinkFindableIDAttribute>();

            StringBuilder builder = new StringBuilder();

            builder.Append("System.Void {0}{1}.ctor");

            if (ctor.ContainsGenericParameters) {
                builder.Append("<");
                Type[] arguments = ctor.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++) {
                    if (i > 0)
                        builder.Append(",");
                    builder.Append(arguments[i].Name);
                }
                builder.Append(">");
            }

            builder.Append("(");

            ParameterInfo[] parameters = ctor.GetParameters();
            for (int i = 0; i < parameters.Length; i++) {
                ParameterInfo parameter = parameters[i];
                if (i > 0)
                    builder.Append(",");

                if (Attribute.IsDefined(parameter, MonoModExt.t_ParamArrayAttribute))
                    builder.Append("...,");

                builder.Append(parameter.ParameterType.FullName);
            }

            builder.Append(")");

            string format = builder.ToString();

            modder.RelinkMap[
                fromID != null ? string.Format(fromID.FindableID, typeFrom, typeTo) :
                string.Format(format, typeFrom, "::")
            ] =
                Tuple.Create(typeTo, string.Format(format, string.Empty, string.Empty));
        }

        private static void Relink(this MonoModder modder, MethodInfo method, string typeFrom, string typeTo) {
            if (method == null)
                return;

            RelinkFindableIDAttribute fromID = method.GetCustomAttribute<RelinkFindableIDAttribute>();

            modder.RelinkMap[
                fromID != null ? string.Format(fromID.FindableID, typeFrom, typeTo) :
                method.GetFindableID(simple: true, type: typeFrom)
            ] =
                Tuple.Create(typeTo, method.Name);
        }

        private static void RelinkNamespace(this MonoModder modder, string from, string to, params string[] types) {
            for (int i = 0; i < types.Length; i++) {
                string type = types[i];
                modder.RelinkMap[$"{from}.{type}"] = $"{to}.{type}";
            }
        }

    }
}
