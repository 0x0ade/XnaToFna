using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using XnaToFna.ProxyForms;
using System.Xml.Serialization;
using System.Xml;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {

        public static ConstructorInfo m_XmlElement_ctor = typeof(XmlElementAttribute).GetConstructor(new Type[] { typeof(string) });

        public static object Stuff_25;

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
            TypeDefinition baseDef = type.BaseType?.Resolve();

            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasPInvokeInfo)
                    continue;
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
            }

            bool baseHasXmlInclude = baseDef?.HasCustomAttribute("System.Xml.Serialization.XmlIncludeAttribute") ?? false;
            foreach (FieldDefinition field in type.Fields) {
                /* Well, this hack isn't that bad... is it?
                 * """Fix""" some games *cough* STARDEW VALLEY *cough* breaking the XmlSerializer in Mono < 4.4.
                 * This only causes minor side effects affecting cross-platform save compatibility.
                 * NOTE: This hack could be implemented better - check for field / Xml attribs deeper than one base type, check for Xml more attribs, ...
                 * NOTE: This needs to be in the pre-processing pass as XmlElement doesn't work and we're thus renaming the field.
                 * -ade
                 */
                if (FixNewInXml && baseHasXmlInclude && baseDef.HasField(field)) {
                    // HAHA NO.
                    // System.InvalidOperationException: Member 'Furniture.price' hides inherited member 'Object.price', but has different custom attributes.
                    /*
                    Log($"[PreProcess] [HACK!!!] Applying XmlElement on field {field.DeclaringType.FullName}::{field.Name} (hiding base field)");
                    CustomAttribute xmlElemAttrib = new CustomAttribute(field.Module.ImportReference(m_XmlElement_ctor));
                    xmlElemAttrib.ConstructorArguments.Add(new CustomAttributeArgument(field.Module.TypeSystem.String, type.Name + "." + field.Name));
                    field.AddAttribute(xmlElemAttrib);
                    */
                    // Rename the field instead.
                    Log($"[PreProcess] [HACK!!!] Renaming serialized field {field.DeclaringType.FullName}::{field.Name} (hiding base field)");
                    string origName = field.FullName;
                    field.Name = field.Name + "In" + type.Name;
                    Modder.RelinkMap[origName] = Tuple.Create(field.DeclaringType.FullName, field.Name);
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

            TypeDefinition baseDef = type.BaseType?.Resolve();

            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasBody) continue;
                for (int i = 0; i < method.Body.Instructions.Count; i++) {
                    Instruction instr = method.Body.Instructions[i];

                    // Fix XnaToFnaHelper calls still being callvirt calls
                    if (instr.OpCode == OpCodes.Callvirt && ((MethodReference) instr.Operand).DeclaringType.FullName == "XnaToFna.XnaToFnaHelper") {
                        instr.OpCode = OpCodes.Call;
                    }

                    if (DestroyLocks)
                        CheckAndDestroyLock(method, i);

                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                PostProcessType(nested);
        }

        public void CheckAndDestroyLock(MethodDefinition method, int instri) {
            Instruction instr = method.Body.Instructions[instri];

            /* FUCKING UGLY HACK DO NOT I SAY DO NOT USE THIS EVERYWHERE
             * *ahem*
             * """Fix""" some games *cough* DUCK GAME *cough* looping for CONTENT to LOAD.
             * I deserve to be hanged for this.
             * -ade
             */
            if (instri >= 1 &&
                (instr.OpCode == OpCodes.Brfalse || instr.OpCode == OpCodes.Brfalse_S ||
                 instr.OpCode == OpCodes.Brtrue || instr.OpCode == OpCodes.Brtrue_S) &&
                ((Instruction) instr.Operand).Offset < instr.Offset && instr.Previous.Operand != null) {
                // Check if field load / method call contains "load" or "content"
                string name =
                    (instr.Previous.Operand as FieldReference)?.Name ??
                    (instr.Previous.Operand as MethodReference)?.Name ??
                    instr.Previous.Operand.ToString();
                name = name.ToLowerInvariant();
                if (instri - method.Body.Instructions.IndexOf((Instruction) instr.Operand) <= 3 &&
                    name != null && (name.Contains("load") || name.Contains("content"))) {
                    // Replace previous, possible volatile and this with nop
                    Log($"[PreProcess] [HACK!!!] NOPing possible content loading waiting loop in {method.GetFindableID()}");
                    if (instr.Previous?.Previous.OpCode == OpCodes.Volatile)
                        instr.Previous.Previous.OpCode = OpCodes.Nop;
                    instr.Previous.OpCode = OpCodes.Nop;
                    instr.Previous.Operand = null;
                    instr.OpCode = OpCodes.Nop;
                    instr.Operand = null;
                }
            }

            /* OH FOR FUCKS SAKE THIS IS EVEN WORSE
             * *ahem*
             * """Fix""" some games *cough* DUCK GAME *cough* locking up the main thread with a lock while LOADing CONTENT.
             * I deserve to be hanged for this, too.
             * -ade
             */
            if (instri >= 3 && instr.OpCode == OpCodes.Call &&
                ((MethodReference) instr.Operand).GetFindableID() ==
                    "System.Void System.Threading.Monitor::Enter(System.Object,System.Boolean&)") {
                // Check for content / load in context
                if (method.DeclaringType.FullName.ToLowerInvariant().Contains("content") ||
                    method.Name.ToLowerInvariant().Contains("load") || method.Name.ToLowerInvariant().Contains("content")) {
                    // "The input must be false.", MSDN says.
                    Log($"[PreProcess] [HACK!!!] Destroying possible content loading lock in {method.GetFindableID()}");
                    DestroyMonitorLock(method, instri);
                } else {
                    // Check for the previous load field, maximally 4 (dup, st, ld in between) behind.
                    for (int i = instri; 0 < i && instri - 4 <= i; i--) {
                        string name =
                            (method.Body.Instructions[i].Operand as FieldReference)?.Name ??
                            (method.Body.Instructions[i].Operand as MethodReference)?.Name ??
                            method.Body.Instructions[i].Operand?.ToString();
                        name = name?.ToLowerInvariant();
                        if (name != null && (name.Contains("load") || name.Contains("content"))) {
                            // "The input must be false.", MSDN says.
                            Log($"[PreProcess] [HACK!!!] Destroying possible content loading lock in {method.GetFindableID()}");
                            DestroyMonitorLock(method, instri);
                            break;
                        }
                    }
                }
            }
        }

        public void DestroyMonitorLock(MethodDefinition method, int instri) {
            Instruction instr = method.Body.Instructions[instri];
            // Replace the Enter call.
            instr.Operand = Modder.Module.ImportReference(
                Modder.FindTypeDeep("XnaToFna.FakeMonitor").Resolve().FindMethod("System.Void Enter(System.Object,System.Boolean&)")
            );

            // Now find the matching Exit call...
            int depth = 1;
            for (; instri < method.Body.Instructions.Count && depth > 0; instri++) {
                instr = method.Body.Instructions[instri];
                if (instr.OpCode == OpCodes.Call) {
                    string id = ((MethodReference) instr.Operand).GetFindableID();
                    if (id == "System.Void System.Threading.Monitor::Enter(System.Object,System.Boolean&)") {
                        // Found another Enter
                        depth++;
                        continue;
                    } else if (id == "System.Void System.Threading.Monitor::Exit(System.Object)") {
                        depth--;
                        continue;
                    }
                }
            }
            if (depth != 0)
                return; // Whoops!... Let's just leave.

            // Replace the Exit call.
            instr.Operand = Modder.Module.ImportReference(
                Modder.FindTypeDeep("XnaToFna.FakeMonitor").Resolve().FindMethod("System.Void Exit(System.Object)")
            );
        }

    }
}
