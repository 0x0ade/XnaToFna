using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using MonoMod;
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
    public static class XnaToFnaExt {
        
        public static byte[] ReadBytesUntil(this BinaryReader reader, long position)
            => reader.ReadBytes((int) (position - reader.BaseStream.Position));

        public static void KillIfAlive(this Process p) {
            try {
                if (!p?.HasExited ?? false) p.Kill();
            } catch { }
        }

        public static Thread AsyncPipeErr(this Process p, bool nullify = false) {
            Thread t = nullify ?

                new Thread(() => {
                    try { StreamReader err = p.StandardError; while (!p.HasExited) err.ReadLine(); } catch { }
                }) {
                    Name = $"STDERR pipe thread for {p.ProcessName}",
                    IsBackground = true
                } :

                new Thread(() => {
                    try { StreamReader err = p.StandardError; while (!p.HasExited) Console.WriteLine(err.ReadLine()); } catch { }
                }) {
                    Name = $"STDERR pipe thread for {p.ProcessName}",
                    IsBackground = true
                };
            t.Start();
            return t;
        }

        public static Thread AsyncPipeOut(this Process p, bool nullify = false) {
            Thread t = nullify ?

                new Thread(() => {
                    try { StreamReader @out = p.StandardOutput; while (!p.HasExited) @out.ReadLine(); } catch { }
                }) {
                    Name = $"STDOUT pipe thread for {p.ProcessName}",
                    IsBackground = true
                } :

                new Thread(() => {
                    try { StreamReader @out = p.StandardOutput; while (!p.HasExited) Console.WriteLine(@out.ReadLine()); } catch { }
                }) {
                    Name = $"STDOUT pipe thread for {p.ProcessName}",
                    IsBackground = true
                };
            t.Start();
            return t;
        }

        public static T GetTarget<T>(this WeakReference<T> weak) where T : class {
            T t;
            if (weak.TryGetTarget(out t))
                return t;
            return null;
        }

        public static void EmitDefault(this ILProcessor il, TypeReference t, bool stind = false, bool arrayEmpty = true) {
            if (t == null) {
                il.Emit(OpCodes.Ldnull);
                if (stind)
                    il.Emit(OpCodes.Stind_Ref);
                return;
            }

            if (t.MetadataType == MetadataType.Void)
                return;

            if (t.IsArray && arrayEmpty) {
                // TODO: Instead of emitting a null array, emit an empty array.
            }

            int var = 0;
            if (!stind) {
                var = il.Body.Variables.Count;
                il.Body.Variables.Add(new VariableDefinition(t));
                il.Emit(OpCodes.Ldloca, var);
            }
            il.Emit(OpCodes.Initobj, t);
            if (!stind)
                il.Emit(OpCodes.Ldloc, var);
        }

        public static T GetDefault<T>()
            => default(T);

    }
}
