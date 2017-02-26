using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {
        
        public void SetupHelperRelinkMap() {
            // To use XnaToFnaGame properly, the actual game override needs to call XnaToFnaGame:: as "base" instead.
            Modder.RelinkMap["System.Void Microsoft.Xna.Framework.Game::.ctor()"] =
                Tuple.Create("XnaToFna.XnaToFnaGame", "System.Void .ctor()");
            foreach (MethodInfo method in typeof(XnaToFnaGame).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                Modder.RelinkMap[method.GetFindableID(type: "Microsoft.Xna.Framework.Game")] =
                    Tuple.Create("XnaToFna.XnaToFnaGame", method.GetFindableID(withType: false));
            }

            // XNA games expect a WinForms handle. Give it a "proxy" handle instead.
            Modder.RelinkMap["System.IntPtr Microsoft.Xna.Framework.GameWindow::get_Handle()"] =
                Tuple.Create("XnaToFna.XnaToFnaHelper", "System.IntPtr GetProxyFormHandle(Microsoft.Xna.Framework.GameWindow)");

            // Let's just completely wreck everything.
            Modder.RelinkMap["System.Windows.Forms.Control"] = "XnaToFna.Forms.ProxyControl";
            Modder.RelinkMap["System.Windows.Forms.Form"] = "XnaToFna.Forms.ProxyForm";
            Modder.RelinkMap["System.Windows.Forms.FormBorderStyle"] = "XnaToFna.Forms.FormBorderStyle";
            Modder.RelinkMap["System.Windows.Forms.FormWindowState"] = "XnaToFna.Forms.FormWindowState";
        }

        public void PreProcessType(TypeDefinition type) {
            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasPInvokeInfo) continue;
                // Just check if PInvokeHooks contains the entry point, ignoring the module name, except for its end. What can go wrong?...
                if (!method.PInvokeInfo.Module.Name.EndsWith("32.dll"))
                    continue;
                string entryPoint = method.PInvokeInfo.EntryPoint ?? method.Name;
                if (typeof(PInvokeHooks).GetMethod(entryPoint) != null) {
                    Log($"[PreProcess] [PInvokeHooks] Remapping {method.GetFindableID()} ({entryPoint})");
                    Modder.RelinkMap[method.GetFindableID()] =
                        Tuple.Create("XnaToFna.PInvokeHooks", method.GetFindableID(withType: false));
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                PreProcessType(nested);
        }

        public void PostProcessType(TypeDefinition type) {

            // Make all Microsoft.Xna.Framework.Games inherit from XnaToFnaGame instead.
            if (type.BaseType?.FullName == "Microsoft.Xna.Framework.Game") {
                type.BaseType = type.Module.ImportReference(typeof(XnaToFnaGame));
            }

            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasBody) continue;
                foreach (Instruction instr in method.Body.Instructions) {

                    // Fix XnaToFnaHelper calls still being callvirt calls
                    if (instr.OpCode == OpCodes.Callvirt && ((MethodReference) instr.Operand).DeclaringType.FullName == "XnaToFna.XnaToFnaHelper") {
                        instr.OpCode = OpCodes.Call;
                    }

                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                PostProcessType(nested);
        }

    }
}
