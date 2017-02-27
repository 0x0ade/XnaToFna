using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using XnaToFna.ProxyForms;

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
            foreach (Type type in typeof(Form).Assembly.GetTypes()) {
                string name = type.FullName;

                // Substitute WinForms for ProxyForms
                if (name.StartsWith("XnaToFna.ProxyForms."))
                    Modder.RelinkMap["System.Windows.Forms." + name.Substring(9 + 11)] = name;
                // Some XNA games use DInput... let's just substitute all DInput references with our ProxyDInput.
                else if (name.StartsWith("XnaToFna.ProxyDInput."))
                    Modder.RelinkMap[/* no namespace */ name.Substring(9 + 12)] = name;
            }
        }

        public void PreProcessType(TypeDefinition type) {
            foreach (MethodDefinition method in type.Methods) {
                if (method.HasPInvokeInfo) {
                    // Just check if PInvokeHooks contains the entry point, ignoring the module name, except for its end. What can go wrong?...
                    if (!method.PInvokeInfo.Module.Name.EndsWith("32.dll"))
                        continue;
                    string entryPoint = method.PInvokeInfo.EntryPoint ?? method.Name;
                    if (typeof(PInvokeHooks).GetMethod(entryPoint) != null) {
                        Log($"[PreProcess] [PInvokeHooks] Remapping call to {entryPoint} ({method.GetFindableID(withType: false)})");
                        Modder.RelinkMap[method.GetFindableID()] =
                            Tuple.Create("XnaToFna.PInvokeHooks", method.GetFindableID(withType: false));
                    } else {
                        Log($"[PreProcess] [PInvokeHooks] Found unhooked call to {entryPoint} ({method.GetFindableID(withType: false)})");
                    }

                } else if (method.HasBody) {
                    /* FUCKING UGLY HACK DO NOT I SAY DO NOT USE THIS PROPERLY
                     * *ahem*
                     * """Fix""" some games *cough* DUCK GAME *cough* setting a bool that locks up Draw during content load.
                     * That bool gets set and then unset around call instance !!0 [FNA]Microsoft.Xna.Framework.Content.ContentManager::Load<!!T>(string)
                     * I deserve to be hanged for this.
                     * -ade
                     */
                    if (method.Name == "Draw" && type.BaseType?.FullName == "Microsoft.Xna.Framework.Game") {
                        for (int i = 1; i < method.Body.Instructions.Count; i++) {
                            Instruction instr = method.Body.Instructions[i];
                            if ((instr.OpCode == OpCodes.Brfalse || instr.OpCode == OpCodes.Brfalse_S ||
                                 instr.OpCode == OpCodes.Brtrue || instr.OpCode == OpCodes.Brtrue_S) &&
                                ((instr.Operand as InstructionOffset?)?.Offset < instr.Offset ||
                                 (instr.Operand as Instruction       )?.Offset < instr.Offset)) {
                                // Check if field load / method call contains "load" or "content"
                                string prevStr = instr.Previous.Operand.ToString().ToLowerInvariant();
                                if (prevStr.Contains("load") || prevStr.Contains("content")) {
                                    // Replace previous, possible volatile and this with nop
                                    Log($"[PreProcess] [HACK] Found branch pointing back depending on {prevStr} - noping");
                                    if (instr.Previous?.Previous.OpCode == OpCodes.Volatile)
                                        instr.Previous.Previous.OpCode = OpCodes.Nop;
                                    instr.Previous.OpCode = OpCodes.Nop;
                                    instr.Previous.Operand = null;
                                    instr.OpCode = OpCodes.Nop;
                                    instr.Operand = null;
                                }
                            }
                        }
                    }

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
