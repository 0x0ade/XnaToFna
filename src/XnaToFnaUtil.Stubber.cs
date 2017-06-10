using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using XnaToFna.ProxyForms;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using XnaToFna.TimeMachine;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {

        public static System.Reflection.ConstructorInfo m_UnverifiableCodeAttribute_ctor = typeof(System.Security.UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes);

        public void Stub(ModuleDefinition mod) {
            Log($"[Stub] Stubbing {mod.Assembly.Name.Name}");
            Modder.Module = mod;

            Log($"[Stub] Updating dependencies");
            for (int i = 0; i < mod.AssemblyReferences.Count; i++) {
                AssemblyNameReference dep = mod.AssemblyReferences[i];

                // Main mapping mass.
                foreach (Tuple<string, string[]> mappings in Mappings)
                    if (mappings.Item2.Contains(dep.Name) &&
                        // Check if the target module has been found and cached
                        Modder.DependencyCache.ContainsKey(mappings.Item1)) {
                        // Check if module already depends on the remap
                        if (mod.AssemblyReferences.Any(existingDep => existingDep.Name == mappings.Item1)) {
                            // If so, just remove the dependency.
                            mod.AssemblyReferences.RemoveAt(i);
                            i--;
                            goto NextDep;
                        }
                        Log($"[Stub] Replacing dependency {dep.Name} -> {mappings.Item1}");
                        // Replace the dependency.
                        mod.AssemblyReferences[i] = Modder.DependencyCache[mappings.Item1].Assembly.Name;
                        // Only check until first match found.
                        goto NextDep;
                    }

                // Didn't remap; Check for RemoveDeps
                if (RemoveDeps.Contains(dep.Name)) {
                    // Remove any unwanted mixed dependencies.
                    Log($"[Stub] Removing unwanted dependency {dep.Name}");
                    mod.AssemblyReferences.RemoveAt(i);
                    i--;
                    goto NextDep;
                }

                NextDep:
                continue;
            }
            if (!mod.AssemblyReferences.Any(dep => dep.Name == ThisAssemblyName)) {
                // Add XnaToFna as dependency
                Log($"[Stub] Adding dependency XnaToFna");
                mod.AssemblyReferences.Add(Modder.DependencyCache[ThisAssemblyName].Assembly.Name);
            }

            if (EnableTimeMachine) {
                // XNA 3.0 / 3.1 games depend on a .NET Framework pre-4.0
                mod.Runtime = TargetRuntime.Net_4_0;
                // TODO: What about the System.*.dll dependencies?
            }

            // MonoMod needs to relink some types (f.e. XnaToFnaHelper) via FindType, which requires a dependency map.
            Log("[Stub] Mapping dependencies for MonoMod");
            Modder.MapDependencies(mod);

            bool mixed = (mod.Attributes & ModuleAttributes.ILOnly) != ModuleAttributes.ILOnly;
            if (mixed) {
                Log("[Stub] Handling mixed mode assembly");
                mod.Attributes |= ModuleAttributes.ILOnly;
                for (int i = 0; i < mod.Assembly.CustomAttributes.Count; i++) {
                    CustomAttribute attrib = mod.Assembly.CustomAttributes[i];
                    if (attrib.AttributeType.FullName == "System.CLSCompliantAttribute") {
                        mod.Assembly.CustomAttributes.RemoveAt(i);
                        i--;
                    }
                }
                if (!mod.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Security.UnverifiableCodeAttribute"))
                    mod.AddAttribute(mod.ImportReference(m_UnverifiableCodeAttribute_ctor));
                mod.ModuleReferences.Clear();
            }
            mod.Attributes &= ~ModuleAttributes.StrongNameSigned;

            Log($"[Stub] Stubbing");
            foreach (TypeDefinition type in mod.Types)
                StubType(type, mixed);

            Log($"[Stub] Pre-processing");
            foreach (TypeDefinition type in mod.Types)
                PreProcessType(type);

            Log($"[Stub] Relinking (MonoMod PatchRefs pass)");
            Modder.PatchRefs();

            Log($"[Stub] Post-processing");
            foreach (TypeDefinition type in mod.Types)
                PostProcessType(type);

            Log($"[Stub] Rewriting and disposing module\n");
            Modder.Module.Write(Modder.WriterParameters);
            // Dispose the module so other modules can read it as a dependency again.
            Modder.Module.Dispose();
            Modder.Module = null;
            Modder.ClearCaches(moduleSpecific: true);
        }

        public void StubType(TypeDefinition type, bool mixed = false) {
            foreach (MethodDefinition method in type.Methods) {
                if (method.HasPInvokeInfo)
                    method.PInvokeInfo = null;
                method.IsManaged = true;
                method.IsIL = true;
                method.IsNative = false;
                method.PInvokeInfo = null;
                method.IsPreserveSig = false;
                method.IsInternalCall = false;
                method.IsPInvokeImpl = false;

                MethodBody body = method.Body = new MethodBody(method);
                body.InitLocals = true;
                ILProcessor il = body.GetILProcessor();

                for (int i = 0; i < method.Parameters.Count; i++) {
                    ParameterDefinition param = method.Parameters[i];
                    if (param.IsOut || param.IsReturnValue) {
                        il.Emit(OpCodes.Ldarg, i);
                        il.EmitDefault(param.ParameterType, true);
                    }
                }

                il.EmitDefault(method.ReturnType ?? method.Module.TypeSystem.Void);
                il.Emit(OpCodes.Ret);
            }

            foreach (TypeDefinition nested in type.NestedTypes)
                StubType(nested);
        }

    }
}
