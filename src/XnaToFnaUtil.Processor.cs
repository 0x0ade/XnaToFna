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
using System.Linq;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {

        public static ConstructorInfo m_XmlIgnore_ctor = typeof(XmlIgnoreAttribute).GetConstructor(Type.EmptyTypes);
        public static MethodInfo m_XnaToFnaHelper_PreUpdate = typeof(XnaToFnaHelper).GetMethod("PreUpdate");
        public static MethodInfo m_XnaToFnaHelper_MainHook = typeof(XnaToFnaHelper).GetMethod("MainHook");
        public static MethodInfo m_FileSystemHelper_FixPath = typeof(FileSystemHelper).GetMethod("FixPath");

        public void SetupHelperRelinker() {
            Modder.Relinker = DefaultRelinker;

            // To use XnaToFnaGame properly, the actual game override needs to call XnaToFnaGame::.ctor as "base" instead.
            Modder.RelinkMap["System.Void Microsoft.Xna.Framework.Game::.ctor()"] =
                Tuple.Create("XnaToFna.XnaToFnaGame", "System.Void .ctor()");
            foreach (MethodInfo method in typeof(XnaToFnaGame).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
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
                // Substitute common Drawing classes (f.e. Rectangle) with our own for Drawing-less environments (f.e. Android)
                else if (name.StartsWith("XnaToFna.ProxyDrawing."))
                    Modder.RelinkMap["System.Drawing." + name.Substring(9 + 13)] = name;
                // Some XNA games use DInput... let's just substitute all DInput references with our ProxyDInput.
                else if (name.StartsWith("XnaToFna.ProxyDInput."))
                    Modder.RelinkMap[/* no namespace */ name.Substring(9 + 12)] = name;
            }

            if (HookIsTrialMode)
                Modder.RelinkMap["System.Boolean Microsoft.Xna.Framework.GamerServices.Guide::get_IsTrialMode()"] =
                    Tuple.Create("XnaToFna.XnaToFnaHelper", "System.IntPtr get_IsTrialMode()");

            if (HookBinaryFormatter) {
                Modder.RelinkMap["System.Void System.Runtime.Serialization.Formatters.Binary.BinaryFormatter::.ctor()"] =
                    Tuple.Create("XnaToFna.BinaryFormatterHelper", "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter Create()");

                // The longest relink mapping ever seen...
                Modder.RelinkMap["System.Void System.Runtime.Serialization.Formatters.Binary.BinaryFormatter::.ctor(System.Runtime.Serialization.ISurrogateSelector,System.Runtime.Serialization.StreamingContext)"] =
                    Tuple.Create("XnaToFna.BinaryFormatterHelper", "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter Create(System.Runtime.Serialization.ISurrogateSelector,System.Runtime.Serialization.StreamingContext)");

                Modder.RelinkMap["System.Runtime.Serialization.SerializationBinder System.Runtime.Serialization.Formatters.Binary.BinaryFormatter::get_Binder()"] =
                    Tuple.Create("XnaToFna.BinaryFormatterHelper", "System.Runtime.Serialization.SerializationBinder get_Binder(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter)");

                Modder.RelinkMap["System.Void System.Runtime.Serialization.Formatters.Binary.BinaryFormatter::set_Binder(System.Runtime.Serialization.SerializationBinder)"] =
                    Tuple.Create("XnaToFna.BinaryFormatterHelper", "System.Void set_Binder(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter,System.Runtime.Serialization.SerializationBinder)");
            }

        }

        public IMetadataTokenProvider DefaultRelinker(IMetadataTokenProvider mtp, IGenericParameterProvider context)
            => _DefaultRelinker(mtp, context);
        public IMetadataTokenProvider _DefaultRelinker(IMetadataTokenProvider mtp, IGenericParameterProvider context, bool retry = false) {
            // Skip MonoModLinkTo attribute handling.
            try {
                return Modder.PostRelinker(
                    Modder.MainRelinker(mtp, context),
                    context);
            } catch (RelinkTargetNotFoundException e) {
                if (retry)
                    throw;
                MemberReference member = mtp as MemberReference;
                if (member == null)
                    throw;

                // If this is a .GamerServices reference pointing towards .Game or similar, flip tables. (X360!)

                string name;
                object target;
                if (mtp is TypeReference) {
                    target = name = ((TypeReference) mtp).FullName;
                } else if (mtp is MethodReference) {
                    name = ((MethodReference) mtp).GetFindableID(withType: true);
                    target = Tuple.Create(member.DeclaringType.FullName, ((MethodReference) mtp).GetFindableID(withType: false));
                } else if (mtp is FieldReference) {
                    name = ((FieldReference) mtp).FullName;
                    target = Tuple.Create(member.DeclaringType.FullName, ((FieldReference) mtp).Name);
                } else
                    throw;

                Modder.RelinkMap[name] = target;
                Modder.RelinkMapCache[name] = null;
                return _DefaultRelinker(mtp, context, true);
            }
        }

        public void PreProcessType(TypeDefinition type) {
            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasPInvokeInfo)
                    continue;
                // Just check if PInvokeHooks contains the entry point, ignoring the module name, except for its end. What can go wrong?...
                if (!method.PInvokeInfo.Module.Name.EndsWith("32.dll") && !method.PInvokeInfo.Module.Name.EndsWith("32"))
                    continue;
                string entryPoint = method.PInvokeInfo.EntryPoint ?? method.Name;
                if (typeof(PInvokeHooks).GetMethod(entryPoint) != null) {
                    Log($"[PreProcess] [PInvokeHooks] Remapping call to {entryPoint} ({method.GetFindableID()})");
                    Modder.RelinkMap[method.GetFindableID(simple: true)] =
                        Tuple.Create("XnaToFna.PInvokeHooks", entryPoint);
                } else {
                    Log($"[PreProcess] [PInvokeHooks] Found unhooked call to {entryPoint} ({method.GetFindableID()})");
                }
            }

            Stack<TypeDefinition> baseTypes = new Stack<TypeDefinition>();
            try {
                for (TypeDefinition baseType = type.BaseType?.Resolve(); baseType != null; baseType = baseType.BaseType?.Resolve())
                    baseTypes.Push(baseType);
            } catch {
                // Unresolved assembly, f.e. XNA itself
            }

            foreach (FieldDefinition field in type.Fields) {
                string name = field.Name;

                if (FixOldMonoXML && baseTypes.Any(baseType => baseType.FindField(name) != null || baseType.FindProperty(name) != null)) {
                    // Field name collision found. Mono 4.4+ handles them well, while Xamarin.Android still fails.
                    Log($"[PreProcess] Renaming field name collison {name} in {type.FullName}");
                    field.Name = $"{name}_{type.Name}";
                    Modder.RelinkMap[$"{type.FullName}::{name}"] = field.FullName;
                }
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                PreProcessType(nested);
        }

        public void PostProcessType(TypeDefinition type) {
            // Make all Microsoft.Xna.Framework.Games inherit from XnaToFnaGame instead.
            bool isGame = false;
            if (type.BaseType?.FullName == "Microsoft.Xna.Framework.Game") {
                Log($"[PostProcess] Found type overriding Game: {type.FullName})");
                type.BaseType = type.Module.ImportReference(typeof(XnaToFnaGame));
                isGame = true;
            }

            foreach (MethodDefinition method in type.Methods) {
                if (!method.HasBody) continue;

                string id = method.GetFindableID(withType: false);

                if (isGame && id == "System.Void Update(Microsoft.Xna.Framework.GameTime)") {
                    Log("[PostProcess] Injecting call to XnaToFnaHelper.PreUpdate into game Update");
                    ILProcessor il = method.Body.GetILProcessor();
                    il.InsertBefore(method.Body.Instructions[0], il.Create(OpCodes.Ldarg_1));
                    il.InsertAfter(method.Body.Instructions[0], il.Create(OpCodes.Callvirt,
                        method.Module.ImportReference(m_XnaToFnaHelper_PreUpdate)));
                    method.Body.UpdateOffsets(1, 2);
                }

                for (int i = 0; i < method.Body.Instructions.Count; i++) {
                    Instruction instr = method.Body.Instructions[i];

                    // Fix XnaToFnaHelper calls still being callvirt calls.
                    if (instr.OpCode == OpCodes.Callvirt && ((MethodReference) instr.Operand).DeclaringType.FullName == "XnaToFna.XnaToFnaHelper") {
                        instr.OpCode = OpCodes.Call;
                    }

                    if (DestroyLocks)
                        CheckAndDestroyLock(method, i);

                    if (FixPathsFor.Count != 0)
                        CheckAndInjectFixPath(method, ref i);

                }

                // Calculate updated offsets required for any further manual fixes.
                // ... could this be useful outside of XnaToFna?
                {
                    int offs = 0;
                    for (int i = 0; i < method.Body.Instructions.Count; i++) {
                        Instruction instr = method.Body.Instructions[i];
                        instr.Offset = offs;
                        offs += instr.GetSize();
                    }
                }


                for (int i = 0; i < method.Body.Instructions.Count; i++) {
                    Instruction instr = method.Body.Instructions[i];

                    // Change short <-> long operations as the method grows / shrinks.
                    if (instr.Operand is Instruction) {
                        int offs = ((Instruction) instr.Operand).Offset - instr.Offset;
                        if (offs < sbyte.MinValue || offs > sbyte.MaxValue)
                            instr.OpCode = ShortToLongOp(instr.OpCode);
                        else
                            instr.OpCode = LongToShortOp(instr.OpCode);
                    }

                }

            }

            foreach (TypeDefinition nested in type.NestedTypes)
                PostProcessType(nested);
        }

        public OpCode ShortToLongOp(OpCode op) {
            string name = Enum.GetName(typeof(Code), op.Code);
            if (!name.EndsWith("_S"))
                return op;
            return (OpCode?) typeof(OpCodes).GetField(name.Substring(0, name.Length - 2))?.GetValue(null) ?? op;
        }

        public OpCode LongToShortOp(OpCode op) {
            string name = Enum.GetName(typeof(Code), op.Code);
            if (name.EndsWith("_S"))
                return op;
            return (OpCode?) typeof(OpCodes).GetField(name + "_S")?.GetValue(null) ?? op;
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
                    Log($"[PostProcess] [HACK!!!] NOPing possible content loading waiting loop in {method.GetFindableID()}");
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
                    Log($"[PostProcess] [HACK!!!] Destroying possible content loading lock in {method.GetFindableID()}");
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
                            Log($"[PostProcess] [HACK!!!] Destroying possible content loading lock in {method.GetFindableID()}");
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

        public void CheckAndInjectFixPath(MethodDefinition method, ref int instri) {
            Mono.Collections.Generic.Collection<Instruction> instrs = method.Body.Instructions;
            Instruction instr = instrs[instri];

            /* This hack at least is somewhat reasonable...
             * """Fix""" paths being passed to methods that heavily rely on proper paths.
             * This is for when MONO_IOMAP=all isn't enough because it doesn't affect native libs.
             * -ade
             */

            if (instr.OpCode != OpCodes.Call && instr.OpCode != OpCodes.Callvirt && instr.OpCode != OpCodes.Newobj)
                return;

            MethodReference called = (MethodReference) instr.Operand;
            if (!FixPathsFor.Contains(called.GetFindableID()) &&
                !FixPathsFor.Contains(called.GetFindableID(withType: false)) &&
                !FixPathsFor.Contains(called.GetFindableID(simple: true)) &&
                !FixPathsFor.Contains(called.GetFindableID(withType: false, simple: true))
                )
                return;

            MethodReference mr_Push = method.Module.ImportReference(StackOpHelper.m_Push);
            MethodReference mr_Pop = method.Module.ImportReference(StackOpHelper.m_Pop);
            MethodReference mr_FixPath = method.Module.ImportReference(m_FileSystemHelper_FixPath);

            ILProcessor il = method.Body.GetILProcessor();
            // Go through all parameters in stack, store all passed parameters in StackOpHelper and handle accordingly.
            for (int i = called.Parameters.Count - 1; i > -1; --i) {
                if (called.Parameters[i].ParameterType.MetadataType == MetadataType.String) {
                    // Before pushing to StackOpHelper, try to fix the path (we're assuming it's a path).
                    il.InsertBefore(instrs[instri], il.Create(OpCodes.Call, mr_FixPath));
                    instri++;
                }

                GenericInstanceMethod push = new GenericInstanceMethod(mr_Push);
                push.GenericArguments.Add(called.Parameters[i].ParameterType);
                il.InsertBefore(instrs[instri], il.Create(OpCodes.Call, push));
                instri++;
            }

            // Pop StackOpHelper.
            for (int i = 0; i <  called.Parameters.Count; i++) {
                GenericInstanceMethod pop = new GenericInstanceMethod(mr_Pop);
                pop.GenericArguments.Add(called.Parameters[i].ParameterType);
                il.InsertBefore(instrs[instri], il.Create(OpCodes.Call, pop));
                instri++;
            }
        }


    }
}
